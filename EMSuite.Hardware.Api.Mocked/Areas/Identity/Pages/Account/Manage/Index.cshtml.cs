using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using EMSuite.DataAccess;
using EMSuite.Identity.Data.Entities;
using EMSuite.Models;
using EMSuite.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EMSuite.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<EMSuiteUser> _userManager;
        private readonly SignInManager<EMSuiteUser> _signInManager;
        private readonly IAuditService _auditService;
        private readonly IMobileService mobileService;
        private readonly IUserProfileService _userProfileService;
        private readonly IAccountService accountService;

        public IndexModel(
            UserManager<EMSuiteUser> userManager,
            SignInManager<EMSuiteUser> signInManager,
            IAuditService auditService,
            IMobileService mobileService,
            IUserProfileService userProfileService,
            IAccountService accountService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auditService = auditService;
            this.mobileService = mobileService;
            _userProfileService = userProfileService;
            this.accountService = accountService;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Language")]
            public string Language { get; set; }

            [Display(Name = "Languages")]
            public IEnumerable<SelectListItem> Languages { get; set; }
        }

        private async Task LoadAsync(EMSuiteUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            var userLanguage = await _userProfileService.GetUserLanguage(user);

            if (String.IsNullOrEmpty(userLanguage))
            {
                var profile = await _userProfileService.GetDefaultDisplayInfo();
                userLanguage = profile.LanguageIsoCode;
            }
            var languages = _userProfileService.GetLanguages();
            var languagesList = new SelectList(languages, "Code", "Name", userLanguage);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                Language = userLanguage,
                Languages = languagesList
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            bool hasSetNewNumber = false;
            if (Input.PhoneNumber != phoneNumber)
            {
                _userManager.Options.User.RequireUniqueEmail = false;
                //check for phonenumber duplicates
                if (await accountService.IsUpdatedPhoneNumberAlreadyUsed(Input.PhoneNumber, user.Id))
                {
                    StatusMessage = "Unable to set phone number.";
                    return RedirectToPage();
                }


                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unable to set phone number.";
                    return RedirectToPage();
                }
                hasSetNewNumber = true;
                //reset mobile access flags in DB if user changes phonenumber
                var resetResult = await mobileService.ResetUserFlagsPhoneNumberChange(user.Id); 
                
                if (!resetResult)
                {
                    StatusMessage = "Unable to reset mobile app end points for user.";
                    return RedirectToPage();
                }
            }
            bool result = await _userProfileService.SetUserLanguage(Input.Language, user);
            if (!result)
            {
                StatusMessage = "Unexpected error when trying to set language.";
                return RedirectToPage();
            }
            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated. Notice any access to the mobile app, has been revoked";
            if (hasSetNewNumber)
                await _auditService.AuditEntry(new AuditEntryObject() { LocalisationKey = "ProfileInformationUpdated" }, user.UserName, phoneNumber, Input.PhoneNumber);
            return RedirectToPage();
        }
    }
}

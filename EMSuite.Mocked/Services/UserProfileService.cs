using Microsoft.Extensions.Localization;

namespace EMSuite.Mocked.Services
{
    public interface IUserProfileService
    {
        List<Language> GetLanguages();
        Task<string> GetUserLanguage(EMSuiteUser user);
        Task<DisplaySetupEntry> GetDefaultDisplayInfo();
        Task<bool> SetUserLanguage(string LanguageIsoCode, EMSuiteUser user);
    }

    public class UserProfileService : IUserProfileService
    {
        private readonly IStringLocalizer<UserProfileService> _localizer;
        private readonly IDataAccess _context;
        private readonly IAuditService _auditService;

        public UserProfileService(
            IStringLocalizer<UserProfileService> localizer,
            IDataAccess context,
            IAuditService auditService
            )
        {
            _localizer = localizer;
            _context = context;
            _auditService = auditService;
        }
        public List<Language> GetLanguages()
        {
            List<Language> languages = new List<Language>
            {
                new Language { Code = "en", Name =_localizer["en"]},
                new Language { Code = "es", Name = _localizer["es"] },
                new Language { Code = "pt", Name = _localizer["pt"] },
                new Language { Code = "de", Name = _localizer["de"] },
                new Language { Code = "fr", Name = _localizer["fr"] },
                new Language { Code = "zh", Name = _localizer["zh"] },
                new Language { Code = "ko", Name = _localizer["ko"] },
                new Language { Code = "ja", Name = _localizer["ja"] },
                new Language { Code = "nl", Name = _localizer["nl"] },
                new Language { Code = "pl", Name = _localizer["pl"] },
                new Language { Code = "it", Name = _localizer["it"] },
                new Language { Code = "ru", Name = _localizer["ru"] },
                new Language { Code = "tr", Name = _localizer["tr"] }
            };

            return languages;
        }
        public async Task<string> GetUserLanguage(EMSuiteUser user)
        {
            string q1 = @"SELECT LanguageIsoCode FROM dbo.DisplaySetupUser WHERE UserId = @userId";
            var result = await _context.Query<string>(q1,
                new QueryParameter<string> { Name = "@userId", Value = user.Id }
            );
            return result.FirstOrDefault();
        }
        public async Task<DisplaySetupEntry> GetDefaultDisplayInfo()
        {
            string q2 = @"SELECT TOP 1 TimeZoneInfoId, LanguageIsoCode, DateTimeFormat FROM dbo.DisplaySetup";
            var result = await _context.Query<DisplaySetupEntry>(q2);
            return result.FirstOrDefault();
        }
        public async Task<bool> SetUserLanguage(string language, EMSuiteUser user)
        {
            var defaultProfile = await GetDefaultDisplayInfo();
            var oldLanguage = await GetUserLanguage(user);
            language = String.IsNullOrEmpty(language) ? defaultProfile.LanguageIsoCode : language;

            using var transaction = await _context.StartTransaction();
            var queryResult = await _context.InsertOrUpdate<string>(
                                "DisplaySetupUser",
                                "UserId = @userId",
                                transaction,
                                new ColumnParameter { ColumnName = "LanguageIsoCode", Name = "@languageIsoCode", Value = language },
                                new ColumnParameter { ColumnName = "UserId", Name = "@userId", Value = user.Id, IsOutput = true }
                            );

            if (queryResult.Succeeded)
            {
                var languages = GetLanguages();
                if (String.IsNullOrEmpty(oldLanguage))
                {
                    await _auditService.AuditEntry(new AuditEntryObject() { LocalisationKey = "add_user_profile", Transaction = transaction }, user.UserName, languages.Where(l => l.Code == language).SingleOrDefault()?.Name);
                }
                else if (oldLanguage != language)
                {
                    await _auditService.AuditEntry(new AuditEntryObject() { LocalisationKey = "update_user_profile", Transaction = transaction }, user.UserName, languages.Where(l => l.Code == oldLanguage).SingleOrDefault()?.Name, languages.Where(l => l.Code == language).SingleOrDefault()?.Name);
                }
                await transaction.Commit();
                return true;
            }
            else
            {
                await transaction.RollBack();
                return false;
            }
        }
    }
    public class Language
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}

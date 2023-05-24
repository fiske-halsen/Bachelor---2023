using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace EMSuite.Hardware.Api.Mocked.Controllers
{
    public class AlarmTemplateController : Controller
    {
        private readonly IStringLocalizer<AlarmTemplateController> _localizer;
        private readonly IAlarmTemplateService _service;

        public AlarmTemplateController(
            IStringLocalizer<AlarmTemplateController> localizer,
            IAlarmTemplateService service)
        {
            _localizer = localizer;
            _service = service;
        }

        public IActionResult Index()
        {
            return View();
        }
        #region -- Notifications -- 

        [HttpGet]
        public async Task<ActionResult> NotificationView(int id)
        {
            ThresholdNotificationViewModel model = await _service.GetNotificationViewModel(id);
            return PartialView("_NotificationPartial", model); // Her loader vi vores Notification Partial view
        }

        [Authorize(Policy = Permissions.AlarmTemplate.Configure)]
        [HttpPut]
        public async Task<IActionResult> SetLocationSpecificMode(int id, bool mode)
        {
            await _service.SetLocationSpecificMode(id, mode);
            return Ok(mode);
        }

        [HttpGet]
        public async Task<IActionResult> NotificationUsers(int id, DataSourceLoadOptions options)
        {
            return Json(await _service.GetNotificationUsers(id, options));
        }

        [HttpGet]
        public IActionResult NotificationNodes(int id, DataSourceLoadOptions options)
        {
            var zoneIDS = User.Zones();
            var siteIDS = User.Sites();
            List<string> zoneString = zoneIDS.ToList().ConvertAll(x => "Z" + x.ToString());
            List<string> sitesString = siteIDS.ToList().ConvertAll(x => "S" + x.ToString());
            var nodes = _service.GetNotificationNodes(id, options).Result;

            var j = JsonConvert.SerializeObject(nodes.data);
            var channelNodes = JsonConvert.DeserializeObject<List<TreeNodeModel>>(j);
            channelNodes = TreenodesFilterSitesZones(channelNodes, sitesString, zoneString);
            var result = DataSourceLoader.Load(channelNodes, options);
            var resultJson = JsonConvert.SerializeObject(result);
            return Content(resultJson, "application/json");
        }

        [Authorize(Policy = Permissions.AlarmTemplate.Configure)]
        [HttpPost]
        public async Task<IActionResult> AssignUsers(int id, [FromBody] NotificationPackageUser notificationPackage)
        {
            DatabaseCommandResult result = await _service.HandleNotificationUser(id, notificationPackage);

            if (!result.Success && result.Errors != null)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            return ModelState.IsValid
                ? (ActionResult)Json(result.Message)
                : (ActionResult)BadRequest(ModelState.ToDevExtremeErrors());
        }

        [Authorize(Policy = Permissions.AlarmTemplate.Configure)]
        [HttpPost]
        public async Task<IActionResult> AssignUserNodes(int id, [FromBody] NotificationPackageZoneSpecific notificationPackage)
        {
            DatabaseCommandResult result = await _service.HandleNotificationZoneSpecific(id, notificationPackage);

            if (!result.Success && result.Errors != null)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
            }

            return ModelState.IsValid
                ? (ActionResult)Json(result.Message)
                : (ActionResult)BadRequest(ModelState.ToDevExtremeErrors());
        }
        #endregion
    }
}

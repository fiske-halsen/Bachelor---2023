using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMSuite.Hardware.Api.Mocked.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("1.1")]
    [Route("api/v{v:apiVersion}/[controller]")]
    [Authorize]
    public class DeviceController : ControllerBase
    {
        private readonly HardwareStatusService _service;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DeviceController> _logger;
        private readonly HardwareDiagnosticsSignalRClient _diagnosticsHub;
        protected readonly IDataAccess _dataContext;

        public DeviceController(HardwareStatusService service,
            IConfiguration configuration,
            IDataAccess dataContext,
            HardwareDiagnosticsSignalRClient diagnosticsHub,
            ILogger<DeviceController> logger)
        {
            _service = service;
            _dataContext = dataContext;
            _configuration = configuration;
            _diagnosticsHub = diagnosticsHub;
            _logger = logger;
        }

        [HttpPost]
        [Route("{serial}/data")]
        public async Task<IActionResult> NewData(uint serial, IEnumerable<HardwareLoggerData> data)
        {
            await RefreshCultureLanguage();

            if (await _service.IsActiveAccessPoint(serial))
            {
                if (await _service.NewData(serial, data))
                {
                    JsonResult result = new(
                        new AccessPointResponse
                        {
                            Success = true,
                            RefreshConfig = await _service.HasWaitingHardwareChanges(serial)
                        });

                    await _diagnosticsHub.AttachDiagnostic(serial, data.First().Serial.ToString(), data, result.Value, "AccessPointJsonData");

                    return result;
                }
                else
                {
                    _logger.LogError("Access Point '{AccessPoint}' Invalid Data!", serial);
                }
            }
            else
            {
                _logger.LogError("Inactive Access Point, Transmitter '{AccessPoint}' Alarm is invalid!", serial);
            }

            return new JsonResult(new AccessPointResponse { Success = true, RefreshConfig = false });
        }

        [HttpPost]
        [Route("{serial}/alarms")]
        public async Task<IActionResult> NewAlarmData(uint serial, IEnumerable<HardwareLoggerAlarm> data)
        {
            IActionResult result = BadRequest();
            await RefreshCultureLanguage();

            if (await _service.IsActiveAccessPoint(serial))
            {
                int errorCount = 0;

                foreach (HardwareLoggerAlarm alarm in data.OrderBy(a => a.Channels.Min(c => c.Time)))
                {
                    if (!await NewAlarmData(serial, alarm))
                    {
                        _logger.LogWarning("Did not process alarm for {Logger}", alarm.Serial);
                        errorCount++;
                    }
                }

                result = new JsonResult(
                       new AccessPointResponse
                       {
                           Success = true,
                           RefreshConfig = await _service.HasWaitingHardwareChanges(serial)
                       });
            }
            else
            {
                _logger.LogError("Inactive Access Point, Transmitter '{Logger}' Alarm is invalid!", serial);
                result = new JsonResult(new AccessPointResponse { Success = true, RefreshConfig = false });
            }

            await _diagnosticsHub.AttachDiagnostic(serial, null, data, result, "AlarmData");
            return result;
        }

        [HttpPost]
        [Route("{serial}/alarm")]
        public async Task<IActionResult> NewAlarm(uint serial, HardwareLoggerAlarm data)
        {
            IActionResult result;
            await RefreshCultureLanguage();

            if (await _service.IsActiveAccessPoint(serial))
            {
                if (await NewAlarmData(serial, data))
                {
                    result = new JsonResult(
                        new AccessPointResponse
                        {
                            Success = true,
                            RefreshConfig = await _service.HasWaitingHardwareChanges(serial)
                        });
                }
                else
                {
                    _logger.LogError("Transmitter '{Logger}' Invalid Alarm Data!", data.Serial);
                    result = new JsonResult(new AccessPointResponse { Success = true, RefreshConfig = false });
                }
            }
            else
            {
                _logger.LogError("Inactive Access Point, Transmitter '{Logger}' Alarm is invalid!", data.Serial);
                result = new JsonResult(new AccessPointResponse { Success = true, RefreshConfig = false });
            }

            await _diagnosticsHub.AttachDiagnostic(serial, null, data, result, "AlarmUpdate");

            return result;
        }

        private async Task<bool> NewAlarmData(uint accessPoint, HardwareLoggerAlarm data)
        {
            if (_service.AccessPoints[accessPoint].ActiveList.ContainsKey(data.Serial))
            {
                return await _service.NewAlarmData(accessPoint, data);
            }

            return false;
        }

    }

}

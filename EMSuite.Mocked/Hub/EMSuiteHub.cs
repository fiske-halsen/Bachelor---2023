using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace EMSuite.Mocked.Hub
{
    [Authorize(Policy = "SignalR")]
    public class EMSuiteHub : Hub
    {
        private readonly IAuditService auditService;
        private readonly ICalibrationService calibrationService;
        private readonly ISensorService sensorService;
        private readonly IClientMappingService clientMappingService;
        private readonly ISitesService sitesService;
        private readonly IEllabEMsuiteTilingService ellabEMsuiteTiling;
        private readonly IMyEMSuiteDashboardService myEMSuiteDashboardService;
        private readonly IGlobalAlarmService globalAlarmService;
        private readonly IHardwareService hardwareService;
        private readonly IMobileService mobileService;
        private readonly ILogicalService logicalService;
        private readonly ICHubService cHubService;
        private readonly IMaintenanceService maintenanceService;
        private readonly IHardwareSmsService _hardwareSms;
        private readonly IParticleService _particleService;
        private readonly ILogger<EMSuiteHub> _logger;
        private readonly IEMSuiteSoftPlcRuleService eMSuiteSoftPlcRuleService;
        private readonly IEMSuiteSoftPlcService eMSuiteSoftPlcService;
        private readonly IAccessPointService accessPointService;

        public EMSuiteHub(IAuditService auditService,
            ICalibrationService calibrationService,
            ISensorService sensorService,
            IClientMappingService clientMapping,
            ISitesService sitesService,
            IEllabEMsuiteTilingService ellabEMsuiteTiling,
            IMyEMSuiteDashboardService myEMSuiteDashboardService,
            IGlobalAlarmService globalAlarmService,
            IHardwareService hardwareService,
            IMobileService mobileService,
            ILogicalService logicalService,
            ICHubService cHubService,
            IMaintenanceService maintenanceService,
            IHardwareSmsService hardwareSms,
            IParticleService particleService,
            ILogger<EMSuiteHub> logger,
            IEMSuiteSoftPlcRuleService eMSuiteSoftPlcRuleService,
            IEMSuiteSoftPlcService eMSuiteSoftPlcService,
            IAccessPointService accessPointService


        )
        {
            this.auditService = auditService;
            this.calibrationService = calibrationService;
            this.sensorService = sensorService;
            this.clientMappingService = clientMapping;
            this.sitesService = sitesService;
            this.ellabEMsuiteTiling = ellabEMsuiteTiling;
            this.myEMSuiteDashboardService = myEMSuiteDashboardService;
            this.globalAlarmService = globalAlarmService;
            this.hardwareService = hardwareService;
            this.mobileService = mobileService;
            this.logicalService = logicalService;
            this.cHubService = cHubService;
            this.maintenanceService = maintenanceService;
            _hardwareSms = hardwareSms;
            _particleService = particleService;
            _logger = logger;
            this.eMSuiteSoftPlcRuleService = eMSuiteSoftPlcRuleService;
            this.eMSuiteSoftPlcService = eMSuiteSoftPlcService;
            this.accessPointService = accessPointService;
        }

        /// <summary>
        /// Handles client mapping clean up !!! no functionality is tied to this yet
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public async override Task OnDisconnectedAsync(Exception exception)
        {
            var cid = Context.ConnectionId;

            Trace.WriteLine($"CLIENT DISCONNECTED {cid} "); // !!!! EXPOSING CONNECTION ID MUST REMOVE - STRICTLY FOR TESTING

            //TODO HANDLE UN-FORCED USER DISCONNECTS / CLIENT CONNECT LOSS DUE TO NO CELL RECEPTION

            CheckApiDisconnect(cid);
            ClientCleanup(cid);

            // For the Chub/EmSuite Particle group
            await RemoveFromGroupByCid(cid);

            await base.OnDisconnectedAsync(exception);
        }

        private void ClientCleanup(string ClientConnectionId)
        {
            CleanupEMSuiteCacheServiceRealTimeGraphIDConnIdPair(ClientConnectionId);
            clientMappingService.RemoveClientObjBasedOnConnId(ClientConnectionId);
            Trace.WriteLine(
                $"cleaned out EMSuiteCacheService new length: {EMSuiteCacheService.RealTimeGraphIDConnIdPair.Count}");
            //hubLoggerService.AddLogEntry(new HubLoggerPacket() { TimeStamp = DateTime.Now, Text = $"Client disconnected: {cid}" });
            GC.Collect();
        }

        //TODO remove connectionId from trace output
        public override Task OnConnectedAsync()
        {

            Trace.WriteLine("new client connected - ID: " + Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public async Task SendPhoneAlarmCall(PhoneNotificationPackage phoneNotificationPackage)
        {
            await Clients.Client(EMSuiteCacheService.PhoneNotificationService).SendAsync("ReceivePhoneAlarmPackage", phoneNotificationPackage);
        }

        public async Task RegisterPhoneNotificationService()
        {
            EMSuiteCacheService.PhoneNotificationService = Context.ConnectionId;
        }

         }
    }

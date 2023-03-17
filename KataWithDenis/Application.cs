using System;
using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KataWithDenis.App;

namespace KataWithDenis
{
    public static class AppConstants
    {
        public static readonly Guid AdminId = Guid.NewGuid();
        public static readonly string InspectorRole = "Inspector";
    }

    public class Application
    {
        private readonly UserRepository _users = new UserRepository();
        private readonly UserRepository _inspectors = new UserRepository();
        private readonly List<string> _adminSessions = new List<string>();
        private readonly InspectionRepository _inspections;
        private readonly InvoiceRepository _invoices = new InvoiceRepository();
        private readonly List<Tuple<Guid, string>> _userSessions = new List<Tuple<Guid, string>>();
        public readonly UninvoicedInspectionsProjector _uninvoicedInspectionsProjector;
        public readonly EventStore _eventStore = new EventStore();

        public Application()
        {
            var inspectionInfra = new EventBasedInfrastructure<InspectionEvent>("inspections.dat", _inspections);
            var invoiceInfra = new StateBasedInfrastructure<Invoice>("invoices.dat", _invoices);
            _inspections = new InspectionRepository(_eventStore);

            _uninvoicedInspectionsProjector = new UninvoicedInspectionsProjector(_inspections, _invoices);

            inspectionInfra.Load();
            invoiceInfra.Load();
        }

        public async Task<Guid> RegisterUser(string email)
        {
            var userId = Guid.NewGuid();

            await _users.Add(userId);

            return userId;
        }

        public async Task<SigninResult> Signin(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return SigninResult.BadInput;
            }

            if (!await _users.CheckIfUserIdExists(userId))
            {
                return SigninResult.UserNotFound;
            }

            string sessionId = Guid.NewGuid().ToString();

            if (userId == AppConstants.AdminId)
            {
                _adminSessions.Add(sessionId);
            }

            await LinkUserToSession(userId, sessionId);

            return SigninResult.Success(sessionId);
        }

        public async Task AssignInspector(Guid userId, string sessionId)
        {
            if (!_adminSessions.Contains(sessionId))
            {
                throw new Exception();
            }

            await _inspectors.Add(userId);
        }

        public async Task<List<string>> GetRoles(Guid userId)
        {
            var roles = new List<string>();

            if (await _inspectors.CheckIfUserIdExists(userId))
            {
                roles.Add(AppConstants.InspectorRole);
            }

            return roles;
        }

        public async Task<List<Inspection>> GetInspections(string inspectorSessionId)
        {
            var allStreams = _eventStore.GetStreamFamily();
            var allIds = allStreams.GetAllUniqueIds();
            List<Inspection> inspections = new List<Inspection>();

            foreach (var id in allIds)
            {
                var stream = allStreams.GetStream(id);

                var inspection = Inspection.Replay(stream.Events);

                inspections.Add(inspection);
            }

            return inspections;
        }

        public async Task<List<Inspection>> GetDashboardInspections(string inspectorSessionId)
        {
            var inspections = (await GetInspections(inspectorSessionId))
                                .OrderByDescending(i => i.IsCompleted).ToList();

            return inspections;
        }

        public async Task<Inspection> StartInspection(Inspection inspection, string inspectorSessionId)
        {
            await CheckRole(inspectorSessionId, AppConstants.InspectorRole);

            await _inspections.Add(inspection);

            return inspection;
        }

        public async Task CompleteInspection(Guid inspectionId, string inspectorSessionId)
        {
            var completeInspectionCmd = new CompleteInspectionCommand(inspectionId);

            var inspectionInfra = new EventBasedInfrastructure<InspectionEvent>("inspections.dat", _inspections);
            var invoiceInfra = new StateBasedInfrastructure<Invoice>("invoices.dat", _invoices);

            await completeInspectionCmd.Run(_inspections, _invoices, inspectionInfra, invoiceInfra);
        }

        public async Task<Dashboard> GetDashboard(string sessionId)
        {
            return new Dashboard(
                await GetInspections(sessionId),
                await _invoices.GetAll());
        }

        public async Task<List<Inspection>> GetJoinableInspections(string inspectoreSessionId)
        {
            return (await GetInspections()).Where(i => !i.IsCompleted).ToList();
        }

        private async Task CheckRole(string sessionId, string role)
        {
            var userId = _userSessions.Find(i => i.Item2 == sessionId)?.Item1;

            if (userId is null)
            {
                throw new Exception();
            }

            var roles = await GetRoles(userId.Value);

            if (!roles.Contains(role))
            {
                throw new Exception();
            }
        }

        private async Task LinkUserToSession(Guid userId, string sessionId)
        {
            _userSessions.Add(new Tuple<Guid, string>(userId, sessionId));
        }
    }

    
}


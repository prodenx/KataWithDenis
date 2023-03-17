using System;
using KataWithDenis;

namespace Tests
{
    public class DSL
    {
        public Application Application => _application;

        private Application _application;

        public DSL()
        {
            _application = new Application();
        }

        public async Task<Guid> InspectorId()
        {
            var userId = await UserId();
            await TestHelper.AdminAssignsInspector(userId, _application);

            return userId;
        }

        public async Task<Guid> UserId()
        {
            return await _application.RegisterUser("denis@mail.com");
        }

        public async Task<string> Session(Guid userId)
        {
            SigninResult signinResult = await _application.Signin(userId);

            return signinResult.SessionId;
        }

        public async Task<int> JoinableInspectionCount(Guid inspectorId)
        {
            var joinableInspections = await _application.GetJoinableInspections(inspectorId);

            return joinableInspections.Count();
        }
    }
}


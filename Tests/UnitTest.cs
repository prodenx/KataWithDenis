using KataWithDenis;

namespace Tests;

public class UnitTest1
{

    [Fact]
    public async Task Register_User()
    {
        var application = new Application();

        Guid? userId = await TestHelper.AssertRegistrationSuccess("denis@mail.com", application);

        List<string> roles = await application.GetRoles(userId.Value);

        Assert.DoesNotContain(AppConstants.InspectorRole, roles);

        await TestHelper.AssertSigninError(Guid.Empty, application);

        await TestHelper.AssertSigninSuccess(userId.Value, application);

        var fakeUserId = Guid.NewGuid();
        await TestHelper.AssertSigninError(fakeUserId, application);
    }

    [Fact]
    public async Task Register_Users_should_be_unique()
    {
        var application = new Application();

        Guid? userId1 = await TestHelper.AssertRegistrationSuccess("denis@mail.com", application);
        Guid? userId2 = await TestHelper.AssertRegistrationSuccess("cahuk@mail.com", application);

        Assert.NotEqual(userId1, userId2);
    }

    [Fact]
    public async Task Only_Admin_Can_Assign_Inspector_Role()
    {
        var application = new Application();

        Guid newUserId = await TestHelper.AssertRegistrationSuccess("putin@mail.com", application);

        var newUserSessionId = await TestHelper.AssertSigninSuccess(newUserId, application);

        await Assert.ThrowsAsync<Exception>(() => application.AssignInspector(newUserId, newUserSessionId));

        await TestHelper.AdminAssignsInspector(newUserId, application);

        var roles = await application.GetRoles(newUserId);

        Assert.Contains("Inspector", roles);
    }

    [Fact]
    public async Task Only_Inspector_Can_Start_Inspection()
    {
        var application = new Application();

        Guid newUserId = await TestHelper.AssertRegistrationSuccess("putin@mail.com", application);

        var newUserSessionId = await TestHelper.AssertSigninSuccess(newUserId, application);

        var inspection = new Inspection(Guid.NewGuid(), 0);

        var inspectionsBefore = await application.GetInspections(newUserSessionId);

        var inspectionsAmountBefore = inspectionsBefore.Count();

        await Assert.ThrowsAsync<Exception>(() => application.StartInspection(inspection, newUserSessionId));

        await TestHelper.AdminAssignsInspector(newUserId, application);

        await application.StartInspection(inspection, newUserSessionId);

        var inspectionsAfter = await application.GetInspections(newUserSessionId);

        Assert.Equal(inspectionsAfter.Count(), inspectionsAmountBefore + 1);
    }

    [Fact]
    public async Task Open_Inspections_ShouldBe_First()
    {
        var application = new Application();

        var inspectorSessionId = await TestHelper.SigninInspector(application);

        var firstInspection = new Inspection(Guid.NewGuid(), 0);

        await application.StartInspection(firstInspection, inspectorSessionId);

        var secondInspection = new Inspection(Guid.NewGuid(), 0);

        await application.StartInspection(secondInspection, inspectorSessionId);

        await application.CompleteInspection(secondInspection.Id, inspectorSessionId);

        var inspections = await application.GetDashboardInspections(inspectorSessionId);

        Assert.True(inspections.First().IsCompleted);
    }

    [Fact]
    public async Task New_Inspection_ShouldBe_Joinable()
    {
        var A = new DSL();
        var An = A;
        var The = A;

        var inspectorId = await An.InspectorId();
        var inspectorSessionId = await A.Session(inspectorId);

        var inspection = new Inspection(Guid.NewGuid(), 0);
        var countInitialState = await The.JoinableInspectionCount(inspectorId);

        await The.Application.StartInspection(inspection, inspectorSessionId);

        var expectedCountAfterStartedInspectionJoinable = countInitialState + 1;
        var expectedCountAfterCompletedInspectionJoinable = expectedCountAfterStartedInspectionJoinable - 1;

        var countAfterStartedInspection = await The.JoinableInspectionCount(inspectorId);

        await The.Application.CompleteInspection(inspection.Id, inspectorSessionId);

        var countAfterCompletedInspection = await The.JoinableInspectionCount(inspectorId);

        Assert.Equal(expectedCountAfterStartedInspectionJoinable, countAfterStartedInspection);
        Assert.Equal(expectedCountAfterCompletedInspectionJoinable, countAfterCompletedInspection);
    }
}

public class TestHelper
{
    static public async Task<Guid> AssertRegistrationSuccess(string email, Application application)
    {
        Guid? userId = await application.RegisterUser("denis@mail.com");

        Assert.NotNull(userId);
        Assert.NotEqual(userId, Guid.Empty);

        return userId.Value;
    }

    static public async Task AssertSigninError(Guid userId, Application application)
    {
        SigninResult signinResult = await application.Signin(userId);

        Assert.True(signinResult.IsError);

        return;
    }

    static public async Task<string> AssertSigninSuccess(Guid userId, Application application)
    {
        SigninResult signinResult = await application.Signin(userId);

        Assert.False(signinResult.IsError);
        Assert.NotNull(signinResult.SessionId);
        Assert.NotEmpty(signinResult.SessionId);

        return signinResult.SessionId;
    }

    static public async Task AdminAssignsInspector(Guid userId, Application application)
    {
        var adminSessionId = await TestHelper.AssertSigninSuccess(AppConstants.AdminId, application);

        await application.AssignInspector(userId, adminSessionId);
    }

    static public async Task<string> SigninNewUser(Application application)
    {
        Guid newUserId = await TestHelper.AssertRegistrationSuccess("putin@mail.com", application);

        var newUserSessionId = await TestHelper.AssertSigninSuccess(newUserId, application);

        return newUserSessionId;
    }

    static public async Task<string> SigninInspector(Application application)
    {
        Guid newUserId = await TestHelper.AssertRegistrationSuccess("putin@mail.com", application);

        var newUserSessionId = await TestHelper.AssertSigninSuccess(newUserId, application);

        await AdminAssignsInspector(newUserId, application);

        return newUserSessionId;
    }

    //static public async Task<string> CreateAndSigninInspector(Application application)
    //{
    //    Guid inspectorId = await TestHelper.AssertRegistrationSuccess("putin@mail.com", application);

    //    await TestHelper.AdminAssignsInspector(inspectorId, application);

    //    var inspectorSessionId = await TestHelper.AssertSigninSuccess(inspectorId, application);

    //    return inspectorSessionId;
    //}
}
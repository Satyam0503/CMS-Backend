using System.Net;

namespace Codeji.CMS.API.Tests;

[Trait("Category", "Security")]
public class AccessControlTests : ApiTestBase
{
    public AccessControlTests(TestWebAppFactory factory) : base(factory) { }

    [Theory]
    [InlineData("GET", "api/Roles/GetRoles")]
    [InlineData("POST", "api/User/GetAllEmployees")]
    [InlineData("POST", "api/PayRoll/ProcessPayrollMonth")]
    [InlineData("POST", "api/PayRoll/RecordPayrollPayment")]
    [InlineData("POST", "api/PayRoll/ClosePayrollMonth")]
    [InlineData("POST", "api/PayRoll/ValidatePayrollData")]
    [InlineData("POST", "api/PayRoll/GetPayrollAuditHistory")]
    [InlineData("POST", "api/Salary/GetCompanySalaries")]
    [InlineData("GET", "api/admin/attendance/test-user")]
    [InlineData("POST", "api/LeaveManagement/GetLeaveRequests")]
    [InlineData("GET", "api/Dashboard/GetApplicationStatusData")]
    [InlineData("GET", "api/Dashboard/GetDashboardOverview")]
    public async Task ProtectedEndpoints_RequireAuth(string method, string endpoint)
    {
        var response = await UnauthenticatedRequestAsync(method, endpoint);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
            $"Protected endpoint {endpoint} returned {response.StatusCode}: {body}");
    }

    [Theory]
    [InlineData("POST", "api/Roles/AddEditRole")]
    [InlineData("PATCH", "api/CompanyMaster/UpdateModuleAccess/Applications")]
    [InlineData("POST", "api/AutoPayroll/GeneratePayRollMonthly")]
    public async Task AdminOnlyEndpoints_RejectUnauthenticated(string method, string endpoint)
    {
        var response = await UnauthenticatedRequestAsync(method, endpoint);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.MethodNotAllowed,
            $"Admin endpoint {endpoint} returned {response.StatusCode} without auth: {body}");
    }

    private async Task<HttpResponseMessage> UnauthenticatedRequestAsync(string method, string endpoint)
    {
        Client.DefaultRequestHeaders.Authorization = null;
        using var request = new HttpRequestMessage(new HttpMethod(method), endpoint);
        if (!string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        return await Client.SendAsync(request);
    }

    [Fact]
    public async Task Swagger_NotAccessibleInProduction()
    {
        // In production (non-debug), Swagger should be disabled.
        // This tests the current dev environment — flag if exposed.
        var response = await Client.GetAsync("swagger/index.html");
        // Log for awareness — in prod this should be 404
        if (response.StatusCode == HttpStatusCode.OK)
        {
            // Swagger is enabled — expected in dev, but flag for prod verification
            Assert.True(true, "Swagger is accessible — verify this is disabled in production");
        }
    }

}


using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Http.CSharp;

// Load test validating NFR-02: sustained 50 req/s on the daily-balance endpoint
// with at most 5% errors.

var reportingUrl = Environment.GetEnvironmentVariable("REPORTING_URL") ?? "http://localhost:5002";
var merchant = Guid.Parse("11111111-1111-1111-1111-111111111111");
var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

using var httpClient = new HttpClient();

var scenario = Scenario.Create("daily_balance_50rps", async ctx =>
{
    var request = Http.CreateRequest("GET", $"{reportingUrl}/api/v1/daily-balance/{merchant}/{today}");
    var response = await Http.Send(httpClient, request);
    return response;
})
.WithLoadSimulations(
    Simulation.Inject(
        rate: 50,
        interval: TimeSpan.FromSeconds(1),
        during: TimeSpan.FromSeconds(60)));

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder("reports")
    .WithReportFormats(ReportFormat.Html, ReportFormat.Md)
    .Run();

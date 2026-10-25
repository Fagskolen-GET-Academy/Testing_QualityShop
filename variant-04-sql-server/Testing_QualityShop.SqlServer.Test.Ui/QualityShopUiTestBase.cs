using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework.Interfaces;

namespace Testing_QualityShop.SqlServer.Test.Ui;

public abstract class QualityShopUiTestBase : PageTest
{
    private HttpClient _client = null!;

    protected string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("QUALITYSHOP_BASE_URL") ?? "http://localhost:5083";

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = BaseUrl,
        Locale = "en-US",
    };

    [SetUp]
    public async Task ResetTestData()
    {
        _client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var response = await _client.PostAsync("/api/test/reset", content: null);
        response.EnsureSuccessStatusCode();
        await Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task SaveDiagnosticsOnFailure()
    {
        var failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;
        if (failed)
        {
            var artifactDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-artifacts");
            Directory.CreateDirectory(artifactDirectory);
            var testName = string.Concat(
                TestContext.CurrentContext.Test.Name.Select(character =>
                    Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDirectory, $"{testName}.png"),
                FullPage = true,
            });
            await Context.Tracing.StopAsync(new()
            {
                Path = Path.Combine(artifactDirectory, $"{testName}.zip"),
            });
        }
        else
        {
            await Context.Tracing.StopAsync();
        }

        _client.Dispose();
    }
}

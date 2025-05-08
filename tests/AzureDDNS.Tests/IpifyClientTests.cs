using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureDDNS.Tests;

public class IpifyClientTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task GetAsync_Works()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddXUnit(outputHelper));
        services.AddHttpClient<IpifyClient>();
        var root = services.BuildServiceProvider();

        using var scope = root.CreateScope();
        var provider = scope.ServiceProvider;
        var client = provider.GetRequiredService<IpifyClient>();

        var ip = await client.GetAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(ip);

        var logger = provider.GetRequiredService<ILogger<IpifyClientTests>>();
        logger.LogInformation("IP: {Ip}", ip);
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using SC = AzureDDNS.AzureDDNSSerializerContext;

namespace AzureDDNS.Tests;

public class AzDdnsConfigTests
{
    [Fact]
    public void Deserialize_Works()
    {
        var json = new JsonObject
        {
            ["subscription"] = "MAXWELLWERU",
            ["resourceGroupName"] = "infra",
            ["zoneName"] = "maxwellweru.io",
            ["recordName"] = "office",
            ["ttl"] = 7200,
            ["interval"] = 1800,
            ["dryRun"] = true,
        };
        var config = JsonSerializer.Deserialize(json, SC.Default.AzDdnsConfig);
        Assert.NotNull(config);
        Assert.Equal("MAXWELLWERU", config.Subscription);
        Assert.Equal("infra", config.ResourceGroupName);
        Assert.Equal("maxwellweru.io", config.ZoneName);
        Assert.Equal("office", config.RecordName);
        Assert.Equal(7200, config.Ttl);
        Assert.Equal(1800, config.Interval);
        Assert.True(config.DryRun);
    }

    [Fact]
    public void Deserialize_Works_WithDefaults()
    {
        var json = new JsonObject
        {
            ["subscription"] = "MAXWELLWERU",
            ["resourceGroupName"] = "infra",
            ["zoneName"] = "maxwellweru.io",
            ["recordName"] = "office",
        };
        var config = JsonSerializer.Deserialize(json, SC.Default.AzDdnsConfig);
        Assert.NotNull(config);
        Assert.Equal("MAXWELLWERU", config.Subscription);
        Assert.Equal("infra", config.ResourceGroupName);
        Assert.Equal("maxwellweru.io", config.ZoneName);
        Assert.Equal("office", config.RecordName);
        Assert.Equal(3600, config.Ttl);
        Assert.Equal(900, config.Interval);
        Assert.False(config.DryRun);
    }
}

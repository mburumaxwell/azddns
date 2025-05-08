using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using SC = AzureDDNS.AzureDDNSSerializerContext;

namespace AzureDDNS;

internal class Updater(IpifyClient ipifyClient, ILogger<UpdateCommand> logger)
{
    public virtual async Task<int> UpdateAsync(string configFile, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configFile))
        {
            logger.LogError("Config file '{ConfigFile}' not found", configFile);
            return -1;
        }

        AzDdnsConfig? config;
        try
        {
            await using var stream = File.OpenRead(configFile);
            config = await JsonSerializer.DeserializeAsync(stream, SC.Default.AzDdnsConfig, cancellationToken);
        }
        catch (JsonException je)
        {
            logger.LogError(je, "Config file contains invalid JSON");
            return -1;
        }

        if (config is null)
        {
            logger.LogError("Deserialized config is null which is unexpected!");
            return -1;
        }

        return await UpdateAsync(config, interactive: false, cancellationToken);
    }

    public virtual async Task<int> UpdateAsync(AzDdnsConfig config, bool interactive = false, CancellationToken cancellationToken = default)
    {
        var interval = config.Interval;

        // if we do not have an interval, execute once and exit
        if (interval is null or < 0) return await UpdateInnerAsync(config, interactive, cancellationToken);

        // we have an interval, we execute the first time the delay in a loop
        do
        {
            await UpdateInnerAsync(config, interactive, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(interval.Value), cancellationToken);
        } while (!cancellationToken.IsCancellationRequested);
        return 0;
    }

    internal virtual async Task<int> UpdateInnerAsync(AzDdnsConfig config, bool interactive = false, CancellationToken cancellationToken = default)
    {
        // prepare client and credential
        var credential = new DefaultAzureCredential(includeInteractiveCredentials: interactive);
        var armClient = new ArmClient(credential);

        // find the subscription
        var (subscriptionId, resourceGroupName, zoneName, recordName, ttl, _, dryRun) = config;
        var hasSubscriptionId = !string.IsNullOrEmpty(subscriptionId);
        var subs = armClient.GetSubscriptions().GetAllAsync(cancellationToken);
        Func<Azure.ResourceManager.Resources.SubscriptionResource, bool> predicate = hasSubscriptionId
            ? (sub => string.Equals(sub.Data.Id, subscriptionId, StringComparison.OrdinalIgnoreCase)
                   || string.Equals(sub.Data.DisplayName, subscriptionId, StringComparison.OrdinalIgnoreCase))
            : (_ => true);
        var subscription = await subs.FirstOrDefaultAsync(predicate, cancellationToken);
        if (subscription is null)
        {
            if (hasSubscriptionId) logger.LogError("Could not find subscription '{SubscriptionIdOrName}", subscriptionId);
            else logger.LogError("There are no available subscriptions");
            return -1;
        }
        logger.LogTrace("Found {SubscriptionId}", subscription.Data.Id);

        // fetch the resource group
        Azure.ResourceManager.Resources.ResourceGroupResource resourceGroup;
        try
        {
            resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName, cancellationToken);
        }
        catch (Azure.RequestFailedException rfe) when (rfe.Status is 404)
        {
            logger.LogError("Resource group named '{ResourceGroupName}' does not exist in {SubscriptionId}",
                            resourceGroupName,
                            subscription.Data.Id);
            return -1;
        }
        logger.LogTrace("Found {ResourceGroupId}", resourceGroup.Data.Id);

        // fetch the zone
        DnsZoneResource zone;
        try
        {
            zone = await resourceGroup.GetDnsZoneAsync(zoneName, cancellationToken);
        }
        catch (Azure.RequestFailedException rfe) when (rfe.Status is 404)
        {
            logger.LogError("DNS Zone named '{ZoneName}' does not exist in '{ResourceGroupId}'", zoneName, resourceGroup.Id);
            return -1;
        }
        logger.LogTrace("Found {DnsZoneId}", zone.Data.Id);

        // fetch the IP value
        var ipifyResult = await ipifyClient.GetAsync(cancellationToken);
        if (ipifyResult is null)
        {
            logger.LogError("Unable to fetch current IP");
            return -1;
        }

        // if the IP is v4 mapped to v6 then undo the mapping
        var ip = ipifyResult.IP;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        logger.LogInformation("Current IP is {IPAddress}", ip);

        // prepare metadata
        var machine = System.Net.Dns.GetHostName();
        var tool = $"azddns/{VersioningHelper.ProductVersion}";

        // fetch existing records
        DnsAaaaRecordResource? aaaaRecord = null;
        DnsARecordResource? aRecord = null;
        try { aaaaRecord = await zone.GetDnsAaaaRecordAsync(recordName, cancellationToken); }
        catch (Azure.RequestFailedException rfe) when (rfe.Status is 404) { }
        try { aRecord = await zone.GetDnsARecordAsync(recordName, cancellationToken); }
        catch (Azure.RequestFailedException rfe) when (rfe.Status is 404) { }

        // update the records
        if (ip.AddressFamily is System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // remove A record if it exists
            if (aRecord is not null)
            {
                logger.LogTrace("Removing {ResourceId} ...", aRecord.Id);
                if (!dryRun)
                {
                    await aRecord.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken: cancellationToken);
                    logger.LogDebug("Removed {ResourceId}", aRecord.Id);
                }
                else logger.LogDebug("Removed {ResourceId} (dry run)", aRecord.Id);
            }

            // if the existing AAAA record is the same, no need to update
            if (aaaaRecord is not null
                && aaaaRecord.Data.DnsAaaaRecords.Count == 1
                && ip.Equals(aaaaRecord.Data.DnsAaaaRecords[0].IPv6Address)) // using '==' does not work
            {
                logger.LogInformation("{RecordName}.{ZoneName} is up to date", recordName, zoneName);
                return 0;
            }

            // create/update AAAA record
            logger.LogDebug("Creating or updating AAAA record ...");
            if (!dryRun)
            {
                var data = new DnsAaaaRecordData
                {
                    DnsAaaaRecords = { new Azure.ResourceManager.Dns.Models.DnsAaaaRecordInfo { IPv6Address = ip } },
                    Metadata = { [nameof(machine)] = machine, [nameof(tool)] = tool },
                    TtlInSeconds = ttl,
                };
                await zone.GetDnsAaaaRecords()
                          .CreateOrUpdateAsync(Azure.WaitUntil.Completed,
                                               recordName,
                                               data,
                                               cancellationToken: cancellationToken);
                logger.LogInformation("Created or updated {RecordName}.{ZoneName}", recordName, zoneName);
            }
            else logger.LogInformation("Created or updated {RecordName}.{ZoneName} (dry run)", recordName, zoneName);
        }
        else
        {
            // remove AAAA record if it exists
            if (aaaaRecord is not null)
            {
                logger.LogTrace("Removing {ResourceId} ...", aaaaRecord.Id);
                if (!dryRun)
                {
                    await aaaaRecord.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken: cancellationToken);
                    logger.LogDebug("Removed {ResourceId}", aaaaRecord.Id);
                }
                else logger.LogDebug("Removed {ResourceId} (dry run)", aaaaRecord.Id);
            }

            // if the existing A record is the same, no need to update
            if (aRecord is not null
                && aRecord.Data.DnsARecords.Count == 1
                && ip.Equals(aRecord.Data.DnsARecords[0].IPv4Address)) // using '==' does not work
            {
                logger.LogInformation("{RecordName}.{ZoneName} is up to date", recordName, zoneName);
                return 0;
            }

            // create/update A record
            logger.LogDebug("Creating or updating A record ...");
            if (!dryRun)
            {
                var data = new DnsARecordData
                {
                    DnsARecords = { new Azure.ResourceManager.Dns.Models.DnsARecordInfo { IPv4Address = ip } },
                    Metadata = { [nameof(machine)] = machine, [nameof(tool)] = tool },
                    TtlInSeconds = ttl,
                };
                await zone.GetDnsARecords()
                          .CreateOrUpdateAsync(Azure.WaitUntil.Completed,
                                               recordName,
                                               data,
                                               cancellationToken: cancellationToken);
                logger.LogInformation("Created or updated {RecordName}.{ZoneName}", recordName, zoneName);
            }
            else logger.LogInformation("Created or updated {RecordName}.{ZoneName} (dry run)", recordName, zoneName);
        }

        return 0;
    }
}

/// <param name="Subscription"> Name or ID of the subscription.</param>
/// <param name="ResourceGroupName"> Name of the resource group.</param>
/// <param name="ZoneName"> Name of the DNS zone.</param>
/// <param name="RecordName"> Name of the DNS record.</param>
/// <param name="Ttl">TTL (time-to-live, seconds) of the DNS record.</param>
/// <param name="Interval">Interval (seconds) to check for updates.</param>
/// <param name="DryRun">Test the logic without actually updating the DNS records.</param>
internal record AzDdnsConfig(
    [property: JsonPropertyName("subscription")] string Subscription,
    [property: JsonPropertyName("resourceGroupName")] string ResourceGroupName,
    [property: JsonPropertyName("zoneName")] string ZoneName,
    [property: JsonPropertyName("recordName")] string RecordName,
    [property: JsonPropertyName("ttl")] int Ttl = 3600,
    [property: JsonPropertyName("interval")] int? Interval = 900,
    [property: JsonPropertyName("dryRun")] bool DryRun = false)
{
}

namespace AzureDDNS;

internal class UpdateCommand : Command
{
    private readonly IHost host;

    private readonly Option<bool> interactiveOption;
    private readonly Option<string?> subscriptionIdOption;
    private readonly Option<string> resourceGroupNameOption, zoneNameOption, recordNameOption;
    private readonly Option<int> ttlOption;
    private readonly Option<int?> intervalOption;
    private readonly Option<bool> dryRunOption;

    public UpdateCommand(IHost host) : base("update", "Update once or at an interval")
    {
        ArgumentNullException.ThrowIfNull(this.host = host, nameof(host));

        Add(interactiveOption = new Option<bool>(name: "--interactive")
        {
            Description = "Allow interactive authentication mode (opens a browser for authentication).",
            DefaultValueFactory = _ => false,
        });

        Add(subscriptionIdOption = new Option<string?>(name: "--subscription", aliases: ["-s"])
        {
            Description = "Name or ID of subscription. If none is provided, the first available subscription is used.",
        });

        Add(resourceGroupNameOption = new Option<string>(name: "--resource-group", aliases: ["-g"])
        {
            Description = "Name of the resource group containing the DNS zone.",
            Required = true,
        });

        Add(zoneNameOption = new Option<string>(name: "--zone", aliases: ["-z"])
        {
            Description = "The name of the DNS zone (without a terminating dot)",
            Required = true,
        });

        Add(recordNameOption = new Option<string>(name: "--record", aliases: ["-r"])
        {
            Description = "The name of the record set, relative to the name of the DNS zone.",
            Required = true,
        });

        Add(ttlOption = new Option<int>(name: "--ttl", aliases: ["-t"])
        {
            Description = "The TTL (time-to-live, seconds) of the DNS record.",
            DefaultValueFactory = _ => 3600, // 1 hour in seconds
        });

        Add(intervalOption = new Option<int?>(name: "--interval", aliases: ["-i"])
        {
            Description = "The interval (seconds) to check for updates. If not provided, the tool runs continuously unless stopped.",
            // TODO; make sure it is at least 5 minutes but less than ttl
        });

        Add(dryRunOption = new Option<bool>(name: "--dry-run")
        {
            Description = "Test the logic without actually updating the DNS records.",
            DefaultValueFactory = _ => false,
        });

        SetAction(ExecuteAsync);
    }

    private Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var interactive = parseResult.GetValue(interactiveOption);
        var subscriptionId = parseResult.GetValue(subscriptionIdOption);
        var resourceGroupName = parseResult.GetValue(resourceGroupNameOption);
        var zoneName = parseResult.GetValue(zoneNameOption);
        var recordName = parseResult.GetValue(recordNameOption);
        var ttl = parseResult.GetValue(ttlOption);
        var interval = parseResult.GetValue(intervalOption);
        var dryRun = parseResult.GetValue(dryRunOption);
        var config = new AzDdnsConfig(subscriptionId!, resourceGroupName!, zoneName!, recordName!, ttl, interval, dryRun);

        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var updater = provider.GetRequiredService<Updater>();
        return updater.UpdateAsync(config, interactive, cancellationToken);
    }
}

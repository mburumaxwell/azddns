namespace AzureDDNS;

internal class RunCommand : Command
{
    private readonly IHost host;

    private readonly Option<string> configFileOption;

    public RunCommand(IHost host) : base("run", "Run using a configuration file.")
    {
        ArgumentNullException.ThrowIfNull(this.host = host, nameof(host));

        Add(configFileOption = new Option<string>(name: "--config", aliases: ["-f"])
        {
            Description = "Path to the configuration file",
            Required = true,
        });

        SetAction(ExecuteAsync);
    }

    private Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var configFile = parseResult.GetValue(configFileOption)!;

        using var scope = host.Services.CreateScope();
        var provider = scope.ServiceProvider;
        var updater = provider.GetRequiredService<Updater>();
        return updater.UpdateAsync(configFile, cancellationToken);
    }
}

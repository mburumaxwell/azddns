using AzureDDNS;

var builder = Host.CreateApplicationBuilder();

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Logging:LogLevel:Default"] = "Information",
    ["Logging:LogLevel:Microsoft"] = "Warning",
    ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
    ["Logging:Debug:LogLevel:Default"] = "None",

    ["Logging:LogLevel:AzureDDNS"] = builder.Environment.IsDevelopment() ? "Trace" : "Information",
    ["Logging:LogLevel:System.Net.Http.HttpClient"] = "None", // removes all, add what we need later

    ["Logging:Console:FormatterName"] = "cli",
    ["Logging:Console:FormatterOptions:SingleLine"] = "True",
    ["Logging:Console:FormatterOptions:IncludeCategory"] = "False",
    ["Logging:Console:FormatterOptions:IncludeEventId"] = "False",
    ["Logging:Console:FormatterOptions:TimestampFormat"] = "yyyy-MM-dd HH:mm:ss ",
});

// configure logging
builder.Logging.AddCliConsole();

// register services
builder.Services.AddHttpClient<IpifyClient>();
builder.Services.AddTransient<Updater>();

// build and start the host
using var host = builder.Build();
await host.StartAsync();

// prepare the root command
var root = new RootCommand("Dynamic DNS tool to Azure")
{
    new UpdateCommand(host),
    new RunCommand(host),
};
var configuration = new CommandLineConfiguration(root);

// execute the command
try
{
    return await configuration.InvokeAsync(args);
}
finally
{
    // stop the host, this will stop and dispose the services which flushes OpenTelemetry data
    await host.StopAsync();
}

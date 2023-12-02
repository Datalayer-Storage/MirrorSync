using DlMirrorSync;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using chia.dotnet;

// the only expected cli arg is an optional path to a chia config file
// if present add it to the config with the key 'chia_config_path'
if (args.Length == 1)
{
    // just modifying the args itself since that gets passed through to the builder
    args = [$"chia_config_path={args.First()}"];
}
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

if (OperatingSystem.IsWindows())
{
    LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);
}

builder.Services
    .AddWindowsService(options =>
    {
        options.ServiceName = "Data Layer Mirror Sync Service";
    })
    .AddHostedService<SyncPollingService>()
    .AddSingleton<ChiaService>()
    .AddSingleton<MirrorService>()
    .AddSingleton<RpcClientHost>()
    .AddSingleton<ChiaConfig>()
    .AddSingleton<SyncService>()
    .AddSingleton<DnsService>()
    .AddSingleton((provider) => new DataLayerProxy(provider.GetRequiredService<RpcClientHost>().GetRpcClient("data_layer"), "DlMirrorSync"))
    .AddSingleton((provider) => new FullNodeProxy(provider.GetRequiredService<RpcClientHost>().GetRpcClient("full_node"), "DlMirrorSync"))
    .AddSingleton((provider) => new WalletProxy(provider.GetRequiredService<RpcClientHost>().GetRpcClient("wallet"), "DlMirrorSync"));

var host = builder.Build();
host.Run();

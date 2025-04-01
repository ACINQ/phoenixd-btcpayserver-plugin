using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BTCPayServer.Lightning;
using BTCPayServer.Lightning.Phoenixd;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Abstractions.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BTCPayServer.Plugins.Phoenixd;

public class PhoenixdPlugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.0" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "PhoenixdNavItem");
        services.AddSingleton<ILightningConnectionStringHandler>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();
            return new PhoenixdConnectionStringHandler(httpClient);
        });
    }
}

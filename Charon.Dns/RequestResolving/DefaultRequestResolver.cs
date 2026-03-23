using Charon.Dns.Settings;
using Serilog;

namespace Charon.Dns.RequestResolving
{
    public class DefaultRequestResolver(
        DnsChainSettings dnsChainSettings,
        ILogger logger) 
        : RequestResolverBase(
                dnsChainSettings.DefaultServers,
                logger), 
            IDefaultRequestResolver;
}

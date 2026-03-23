using Charon.Dns.Settings;
using Serilog;

namespace Charon.Dns.RequestResolving
{
    public class SafeRequestResolver(
        DnsChainSettings dnsChainSettings,
        ILogger logger) 
        : RequestResolverBase(
                dnsChainSettings.SecuredServers.Select(x => x.Ip),
                logger), 
            ISafeRequestResolver;
}

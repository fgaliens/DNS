using System.Diagnostics;
using System.Net;
using Charon.Dns.Lib.Client.RequestResolver;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Settings;
using Serilog;

namespace Charon.Dns.RequestResolving
{
    public class SafeRequestResolver : ISafeRequestResolver
    {
        private readonly ILogger _logger;
        private readonly UdpRequestResolver[] _innerResolvers;
        private const int DefaultDnsPort = 53; 
        
        public SafeRequestResolver(
            DnsChainSettings dnsChainSettings,
            ILogger logger)
        {
            _logger = logger;
            _innerResolvers = dnsChainSettings
                .SecuredServers
                .Select(x => new UdpRequestResolver(new IPEndPoint(x.Ip, DefaultDnsPort), logger: logger))
                .ToArray();
        }

        public async Task<IResponse> Resolve(IRequest request, IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default)
        {
            _logger.Debug("Resolving {@Request} safely", request);
            
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var randomResolver = _innerResolvers[Random.Shared.Next(_innerResolvers.Length)];
                return await randomResolver.Resolve(request, remoteEndPoint, cancellationToken);
            }
            finally
            {
                _logger.Debug(
                    "{Source}: request resolved by chain in {ElapsedMilliseconds}", 
                    nameof(SafeRequestResolver), 
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}

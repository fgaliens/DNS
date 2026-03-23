using System.Diagnostics;
using System.Net;
using Charon.Dns.Lib.Client.RequestResolver;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Settings;
using Serilog;

namespace Charon.Dns.RequestResolving
{
    public class DefaultRequestResolver : IDefaultRequestResolver
    {
        private const int DefaultDnsPort = 53; 
        
        private readonly UdpRequestResolver[] _innerResolvers;
        private readonly ILogger _logger;
        
        public DefaultRequestResolver(
            DnsChainSettings dnsChainSettings,
            ILogger logger)
        {
            _logger = logger;
            _innerResolvers = dnsChainSettings
                .DefaultServers
                .Select(x => new UdpRequestResolver(new IPEndPoint(x, DefaultDnsPort), logger: logger))
                .ToArray();
        }

        public async Task<IResponse> Resolve(IRequest request, IPEndPoint remoteEndPoint, CancellationToken cancellationToken = default)
        {
            _logger.Debug("Resolving {@Request} by default", request);

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
                    nameof(DefaultRequestResolver), 
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }
}

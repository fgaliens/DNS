using System.Diagnostics;
using System.Net;
using Charon.Dns.Lib.Client.RequestResolver;
using Charon.Dns.Lib.Protocol;
using Serilog;

namespace Charon.Dns.RequestResolving;

public class RequestResolverBase : IRequestResolver
{
    private const int DefaultDnsPort = 53; 
        
    private readonly UdpRequestResolver[] _innerResolvers;
    private readonly ILogger _logger;
        
    public RequestResolverBase(
        IEnumerable<IPAddress> chainDnsServers,
        ILogger logger)
    {
        _logger = logger;
        _innerResolvers = chainDnsServers
            .Select(x => new UdpRequestResolver(new IPEndPoint(x, DefaultDnsPort), logger: logger))
            .ToArray();
    }

    public async Task<IResponse> Resolve(
        IRequest request, 
        IPEndPoint remoteEndPoint, 
        CancellationToken cancellationToken = default)
    {
        _logger.Debug("Resolving {@Request} safely", request);
            
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var responseTasks = _innerResolvers.Select(x => 
                x.Resolve(request, remoteEndPoint, cancellationToken));
            var response = await Task.WhenAny(responseTasks);
            return await response;
        }
        finally
        {
            _logger.Debug(
                "{Source}: request resolved by chain in {ElapsedMilliseconds} ms.", 
                nameof(SafeRequestResolver), 
                stopwatch.ElapsedMilliseconds);
        }
    }
}

using System.Net;
using Charon.Dns.Lib.AsyncEvents;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Net;
using Charon.Dns.RequestResolving;
using Charon.Dns.Routing;
using Serilog;

namespace Charon.Dns.Interceptors
{
    public class ResponseInterceptor(
        IHostNameAnalyzer hostNameAnalyzer,
        IRouteManager<IpV4Network> ipV4NetworkManager,
        IRouteManager<IpV6Network> ipV6NetworkManager,
        ILogger logger) 
        : IResponseInterceptor
    {
        public async Task Handle(
            IRequest request, 
            IResponse response,
            IPEndPoint remoteEndPoint,
            CancellationToken token = default)
        {
            logger.Debug("Intercepting request from {RemoteIp}", remoteEndPoint);

            if (response.Truncated)
            {
                logger.Warning("Response {@Response} for request {@Request} has been truncated", response, request);
            }
            
            var previousHostNameWasSecured = false;
            Domain? previousHostName = null;
            SecuredConnectionParams? connectionParams = null;

            List<RouteBatchItem<IpV4Network>>? ipV4NetworksToSecure = null;
            List<RouteBatchItem<IpV6Network>>? ipV6NetworksToSecure = null;
            
            foreach (var answer in response.AnswerRecords)
            {
                var shouldBeSecured = false;

                if (answer.Name.Equals(previousHostName))
                {
                    shouldBeSecured = previousHostNameWasSecured;
                }
                else
                {
                    shouldBeSecured = hostNameAnalyzer.ShouldBeSecured(answer.Name.ToString(), out connectionParams);
                    previousHostName = answer.Name;
                    previousHostNameWasSecured = shouldBeSecured;
                }
                
                if (shouldBeSecured)
                {
                    if (answer.Type is RecordType.A)
                    {
                        var ipV4Network = new IpV4Network(answer.Data, connectionParams!.IpV4RoutingSubnet);
                        
                        ipV4NetworksToSecure ??= new(response.AnswerRecords.Count);
                        ipV4NetworksToSecure.Add(new()
                        {
                            Ip = ipV4Network,
                            Interface = connectionParams.InterfaceName,
                        });
                    }
                    else if (answer.Type is RecordType.AAAA)
                    {
                        var ipV6Network = new IpV6Network(answer.Data, connectionParams!.IpV6RoutingSubnet);
                        
                        ipV6NetworksToSecure ??= new(response.AnswerRecords.Count);
                        ipV6NetworksToSecure.Add(new()
                        {
                            Ip = ipV6Network,
                            Interface = connectionParams.InterfaceName,
                        });
                    }
                }
            }

            if (ipV4NetworksToSecure is { Count: > 0 })
            {
                await ipV4NetworkManager.AddRoutesBatch(ipV4NetworksToSecure);
            }
            
            if (ipV6NetworksToSecure is { Count: > 0 })
            {
                await ipV6NetworkManager.AddRoutesBatch(ipV6NetworksToSecure);
            }
        }

        async Task IAsyncObserver<OnResponseEventArgs>.OnEvent(OnResponseEventArgs eventArgs)
        {
            await Handle(eventArgs.Request, eventArgs.Response, eventArgs.Remote);
        }

        Task IAsyncObserver<OnResponseEventArgs>.OnCompleted()
        {
            return Task.CompletedTask;
        }
    }
}

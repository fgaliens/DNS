using System.Net;
using Charon.Dns.Lib.Protocol;
using Charon.Dns.Lib.Tracing;

namespace Charon.Dns.Lib.AsyncEvents;

public readonly record struct OnResponseEventArgs
{
    public required IRequest Request { get; init; }
    public required IResponse Response { get; init; }
    public required RequestTrace Trace { get; init; }
}
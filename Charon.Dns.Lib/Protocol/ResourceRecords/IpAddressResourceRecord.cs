using System;
using System.Net;

namespace Charon.Dns.Lib.Protocol.ResourceRecords
{
    public class IpAddressResourceRecord : BaseResourceRecord
    {
        private static IResourceRecord Create(Domain domain, IPAddress ip, TimeSpan ttl)
        {
            byte[] data = ip.GetAddressBytes();
            RecordType type = data.Length == 4 ? RecordType.A : RecordType.AAAA;

            return new ResourceRecord(domain, data, type, RecordClass.IN, ttl);
        }

        public IpAddressResourceRecord(IResourceRecord record) : base(record)
        {
            IpAddress = new IPAddress(Data);
        }

        public IpAddressResourceRecord(Domain domain, IPAddress ip, TimeSpan ttl = default(TimeSpan)) :
            base(Create(domain, ip, ttl))
        {
            IpAddress = ip;
        }

        public IPAddress IpAddress { get; }

        public override string ToString()
        {
            return Stringify().Add(nameof(IpAddress)).ToString();
        }
    }
}

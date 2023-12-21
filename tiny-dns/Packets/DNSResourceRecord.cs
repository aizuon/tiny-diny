using System.Net;

namespace TinyDNS.Packets;

public record DNSResourceRecord : IDeserializable<DNSResourceRecord>
{
    public string Name { get; set; }
    public ushort Type { get; set; }
    public ushort Class { get; set; }
    public uint TTL { get; set; }
    public byte[] RData { get; set; }
    public object ParsedRData { get; set; }

    public static DNSResourceRecord Deserialize(BinaryBuffer buffer)
    {
        var authority = new DNSResourceRecord();

        string _name = string.Empty;
        if (!buffer.ReadDomainName(ref _name))
            return null;
        authority.Name = _name;

        ushort _type = 0;
        if (!buffer.Read(ref _type))
            return null;
        authority.Type = _type;

        ushort _class = 0;
        if (!buffer.Read(ref _class))
            return null;
        authority.Class = _class;

        uint _ttl = 0;
        if (!buffer.Read(ref _ttl))
            return null;
        authority.TTL = _ttl;

        ushort _rdLength = 0;
        if (!buffer.Read(ref _rdLength))
            return null;

        byte[] _rdata = new byte[_rdLength];
        if (!buffer.ReadRaw(ref _rdata, _rdLength))
            return null;
        authority.RData = _rdata;
        buffer.ReadOffset -= _rdLength;
        authority.ParseRData(buffer);
        if (authority.ParsedRData == null)
            buffer.ReadOffset += _rdLength;

        return authority;
    }

    private void ParseRData(BinaryBuffer buffer)
    {
        switch (Type)
        {
            case 1:
                ParsedRData = ParseARecord(buffer);
                break;
            case 2:
                ParsedRData = ParseNSRecord(buffer);
                break;
        }
    }

    private static IPAddress ParseARecord(BinaryBuffer buffer)
    {
        byte[] octets = Array.Empty<byte>();
        if (!buffer.ReadRaw(ref octets, 4))
            return null;

        return new IPAddress(octets);
    }

    private static string ParseNSRecord(BinaryBuffer buffer)
    {
        string nsDomainName = string.Empty;
        if (!buffer.ReadDomainName(ref nsDomainName))
            return null;

        return nsDomainName;
    }
}

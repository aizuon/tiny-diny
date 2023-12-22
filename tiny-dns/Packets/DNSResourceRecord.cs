using System.Net;
using TinyDNS.Serialization;

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

        authority.Name = buffer.ReadDomainName();
        ;
        authority.Type = buffer.Read<ushort>();

        authority.Class = buffer.Read<ushort>();

        authority.TTL = buffer.Read<uint>();

        ushort rdLength = buffer.Read<ushort>();
        authority.RData = buffer.ReadRaw<byte>(rdLength);
        buffer.ReadOffset -= rdLength;
        authority.ParseRData(buffer);
        if (authority.ParsedRData == null)
            buffer.ReadOffset += rdLength;

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
        byte[] octets = buffer.ReadRaw<byte>(4);

        return new IPAddress(octets);
    }

    private static string ParseNSRecord(BinaryBuffer buffer)
    {
        string nsDomainName = buffer.ReadDomainName();

        return nsDomainName;
    }
}

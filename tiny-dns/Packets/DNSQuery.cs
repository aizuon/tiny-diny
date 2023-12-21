namespace TinyDNS.Packets;

public record DNSQuery : ISerializable
{
    public DNSHeader Header { get; set; }
    public DNSQuestion Question { get; set; }
    public EDNSOption EDNSOption { get; set; } = new EDNSOption();

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        if (EDNSOption != null)
            Header.AdditionalRRs = 1;
        buffer.WriteRaw(Header.Serialize().Buffer);

        buffer.WriteRaw(Question.Serialize().Buffer);

        if (EDNSOption != null)
            buffer.WriteRaw(EDNSOption.Serialize().Buffer);

        return buffer;
    }
}

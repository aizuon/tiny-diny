namespace TinyDNS.Packets;

public record DNSQuestion : ISerializable, IDeserializable<DNSQuestion>
{
    public string QName { get; set; }
    public ushort QType { get; set; } = 1;
    public ushort QClass { get; set; } = 1;

    public static DNSQuestion Deserialize(BinaryBuffer buffer)
    {
        var question = new DNSQuestion();

        string qname = string.Empty;
        if (!buffer.ReadDomainName(ref qname))
            return null;
        question.QName = qname;

        ushort qtype = 0;
        if (!buffer.Read(ref qtype))
            return null;
        question.QType = qtype;

        ushort qclass = 0;
        if (!buffer.Read(ref qclass))
            return null;
        question.QClass = qclass;

        return question;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.WriteDomainName(QName);
        buffer.Write(QType);
        buffer.Write(QClass);

        return buffer;
    }
}

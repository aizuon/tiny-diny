using TinyDNS.Serialization;

namespace TinyDNS.Packets;

public record DNSQuestion : ISerializable, IDeserializable<DNSQuestion>
{
    public string QName { get; set; }
    public ushort QType { get; set; } = 1;
    public ushort QClass { get; set; } = 1;

    public static DNSQuestion Deserialize(BinaryBuffer buffer)
    {
        var question = new DNSQuestion();

        question.QName = buffer.ReadDomainName();

        question.QType = buffer.Read<ushort>();

        question.QClass = buffer.Read<ushort>();

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

using TinyDNS.Serialization;

namespace TinyDNS.Packets;

public record DNSHeader : ISerializable, IDeserializable<DNSHeader>
{
    public ushort Id { get; set; } = (ushort)Random.Shared.Next(ushort.MinValue, ushort.MaxValue + 1);
    public byte QR { get; set; }
    public byte Opcode { get; set; }
    public byte AA { get; set; }
    public byte TC { get; set; }
    public byte RD { get; set; } = 1;
    public byte RA { get; set; }
    public byte Z { get; set; }
    public byte RCode { get; set; }
    public ushort Questions { get; set; } = 1;
    public ushort AnswerRRs { get; set; }
    public ushort AuthorityRRs { get; set; }
    public ushort AdditionalRRs { get; set; }

    public static DNSHeader Deserialize(BinaryBuffer buffer)
    {
        var header = new DNSHeader();

        ushort id = buffer.Read<ushort>();

        header.Id = id;
        byte[] flagsBuffer = buffer.ReadRaw<byte>(2);

        var flags = new BitBuffer(flagsBuffer);
        header.QR = flags.Read<byte>(1);
        header.Opcode = flags.Read<byte>(4);
        header.AA = flags.Read<byte>(1);
        header.TC = flags.Read<byte>(1);
        header.RD = flags.Read<byte>(1);
        header.RA = flags.Read<byte>(1);
        header.Z = flags.Read<byte>(3);
        header.RCode = flags.Read<byte>(4);

        header.Questions = buffer.Read<ushort>();

        header.AnswerRRs = buffer.Read<ushort>();

        header.AuthorityRRs = buffer.Read<ushort>();

        header.AdditionalRRs = buffer.Read<ushort>();

        return header;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.Write(Id);

        var flags = new BitBuffer();
        flags.Write(QR, 1);
        flags.Write(Opcode, 4);
        flags.Write(AA, 1);
        flags.Write(TC, 1);
        flags.Write(RD, 1);
        flags.Write(RA, 1);
        flags.Write(Z, 3);
        flags.Write(RCode, 4);
        buffer.WriteRaw(flags.Buffer);

        buffer.Write(Questions);
        buffer.Write(AnswerRRs);
        buffer.Write(AuthorityRRs);
        buffer.Write(AdditionalRRs);

        return buffer;
    }
}

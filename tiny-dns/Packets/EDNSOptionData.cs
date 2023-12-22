using TinyDNS.Serialization;

namespace TinyDNS.Packets;

public record EDNSOptionData : ISerializable, IDeserializable<EDNSOptionData>
{
    public ushort Code { get; set; }
    public byte[] Data { get; set; }

    public static EDNSOptionData Deserialize(BinaryBuffer buffer)
    {
        var optionData = new EDNSOptionData();

        optionData.Code = buffer.Read<ushort>();

        ushort dataLength = buffer.Read<ushort>();

        optionData.Data = buffer.ReadRaw<byte>(dataLength);

        return optionData;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.Write(Code);

        buffer.Write((ushort)Data.Length);

        buffer.WriteRaw(Data);

        return buffer;
    }
}

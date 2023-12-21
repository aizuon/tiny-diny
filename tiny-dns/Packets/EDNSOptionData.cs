namespace TinyDNS.Packets;

public record EDNSOptionData : ISerializable, IDeserializable<EDNSOptionData>
{
    public ushort Code { get; set; }
    public byte[] Data { get; set; }

    public static EDNSOptionData Deserialize(BinaryBuffer buffer)
    {
        var optionData = new EDNSOptionData();

        ushort code = 0;
        if (!buffer.Read(ref code))
            return null;
        optionData.Code = code;

        ushort dataLength = 0;
        if (!buffer.Read(ref dataLength))
            return null;

        byte[] data = new byte[dataLength];
        if (!buffer.ReadRaw(ref data, dataLength))
            return null;
        optionData.Data = data;

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

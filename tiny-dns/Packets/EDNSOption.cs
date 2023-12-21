namespace TinyDNS.Packets;

public record EDNSOption : ISerializable, IDeserializable<EDNSOption>
{
    public ushort UDPSize { get; set; } = 4096;
    public byte ExtendedRCode { get; set; }
    public byte Version { get; set; }
    public ushort Flags { get; set; }
    public List<EDNSOptionData> Options { get; set; } = new List<EDNSOptionData>();

    public static EDNSOption Deserialize(BinaryBuffer buffer)
    {
        var ednsOption = new EDNSOption();

        byte name = 0;
        if (!buffer.Read(ref name) || name != 0)
            return null;

        ushort type = 0;
        if (!buffer.Read(ref type) || type != 41)
            return null;

        ushort udpSize = 0;
        if (!buffer.Read(ref udpSize))
            return null;
        ednsOption.UDPSize = udpSize;

        uint ttl = 0;
        if (!buffer.Read(ref ttl))
            return null;

        ednsOption.ExtendedRCode = (byte)((ttl >> 24) & 0xFF);
        ednsOption.Version = (byte)((ttl >> 16) & 0xFF);
        ednsOption.Flags = (ushort)(ttl & 0xFFFF);

        ushort rdLength = 0;
        if (!buffer.Read(ref rdLength))
            return null;

        if (rdLength > 0)
        {
            uint endPosition = buffer.ReadOffset + rdLength;
            while (buffer.ReadOffset < endPosition)
            {
                var option = EDNSOptionData.Deserialize(buffer);
                if (option == null)
                    return null;
                ednsOption.Options.Add(option);
            }
        }

        return ednsOption;
    }

    public BinaryBuffer Serialize()
    {
        var buffer = new BinaryBuffer();

        buffer.Write((byte)0);

        buffer.Write((ushort)41);

        buffer.Write(UDPSize);

        uint ttl = (uint)((ExtendedRCode << 24) | (Version << 16) | Flags);
        buffer.Write(ttl);

        ushort rdLength = (ushort)Options.Sum(option => 4 + option.Data.Length);
        buffer.Write(rdLength);

        foreach (var option in Options)
            buffer.WriteRaw(option.Serialize().Buffer);

        return buffer;
    }
}

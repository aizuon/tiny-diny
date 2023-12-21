namespace TinyDNS.Packets;

public interface ISerializable
{
    public BinaryBuffer Serialize();
}

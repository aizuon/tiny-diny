using TinyDNS.Serialization;

namespace TinyDNS.Packets;

public interface IDeserializable<out T>
{
    public static abstract T Deserialize(BinaryBuffer buffer);
}

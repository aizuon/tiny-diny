using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyDNS.Serialization;

public static class Mem
{
    public static unsafe T ToBigEndian<T>(T obj) where T : unmanaged, IBinaryNumber<T>
    {
        if (!BitConverter.IsLittleEndian)
            return obj;

        uint size = (uint)Unsafe.SizeOf<T>();
        if (size == 1)
            return obj;

        Span<byte> bytes = stackalloc byte[(int)size];
        ref byte objRef = ref Unsafe.As<T, byte>(ref obj);

        Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(bytes), ref objRef, size);

        bytes.Reverse();

        return MemoryMarshal.Read<T>(bytes);
    }
}

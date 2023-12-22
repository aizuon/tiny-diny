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

        int size = Unsafe.SizeOf<T>();
        Span<byte> bytes = stackalloc byte[size];
        ref byte objRef = ref Unsafe.As<T, byte>(ref obj);

        Unsafe.CopyBlockUnaligned(ref MemoryMarshal.GetReference(bytes), ref objRef, (uint)size);

        bytes.Reverse();

        return MemoryMarshal.Read<T>(bytes);
    }
}

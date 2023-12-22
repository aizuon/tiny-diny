using System.Runtime.CompilerServices;

namespace TinyDNS.Serialization;

public static class Mem
{
    public static unsafe T ToBigEndian<T>(T obj) where T : unmanaged
    {
        if (BitConverter.IsLittleEndian)
        {
            uint length = (uint)Unsafe.SizeOf<T>();

            var o = &obj;
            byte* p = (byte*)o;
            if (BitConverter.IsLittleEndian)
            {
                byte* pStart = p;
                byte* pEnd = p + length - 1;
                for (int i = 0; i < length / 2; i++)
                {
                    byte temp = *pStart;
                    *pStart++ = *pEnd;
                    *pEnd-- = temp;
                }
            }
        }

        return obj;
    }
}

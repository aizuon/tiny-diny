using System.Runtime.CompilerServices;
using System.Text;

namespace TinyDNS;

public class BinaryBuffer
{
    private readonly object _mutex = new object();

    public BinaryBuffer()
    {
        Buffer = Array.Empty<byte>();
    }

    public BinaryBuffer(byte[] obj)
    {
        Buffer = obj;
        WriteOffset = (uint)obj.Length;
    }

    public byte[] Buffer { get; set; }

    public uint ReadOffset { get; set; }
    public uint WriteOffset { get; set; }

    public unsafe void Write<T>(T obj) where T : unmanaged
    {
        lock (_mutex)
        {
            uint length = (uint)Unsafe.SizeOf<T>();
            GrowIfNeeded(length);
            fixed (byte* b = Buffer)
            {
                System.Buffer.MemoryCopy(&obj, b + WriteOffset, Buffer.Length - WriteOffset, length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(Buffer, (int)WriteOffset, (int)length);
            }

            WriteOffset += length;
        }
    }

    public void WriteRaw<T>(T[] obj) where T : unmanaged
    {
        lock (_mutex)
        {
            uint length = (uint)obj.Length * (uint)Unsafe.SizeOf<T>();
            GrowIfNeeded(length);

            foreach (var o in obj)
                Write(o);
        }
    }

    public void Write(string obj)
    {
        lock (_mutex)
        {
            byte[] ascii = Encoding.ASCII.GetBytes(obj);
            Write((byte)ascii.Length);

            GrowIfNeeded((uint)ascii.Length);

            foreach (byte o in ascii)
                Write(o);
        }
    }

    public void WriteDomainName(string obj)
    {
        lock (_mutex)
        {
            string[] labels = obj.Split('.');
            foreach (string label in labels)
                Write(label);
            Write((byte)0);
        }
    }

    public unsafe bool Read<T>(ref T obj) where T : unmanaged
    {
        lock (_mutex)
        {
            uint length = (uint)Unsafe.SizeOf<T>();

            uint finalOffset = ReadOffset + length;
            if (Buffer.Length < finalOffset)
                return false;

            fixed (T* o = &obj)
            fixed (byte* b = Buffer)
            {
                byte* p = (byte*)o;

                System.Buffer.MemoryCopy(b + ReadOffset, p, length, length);

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

            ReadOffset = finalOffset;

            return true;
        }
    }

    public bool ReadRaw<T>(ref T[] obj, uint count) where T : unmanaged
    {
        lock (_mutex)
        {
            uint length = count * (uint)Unsafe.SizeOf<T>();

            uint finalOffset = ReadOffset + length;
            if (Buffer.Length < finalOffset)
                return false;

            obj = new T[count];
            for (uint i = 0; i < count; i++)
                if (!Read(ref obj[i]))
                    return false;

            return true;
        }
    }

    public bool Read(ref string obj)
    {
        lock (_mutex)
        {
            byte size = 0;
            if (!Read(ref size))
                return false;

            uint length = (uint)(size * sizeof(byte));

            uint finalOffset = ReadOffset + length;
            if (Buffer.Length < finalOffset)
                return false;

            byte[] ascii = new byte[length];
            for (uint i = 0; i < size; i++)
            {
                byte c = 0x00;
                if (!Read(ref c))
                    return false;
                ascii[i] = c;
            }

            obj = Encoding.ASCII.GetString(ascii);

            return true;
        }
    }

    public bool ReadDomainName(ref string obj)
    {
        lock (_mutex)
        {
            var labels = new List<string>();
            bool endOfLabels = false;

            while (!endOfLabels)
            {
                byte lengthOrPointer = 0;
                if (!Read(ref lengthOrPointer))
                    return false;

                if (lengthOrPointer == 0)
                {
                    endOfLabels = true;
                }
                else if ((lengthOrPointer & 0xC0) == 0xC0)
                {
                    byte secondByte = 0;
                    if (!Read(ref secondByte))
                        return false;

                    uint offset = (uint)(((lengthOrPointer & 0x3F) << 8) | secondByte);
                    uint currentPosition = ReadOffset;
                    ReadOffset = offset;
                    string label = string.Empty;
                    if (!ReadDomainName(ref label))
                        return false;
                    labels.Add(label);
                    ReadOffset = currentPosition;
                    endOfLabels = true;
                }
                else
                {
                    ReadOffset--;
                    string label = string.Empty;
                    if (!Read(ref label))
                        return false;
                    labels.Add(label);
                }
            }

            obj = string.Join('.', labels);

            return true;
        }
    }

    private void GrowIfNeeded(uint writeLength)
    {
        uint finalLength = WriteOffset + writeLength;
        bool resizeNeeded = Buffer.Length <= finalLength;

        if (resizeNeeded)
        {
            byte[] tmp = Buffer;
            Array.Resize(ref tmp, (int)finalLength);
            Buffer = tmp;
        }
    }
}

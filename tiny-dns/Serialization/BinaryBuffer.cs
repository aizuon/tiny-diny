using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace TinyDNS.Serialization;

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

    public unsafe void Write<T>(T obj) where T : unmanaged, IBinaryNumber<T>
    {
        lock (_mutex)
        {
            uint length = (uint)Unsafe.SizeOf<T>();
            GrowIfNeeded(length);
            fixed (byte* b = Buffer)
            {
                obj = Mem.ToBigEndian(obj);

                System.Buffer.MemoryCopy(&obj, b + WriteOffset, Buffer.Length - WriteOffset, length);
            }

            WriteOffset += length;
        }
    }

    public void WriteRaw<T>(T[] obj) where T : unmanaged, IBinaryNumber<T>
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

    public unsafe T Read<T>() where T : unmanaged, IBinaryNumber<T>
    {
        lock (_mutex)
        {
            uint length = (uint)Unsafe.SizeOf<T>();

            uint finalOffset = ReadOffset + length;
            if (Buffer.Length < finalOffset)
                throw new ArgumentOutOfRangeException();

            var obj = T.Zero;
            var o = &obj;
            fixed (byte* b = Buffer)
            {
                byte* p = (byte*)o;

                System.Buffer.MemoryCopy(b + ReadOffset, p, length, length);

                obj = Mem.ToBigEndian(obj);
            }

            ReadOffset = finalOffset;

            return obj;
        }
    }

    public T[] ReadRaw<T>(uint count) where T : unmanaged, IBinaryNumber<T>
    {
        lock (_mutex)
        {
            uint length = count * (uint)Unsafe.SizeOf<T>();

            uint finalOffset = ReadOffset + length;
            if (Buffer.Length < finalOffset)
                throw new ArgumentOutOfRangeException();

            var obj = new T[count];
            for (uint i = 0; i < count; i++)
                obj[i] = Read<T>();

            return obj;
        }
    }

    public string ReadString()
    {
        lock (_mutex)
        {
            byte size = Read<byte>();

            uint length = (uint)(size * sizeof(byte));

            uint finalOffset = ReadOffset + length;
            if (Buffer.Length < finalOffset)
                throw new ArgumentOutOfRangeException();

            byte[] ascii = new byte[length];
            for (uint i = 0; i < size; i++)
                ascii[i] = Read<byte>();

            string obj = Encoding.ASCII.GetString(ascii);

            return obj;
        }
    }

    public string ReadDomainName()
    {
        lock (_mutex)
        {
            var labels = new List<string>();
            bool endOfLabels = false;

            while (!endOfLabels)
            {
                byte lengthOrPointer = Read<byte>();

                if (lengthOrPointer == 0)
                {
                    endOfLabels = true;
                }
                else if ((lengthOrPointer & 0xC0) == 0xC0)
                {
                    byte secondByte = Read<byte>();

                    uint offset = (uint)(((lengthOrPointer & 0x3F) << 8) | secondByte);
                    uint currentPosition = ReadOffset;
                    ReadOffset = offset;
                    string label = ReadDomainName();
                    labels.Add(label);
                    ReadOffset = currentPosition;
                    endOfLabels = true;
                }
                else
                {
                    ReadOffset--;
                    string label = ReadString();
                    labels.Add(label);
                }
            }

            string obj = string.Join('.', labels);

            return obj;
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

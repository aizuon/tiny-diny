using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;

namespace TinyDNS.Serialization;

public class BinaryBuffer
{
    private readonly object _mutex = new object();

    private byte[] _buffer;

    private uint _readOffset;

    private uint _writeOffset;

    public BinaryBuffer()
    {
        _buffer = Array.Empty<byte>();
        Capacity = 0;
        Length = 0;
    }

    public BinaryBuffer(byte[] obj)
    {
        _buffer = obj;
        _writeOffset = (uint)obj.Length;
        Capacity = (uint)obj.Length;
        Length = (uint)obj.Length;
    }

    public ArraySegment<byte> Buffer
    {
        get
        {
            lock (_mutex)
            {
                return new ArraySegment<byte>(_buffer, 0, (int)Length);
            }
        }
    }

    public uint Length { get; private set; }

    public uint Capacity { get; private set; }

    public uint ReadOffset
    {
        get => _readOffset;
        set
        {
            if (value > Length)
                throw new ArgumentOutOfRangeException();
            _readOffset = value;
        }
    }

    public uint WriteOffset
    {
        get => _writeOffset;
        set
        {
            if (value > Length)
                throw new ArgumentOutOfRangeException();
            _writeOffset = value;
        }
    }

    public void Write<T>(T obj) where T : unmanaged, IBinaryNumber<T>
    {
        lock (_mutex)
        {
            int length = Unsafe.SizeOf<T>();
            GrowIfNeeded((uint)length);

            var bufferSpan = _buffer.AsSpan((int)_writeOffset, length);
            obj = Mem.ToBigEndian(obj);
            MemoryMarshal.Write(bufferSpan, in obj);

            _writeOffset += (uint)length;
            Length = Math.Max(Length, _writeOffset);
        }
    }

    public void WriteRaw<T>(Span<T> obj) where T : unmanaged, IBinaryNumber<T>
    {
        lock (_mutex)
        {
            int sizeOfT = Unsafe.SizeOf<T>();
            uint length = (uint)obj.Length * (uint)sizeOfT;
            GrowIfNeeded(length);

            var targetSpan = new Span<byte>(_buffer, (int)_writeOffset, (int)length);

            for (int i = 0; i < obj.Length; i++)
            {
                var value = Mem.ToBigEndian(obj[i]);
                MemoryMarshal.Write(targetSpan.Slice(i * sizeOfT, sizeOfT), in value);
            }

            _writeOffset += length;
            Length = Math.Max(Length, _writeOffset);
        }
    }

    public void WriteString(string obj)
    {
        lock (_mutex)
        {
            int byteCount = Encoding.ASCII.GetByteCount(obj);
            Write((byte)byteCount);

            GrowIfNeeded((uint)byteCount);

            Encoding.ASCII.GetBytes(obj, 0, obj.Length, _buffer, (int)_writeOffset);
            _writeOffset += (uint)byteCount;
            Length = Math.Max(Length, _writeOffset);
        }
    }

    public void WriteDomainName(string obj)
    {
        lock (_mutex)
        {
            string[] labels = obj.Split('.');
            foreach (string label in labels)
                WriteString(label);
            Write((byte)0);
        }
    }

    public T Read<T>() where T : unmanaged, IBinaryNumber<T>
    {
        lock (_mutex)
        {
            int length = Unsafe.SizeOf<T>();

            if (_buffer.Length < _readOffset + length)
                throw new ArgumentOutOfRangeException();

            var bufferSpan = _buffer.AsSpan((int)_readOffset, length);
            var value = MemoryMarshal.Read<T>(bufferSpan);
            value = Mem.ToBigEndian(value);

            _readOffset += (uint)length;

            return value;
        }
    }

    public T[] ReadRaw<T>(uint count) where T : unmanaged, IBinaryNumber<T>
    {
        lock (_mutex)
        {
            int sizeOfT = Unsafe.SizeOf<T>();
            uint length = count * (uint)sizeOfT;

            if (_buffer.Length - _readOffset < length)
                throw new ArgumentOutOfRangeException();

            var result = new T[count];
            var byteSpan = new Span<byte>(_buffer, (int)_readOffset, (int)length);

            for (int i = 0; i < count; i++)
            {
                var value = MemoryMarshal.Read<T>(byteSpan[(i * sizeOfT)..]);
                result[i] = Mem.ToBigEndian(value);
            }

            _readOffset += length;
            return result;
        }
    }

    public string ReadString()
    {
        lock (_mutex)
        {
            byte size = Read<byte>();

            if (_buffer.Length < _readOffset + size)
                throw new ArgumentOutOfRangeException();

            string obj = Encoding.ASCII.GetString(_buffer, (int)_readOffset, size);
            _readOffset += size;

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
        uint requiredLength = WriteOffset + writeLength;
        if (Capacity < requiredLength)
        {
            uint newCapacity = Math.Max(Capacity * 2, requiredLength);
            byte[] tmp = _buffer;
            Array.Resize(ref tmp, (int)newCapacity);
            _buffer = tmp;
            Length = requiredLength;
            Capacity = newCapacity;
        }
    }
}

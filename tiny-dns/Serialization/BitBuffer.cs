using System.Numerics;
using System.Runtime.CompilerServices;

namespace TinyDNS.Serialization;

public class BitBuffer
{
    private uint _bitsInPartialByte;
    private uint _bufferLengthInBits;
    private uint _byteArrayIndex;
    private byte _partialByte;
    private int _remaining;

    public BitBuffer()
    {
        Buffer = Array.Empty<byte>();
    }

    public BitBuffer(byte[] obj)
    {
        Buffer = obj;
        _bufferLengthInBits = (uint)obj.Length * 8;
    }

    public byte[] Buffer { get; set; }

    public void Write<T>(T obj, uint countOfBits) where T : unmanaged, IBinaryInteger<T>
    {
        uint length = (uint)Unsafe.SizeOf<T>();
        if (countOfBits <= 0 || countOfBits > length * 8)
            throw new ArgumentOutOfRangeException(nameof(countOfBits));

        int fullBytes = (int)countOfBits / 8;

        uint bitsToWrite = countOfBits % 8;

        obj = Mem.ToBigEndian(obj);

        for (; fullBytes >= 0; fullBytes--)
        {
            var byteOfData = obj >> (fullBytes * 8);
            if (bitsToWrite > 0)
                WriteByte(byte.CreateTruncating(byteOfData), bitsToWrite);
            if (fullBytes > 0)
                bitsToWrite = 8;
        }
    }


    private void WriteByte(byte bits, uint countOfBits)
    {
        byte buffer;
        if (_remaining > 0)
        {
            buffer = Buffer[^1];
            if (countOfBits > _remaining)
                buffer |= (byte)((bits & (0xFF >> (int)(8 - countOfBits))) >> (int)(countOfBits - _remaining));
            else
                buffer |= (byte)((bits & (0xFF >> (int)(8 - countOfBits))) << (int)(_remaining - countOfBits));
            Buffer[^1] = buffer;
        }

        if (countOfBits > _remaining)
        {
            _remaining = 8 - ((int)countOfBits - _remaining);
            unchecked
            {
                buffer = (byte)(bits << _remaining);
            }

            byte[] tmp = Buffer;
            Array.Resize(ref tmp, Buffer.Length + 1);
            Buffer = tmp;

            Buffer[^1] = buffer;
        }
        else
        {
            _remaining -= (int)countOfBits;
        }
    }

    public T Read<T>(uint countOfBits) where T : unmanaged, IBinaryInteger<T>
    {
        uint length = (uint)Unsafe.SizeOf<T>();
        if (countOfBits > length * 8 || countOfBits > _bufferLengthInBits)
            throw new ArgumentOutOfRangeException();

        var obj = T.Zero;
        while (countOfBits > 0)
        {
            uint countToRead = 8;
            if (countOfBits < 8)
                countToRead = countOfBits;
            obj <<= (int)countToRead;
            byte b = ReadByte(countToRead);
            obj |= T.CreateTruncating(b);
            countOfBits -= countToRead;
        }

        obj = Mem.ToBigEndian(obj);

        return obj;
    }

    private byte ReadByte(uint countOfBits)
    {
        if (countOfBits > _bufferLengthInBits)
            throw new ArgumentOutOfRangeException();

        _bufferLengthInBits -= countOfBits;

        byte obj = 0;
        if (_bitsInPartialByte >= countOfBits)
        {
            uint rightShiftPartialByteBy = 8 - countOfBits;
            obj = (byte)(_partialByte >> (int)rightShiftPartialByteBy);

            _partialByte <<= (int)countOfBits;
            _bitsInPartialByte -= countOfBits;
        }
        else
        {
            byte nextByte = Buffer[_byteArrayIndex];
            _byteArrayIndex++;

            uint rightShiftPartialByteBy = 8 - countOfBits;
            obj = (byte)(_partialByte >> (int)rightShiftPartialByteBy);

            uint rightShiftNextByteBy = (uint)Math.Abs((int)countOfBits - _bitsInPartialByte - 8);
            obj |= (byte)(nextByte >> (int)rightShiftNextByteBy);

            unchecked
            {
                _partialByte = (byte)(nextByte << (int)(countOfBits - _bitsInPartialByte));
            }

            _bitsInPartialByte = 8 - (countOfBits - _bitsInPartialByte);
        }

        return obj;
    }
}

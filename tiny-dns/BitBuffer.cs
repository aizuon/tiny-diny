using System.Numerics;
using System.Runtime.CompilerServices;

namespace TinyDNS;

public class BitBuffer
{
    private int _bitsInPartialByte;
    private uint _bufferLengthInBits;
    private int _byteArrayIndex;
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

    public bool EndOfStream => 0 == _bufferLengthInBits;
    public int CurrentIndex => _byteArrayIndex - 1;

    public T Read<T>(int countOfBits) where T : unmanaged, IBinaryInteger<T>
    {
        if (countOfBits > Unsafe.SizeOf<T>() * 8 || countOfBits <= 0)
            throw new ArgumentOutOfRangeException(nameof(countOfBits));

        var obj = T.Zero;
        while (countOfBits > 0)
        {
            int countToRead = 8;
            if (countOfBits < 8)
                countToRead = countOfBits;
            obj <<= countToRead;
            byte b = ReadByte(countToRead);
            obj |= T.CreateChecked(b);
            countOfBits -= countToRead;
        }

        return obj;
    }

    private byte ReadByte(int countOfBits)
    {
        if (EndOfStream)
            throw new EndOfStreamException();

        if (countOfBits > sizeof(byte) * 8 || countOfBits <= 0)
            throw new ArgumentOutOfRangeException(nameof(countOfBits));

        if (countOfBits > _bufferLengthInBits)
            throw new ArgumentOutOfRangeException(nameof(countOfBits));

        _bufferLengthInBits -= (uint)countOfBits;

        byte returnByte;

        if (_bitsInPartialByte >= countOfBits)
        {
            int rightShiftPartialByteBy = 8 - countOfBits;
            returnByte = (byte)(_partialByte >> rightShiftPartialByteBy);

            _partialByte <<= countOfBits;
            _bitsInPartialByte -= countOfBits;
        }
        else
        {
            byte nextByte = Buffer[_byteArrayIndex];
            _byteArrayIndex++;

            int rightShiftPartialByteBy = 8 - countOfBits;
            returnByte = (byte)(_partialByte >> rightShiftPartialByteBy);

            int rightShiftNextByteBy = Math.Abs(countOfBits - _bitsInPartialByte - 8);
            returnByte |= (byte)(nextByte >> rightShiftNextByteBy);

            unchecked
            {
                _partialByte = (byte)(nextByte << (countOfBits - _bitsInPartialByte));
            }

            _bitsInPartialByte = 8 - (countOfBits - _bitsInPartialByte);
        }

        return returnByte;
    }

    public void Write<T>(T bits, int countOfBits) where T : unmanaged, IBinaryInteger<T>
    {
        if (countOfBits <= 0 || countOfBits > Unsafe.SizeOf<T>() * 8)
            throw new ArgumentOutOfRangeException(nameof(countOfBits));

        int fullBytes = countOfBits / 8;

        int bitsToWrite = countOfBits % 8;

        for (; fullBytes >= 0; fullBytes--)
        {
            var byteOfData = bits >> (fullBytes * 8);
            if (bitsToWrite > 0)
                WriteByte(byte.CreateTruncating(byteOfData), bitsToWrite);
            if (fullBytes > 0)
                bitsToWrite = 8;
        }
    }


    private void WriteByte(byte bits, int countOfBits)
    {
        if (countOfBits <= 0 || countOfBits > sizeof(byte) * 8)
            throw new ArgumentOutOfRangeException(nameof(countOfBits));

        byte buffer;
        if (_remaining > 0)
        {
            buffer = Buffer[^1];
            if (countOfBits > _remaining)
                buffer |= (byte)((bits & (0xFF >> (8 - countOfBits))) >> (countOfBits - _remaining));
            else
                buffer |= (byte)((bits & (0xFF >> (8 - countOfBits))) << (_remaining - countOfBits));
            Buffer[^1] = buffer;
        }

        if (countOfBits > _remaining)
        {
            _remaining = 8 - (countOfBits - _remaining);
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
            _remaining -= countOfBits;
        }
    }
}

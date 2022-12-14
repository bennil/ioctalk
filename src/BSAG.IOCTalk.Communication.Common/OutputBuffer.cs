// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


// This is a copy from https://github.com/Microsoft/bond/blob/master/cs/src/core/io/safe/OutputBuffer.cs with the ability to change the underlying byte array from the outside


namespace Bond.IO.Safe
{
    using BSAG.IOCTalk.Common.Interface.Communication;
    using System;
    using System.Buffers;
    using System.Diagnostics;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Implements IOutputStream on top of memory buffer
    /// </summary>
    public class OutputBuffer : IStreamWriter, IBufferWriter<byte> //: IOutputStream
    {
        const int BlockCopyMin = 32;
        internal byte[] buffer;
        internal int position;
        internal int length;

        /// <summary>
        /// Gets data inside the buffer
        /// </summary>
        public ArraySegment<byte> Data
        {
            get { return new ArraySegment<byte>(buffer, 0, position); }
        }

        /// <summary>
        /// Gets the data buffer.
        /// </summary>
        /// <value>The data buffer.</value>
        public byte[] DataBuffer
        {
            get { return buffer; }
        }

        /// <summary>
        /// Gets or sets the current position within the buffer
        /// </summary>
        public virtual long Position
        {
            get { return position; }
            set { position = checked((int)value); }
        }

        public int WrittenCount => position;

        public OutputBuffer(int length = 64 * 1024)
            : this(new byte[length])
        { }

        public OutputBuffer(byte[] buffer)
        {
            Debug.Assert(BitConverter.IsLittleEndian);
            this.buffer = buffer;
            length = buffer.Length;
            position = 0;
        }

        /// <summary>
        /// Updates the buffer and resets the position.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="length">The length.</param>
        public void UpdateBuffer(byte[] data, int length)
        {
            this.buffer = data;
            this.length = length;
            this.position = 0;
        }

        public void Reset()
        {
            this.position = 0;
        }

        #region IOutputStream

        /// <summary>
        /// Write 8-bit unsigned integer
        /// </summary>
        public void WriteUInt8(byte value)
        {
            if (position >= length)
            {
                Grow(sizeof(byte));
            }

            buffer[position++] = value;
        }

        /// <summary>
        /// Write little-endian encoded 16-bit unsigned integer
        /// </summary>
        public void WriteUInt16(ushort value)
        {
            if (position + sizeof(ushort) > length)
            {
                Grow(sizeof(ushort));
            }

            var i = position;
            var b = buffer;
            b[i++] = (byte)value;
            b[i++] = (byte)(value >> 8);
            position = i;
        }

        /// <summary>
        /// Write little-endian encoded 32-bit unsigned integer
        /// </summary>
        public virtual void WriteUInt32(uint value)
        {
            if (position + sizeof(uint) > length)
            {
                Grow(sizeof(uint));
            }

            var i = position;
            var b = buffer;
            b[i++] = (byte)value;
            b[i++] = (byte)(value >> 8);
            b[i++] = (byte)(value >> 16);
            b[i++] = (byte)(value >> 24);
            position = i;
        }

        /// <summary>
        /// Write little-endian encoded 64-bit unsigned integer
        /// </summary>
        public virtual void WriteUInt64(ulong value)
        {
            if (position + sizeof(ulong) > length)
            {
                Grow(sizeof(ulong));
            }

            var i = position;
            var b = buffer;
            b[i++] = (byte)value;
            b[i++] = (byte)(value >> 8);
            b[i++] = (byte)(value >> 16);
            b[i++] = (byte)(value >> 24);
            b[i++] = (byte)(value >> 32);
            b[i++] = (byte)(value >> 40);
            b[i++] = (byte)(value >> 48);
            b[i++] = (byte)(value >> 56);
            position = i;
        }

        /// <summary>
        /// Write little-endian encoded single precision IEEE 754 float
        /// </summary>
        public virtual void WriteFloat(float value)
        {
            WriteUInt32(new FloatLayout { value = value }.bytes);
        }

        /// <summary>
        /// Write little-endian encoded double precision IEEE 754 float
        /// </summary>
        public virtual void WriteDouble(double value)
        {
            WriteUInt64(new DoubleLayout { value = value }.bytes);
        }

        /// <summary>
        /// Write an array of bytes verbatim
        /// </summary>
        /// <param name="data">Array segment specifying bytes to write</param>
        public virtual void WriteBytes(ArraySegment<byte> data)
        {
            var newOffset = position + data.Count;
            if (newOffset > length)
            {
                Grow(data.Count);
            }

            if (data.Count < BlockCopyMin)
            {
                for (int i = position, j = data.Offset; i < newOffset; ++i, ++j)
                {
                    buffer[i] = data.Array[j];
                }
            }
            else
            {
                Buffer.BlockCopy(data.Array, data.Offset, buffer, position, data.Count);
            }
            position = newOffset;
        }

        /// <summary>
        /// Write an array of bytes verbatim
        /// </summary>
        /// <param name="data">Array segment specifying bytes to write</param>
        public virtual void WriteBytes(byte[] data)
        {
            var newOffset = position + data.Length;
            if (newOffset > length)
            {
                Grow(data.Length);
            }

            if (data.Length < BlockCopyMin)
            {
                for (int i = position, j = data.Length; i < newOffset; ++i, ++j)
                {
                    buffer[i] = data[j];
                }
            }
            else
            {
                Buffer.BlockCopy(data, 0, buffer, position, data.Length);
            }
            position = newOffset;
        }

        /// <summary>
        /// Write variable encoded 16-bit unsigned integer
        /// </summary>
        public void WriteVarUInt16(ushort value)
        {
            if (position + IntegerHelper.MaxBytesVarInt16 > length)
            {
                Grow(IntegerHelper.MaxBytesVarInt16);
            }
            position = IntegerHelper.EncodeVarUInt16(buffer, value, position);
        }

        /// <summary>
        /// Write variable encoded 32-bit unsigned integer
        /// </summary>
        public void WriteVarUInt32(uint value)
        {
            if (position + IntegerHelper.MaxBytesVarInt32 > length)
            {
                Grow(IntegerHelper.MaxBytesVarInt32);
            }
            position = IntegerHelper.EncodeVarUInt32(buffer, value, position);
        }

        /// <summary>
        /// Write variable encoded 64-bit unsigned integer
        /// </summary>
        public void WriteVarUInt64(ulong value)
        {
            if (position + IntegerHelper.MaxBytesVarInt64 > length)
            {
                Grow(IntegerHelper.MaxBytesVarInt64);
            }
            position = IntegerHelper.EncodeVarUInt64(buffer, value, position);
        }


        /// <summary>
        /// Write UTF-8 or UTF-16 encoded string
        /// </summary>
        /// <param name="encoding">String encoding</param>
        /// <param name="value">String value</param>
        /// <param name="size">Size in bytes of encoded string</param>
        public virtual void WriteString(Encoding encoding, string value, int size)
        {
            if (position + size > length)
            {
                Grow(size);
            }
            position += encoding.GetBytes(value, 0, value.Length, buffer, position);
        }

        #endregion

        // Grow the buffer so that there is enough space to write 'count' bytes
        internal virtual void Grow(int count)
        {
            var minLength = position + count;
            length += length >> 1;
            if (length < minLength) length = minLength;

            Array.Resize(ref buffer, length);
        }

        #region layouts

        [StructLayout(LayoutKind.Explicit)]
        struct DoubleLayout
        {
            [FieldOffset(0)]
            public readonly ulong bytes;

            [FieldOffset(0)]
            public double value;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct FloatLayout
        {
            [FieldOffset(0)]
            public readonly uint bytes;

            [FieldOffset(0)]
            public float value;
        }

        #endregion


        #region writer methods


        /// <summary>
        /// Writes the bool.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBool(bool value)
        {
            this.WriteUInt8((byte)(value ? 1 : 0));
        }

        /// <summary>
        /// Writes the int16.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt16(short value)
        {
            this.WriteUInt16(unchecked((UInt16)value));
        }

        /// <summary>
        /// Writes the int32.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt32(int value)
        {
            this.WriteUInt32(unchecked((UInt32)value));
        }

        /// <summary>
        /// Writes the int64.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt64(long value)
        {
            this.WriteUInt64(unchecked((UInt64)value));
        }

        /// <summary>
        /// Writes the int8.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt8(sbyte value)
        {
            this.WriteUInt8(unchecked((Byte)value));
        }

        /// <summary>
        /// Writes the string.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteString(string value)
        {
            if (value.Length == 0)
            {
                WriteLength(0);
            }
            else
            {
                var size = Encoding.UTF8.GetByteCount(value);
                WriteLength(size);
                this.WriteString(Encoding.UTF8, value, size);
            }
        }

        /// <summary>
        /// Writes the length.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteLength(int value)
        {
            this.WriteVarUInt32((uint)value);
        }



        #endregion

        #region IBufferWriter<byte>

        public void Advance(int count)
        {
            if (position + count >= length)
            {
                Grow(count);
            }

            position += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (position + sizeHint >= length)
            {
                Grow(sizeHint);
            }

            return buffer.AsMemory(position);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (position + sizeHint >= length)
            {
                Grow(sizeHint);
            }

            return buffer.AsSpan(position);
        }


        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
        /// </summary>
        public ReadOnlySpan<byte> WrittenSpan => buffer.AsSpan(0, position);


        /// <summary>
        /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlyMemory{T}"/>.
        /// </summary>
        public ReadOnlyMemory<byte> WrittenMemory => buffer.AsMemory(0, position);

        #endregion
    }
}
using Bond.IO.Safe;
using BSAG.IOCTalk.Common.Interface.Communication;
using BSAG.IOCTalk.Serialization.Binary.TypeStructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Serialization.Binary.Stream
{
    /// <summary>
    /// Class StreamWriter.
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Common.Interface.Communication.IStreamWriter" />
    /// <seealso cref="Bond.IO.Safe.OutputBuffer" />
    public class StreamWriter : OutputBuffer, IStreamWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter" /> class.
        /// </summary>
        /// <param name="length">The length.</param>
        public StreamWriter(int length)
                    : base(length)
        {
        }

        /// <summary>
        /// Writes the bool.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteBool(bool value)
        {
            base.WriteUInt8((byte)(value ? 1 : 0));
        }

        /// <summary>
        /// Writes the int16.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt16(short value)
        {
            base.WriteUInt16(unchecked((UInt16)value));
        }

        /// <summary>
        /// Writes the int32.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt32(int value)
        {
            base.WriteUInt32(unchecked((UInt32)value));
        }

        /// <summary>
        /// Writes the int64.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt64(long value)
        {
            base.WriteUInt64(unchecked((UInt64)value));
        }

        /// <summary>
        /// Writes the int8.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteInt8(sbyte value)
        {
            base.WriteUInt8(unchecked((Byte)value));
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
                base.WriteString(Encoding.UTF8, value, size);
            }
        }

        /// <summary>
        /// Writes the length.
        /// </summary>
        /// <param name="value">The value.</param>
        void WriteLength(int value)
        {
            base.WriteVarUInt32((uint)value);
        }
    }
}

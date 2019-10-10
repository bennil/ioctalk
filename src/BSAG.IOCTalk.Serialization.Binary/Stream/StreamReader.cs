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
    /// Class StreamReader.
    /// </summary>
    /// <seealso cref="BSAG.IOCTalk.Common.Interface.Communication.IStreamReader" />
    /// <seealso cref="Bond.IO.Safe.InputBuffer" />
    public class StreamReader : InputBuffer, IStreamReader
    {        

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReader" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public StreamReader(byte[] data)
            :base(data)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReader"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="length">The length.</param>
        public StreamReader(byte[] data, int length)
            : base(data, length)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamReader" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        public StreamReader(byte[] data, int offset, int length)
            : base(data, offset, length)
        {
        }


        /// <summary>
        /// Reads the bool.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool ReadBool()
        {
            return base.ReadUInt8() != 0;
        }


        /// <summary>
        /// Reads the int16.
        /// </summary>
        /// <returns>System.Int16.</returns>
        public short ReadInt16()
        {
            return unchecked((short)base.ReadUInt16());
        }

        /// <summary>
        /// Reads the int32.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int ReadInt32()
        {
            return unchecked((int)base.ReadUInt32());
        }

        /// <summary>
        /// Reads the int64.
        /// </summary>
        /// <returns>System.Int64.</returns>
        public long ReadInt64()
        {
            return unchecked((Int64)base.ReadUInt64());
        }

        /// <summary>
        /// Reads the int8.
        /// </summary>
        /// <returns>System.SByte.</returns>
        public sbyte ReadInt8()
        {
            return unchecked((sbyte)base.ReadUInt8());
        }

        /// <summary>
        /// Reads the string.
        /// </summary>
        /// <returns>System.String.</returns>
        public string ReadString()
        {
            var length = ReadLength();
            return length == 0 ? string.Empty : base.ReadString(Encoding.UTF8, length);
        }

        /// <summary>
        /// Reads the length.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int ReadLength()
        {
            return (int)base.ReadVarUInt32();
        }


        /// <summary>
        /// Skips the bool.
        /// </summary>
        public void SkipBool()
        {
            base.SkipBytes(1);
        }

        /// <summary>
        /// Skips the double.
        /// </summary>
        public void SkipDouble()
        {
            base.SkipBytes(8);
        }

        /// <summary>
        /// Skips the float.
        /// </summary>
        public void SkipFloat()
        {
            base.SkipBytes(4);
        }

        /// <summary>
        /// Skips the int16.
        /// </summary>
        public void SkipInt16()
        {
            base.SkipBytes(2);
        }

        /// <summary>
        /// Skips the int32.
        /// </summary>
        public void SkipInt32()
        {
            base.SkipBytes(4);

        }

        /// <summary>
        /// Skips the int64.
        /// </summary>
        public void SkipInt64()
        {
            base.SkipBytes(8);
        }

        /// <summary>
        /// Skips the int8.
        /// </summary>
        public void SkipInt8()
        {
            base.SkipBytes(1);
        }

        /// <summary>
        /// Skips the string.
        /// </summary>
        public void SkipString()
        {
            base.SkipBytes(ReadLength());
        }

        /// <summary>
        /// Skips the u int16.
        /// </summary>
        public void SkipUInt16()
        {
            base.SkipBytes(2);
        }

        /// <summary>
        /// Skips the u int32.
        /// </summary>
        public void SkipUInt32()
        {
            base.SkipBytes(4);
        }

        /// <summary>
        /// Skips the u int64.
        /// </summary>
        public void SkipUInt64()
        {
            base.SkipBytes(8);
        }

        /// <summary>
        /// Skips the u int8.
        /// </summary>
        public void SkipUInt8()
        {
            base.SkipBytes(1);
        }
    }
}

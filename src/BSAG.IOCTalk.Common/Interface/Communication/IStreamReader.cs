using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Interface IStreamReader (interface surface from Bond.Protocols)
    /// </summary>
    public interface IStreamReader
    {
        //
        // Summary:
        //     Read a bool
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        bool ReadBool();
        //
        // Summary:
        //     Read an array of bytes verbatim
        //
        // Parameters:
        //   count:
        //     Number of bytes to read
        ArraySegment<byte> ReadBytes(int count);

        //
        // Summary:
        //     Read a double
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        double ReadDouble();

        //
        // Summary:
        //     Read a float
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        float ReadFloat();
        //
        // Summary:
        //     Read an Int16
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        short ReadInt16();
        //
        // Summary:
        //     Read an Int32
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        int ReadInt32();
        //
        // Summary:
        //     Read an Int64
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        long ReadInt64();
        //
        // Summary:
        //     Read an Int8
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        sbyte ReadInt8();
        //
        // Summary:
        //     Read a UTF8 string
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        string ReadString();
        //
        // Summary:
        //     Read an UInt16
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        ushort ReadUInt16();
        //
        // Summary:
        //     Read an UInt32
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        uint ReadUInt32();
        //
        // Summary:
        //     Read an UInt64
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        ulong ReadUInt64();
        //
        // Summary:
        //     Read a UInt8
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        byte ReadUInt8();

        //
        // Summary:
        //     Skip a bool
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipBool();
        //
        // Summary:
        //     Skip forward specified number of bytes
        //
        // Parameters:
        //   count:
        //     Number of bytes to skip
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipBytes(int count);
        //
        // Summary:
        //     Skip a double
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipDouble();
        //
        // Summary:
        //     Skip an float
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipFloat();
        //
        // Summary:
        //     Skip an Int16
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipInt16();
        //
        // Summary:
        //     Skip an Int32
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipInt32();
        //
        // Summary:
        //     Skip an Int64
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipInt64();
        //
        // Summary:
        //     Skip an Int8
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipInt8();
        //
        // Summary:
        //     Skip a UTF8 string
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipString();
        //
        // Summary:
        //     Skip an UInt16
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipUInt16();
        //
        // Summary:
        //     Skip an UInt32
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipUInt32();
        //
        // Summary:
        //     Skip one UInt64
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipUInt64();
        //
        // Summary:
        //     Skip an UInt8
        //
        // Exceptions:
        //   T:System.IO.EndOfStreamException:
        void SkipUInt8();



        /// <summary>
        /// Reads the length.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int ReadLength();


        /// <summary>
        /// Read variable encoded 32-bit unsigned integer
        /// </summary>
        /// <exception cref="EndOfStreamException"/>
        uint ReadVarUInt32();


        /// <summary>
        /// Gets the current reader position.
        /// </summary>
        long Position { get; }
    }
}

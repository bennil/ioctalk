using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSAG.IOCTalk.Common.Interface.Communication
{
    /// <summary>
    /// Interface IStreamWriter (interface surface from Bond.Protocols)
    /// </summary>
    public interface IStreamWriter
    {
        //
        // Summary:
        //     Write a bool
        void WriteBool(bool value);
        //
        // Summary:
        //     Write array of bytes verbatim
        void WriteBytes(ArraySegment<byte> data);


        void WriteBytes(byte[] data);

        //
        // Summary:
        //     Write a double
        void WriteDouble(double value);

        //
        // Summary:
        //     Write a float
        void WriteFloat(float value);
        //
        // Summary:
        //     Write an Int16
        void WriteInt16(short value);
        //
        // Summary:
        //     Write an Int32
        void WriteInt32(int value);
        //
        // Summary:
        //     Write an Int64
        void WriteInt64(long value);
        //
        // Summary:
        //     Write an Int8
        void WriteInt8(sbyte value);
        //
        // Summary:
        //     Write a UTF-8 string
        void WriteString(string value);

        //
        // Summary:
        //     Write an UInt16
        void WriteUInt16(ushort value);
        //
        // Summary:
        //     Write an UInt32
        void WriteUInt32(uint value);
        //
        // Summary:
        //     Write an UInt64
        void WriteUInt64(ulong value);
        //
        // Summary:
        //     Write an UInt8
        void WriteUInt8(byte value);
    }
}

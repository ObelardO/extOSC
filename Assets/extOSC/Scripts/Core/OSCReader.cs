using System;
using System.Text;

namespace extOSC.Core
{
    internal static class OSCReader
    {
        private static readonly object _lock = new();
        private static readonly DateTime _zeroTime = new DateTime(1900, 1, 1, 0, 0, 0, 0);
        
        // read
        public static IOSCPacket ReadPackage(byte[] buffer, ref int offset, bool useAscii)
        {
            var address = ReadString(buffer, ref offset, useAscii);
            if (address == OSCBundle.BundleAddress)
            {
                return ReadBundleInternal(buffer, ref offset, useAscii);
            }

            return ReadMessageInternal(buffer, ref offset, address, useAscii);
        }

        public static OSCBundle ReadBundle(byte[] buffer, ref int offset, bool useAscii)
        {
            // read address
            var address = ReadString(buffer, ref offset, useAscii);
            if (address != OSCBundle.BundleAddress)
                throw new Exception("Wrong OSC Address.");
         
            // read data
            return ReadBundleInternal(buffer, ref offset, useAscii);
        }

        private static OSCBundle ReadBundleInternal(byte[] buffer, ref int offset, bool useAscii)
        {
            var bundle = new OSCBundle();
            
            // read timetag
            bundle.TimeStamp = ReadDateTime(buffer, ref offset);
            
            // read packages
            while (offset < buffer.Length)
            {
                var packet = ReadPackage(buffer, ref offset, useAscii);
                
                bundle.Append(packet);
            }
            
            return bundle;
        }

        public static OSCMessage ReadMessage(byte[] buffer, ref int offset, bool useAscii)
        {
            // read address
            var address = ReadString(buffer, ref offset, useAscii);
            if (address == OSCBundle.BundleAddress)
                throw new Exception("Wrong OSC Address.");
            
            // read data
            return ReadMessageInternal(buffer, ref offset, address, useAscii);
        }
        
        private static OSCMessage ReadMessageInternal(byte[] buffer, ref int offset, string address, bool useAscii)
        {
            var message = new OSCMessage(address);
            
            // read tags
            var typesString = ReadString(buffer, ref offset, useAscii).Substring(1);
            
            // read values
            for (var i = 0; i < typesString.Length; i++) 
                message.AddValue(ReadValue(buffer, ref offset, typesString[i], useAscii));
            
            return message;
        }

        public static OSCValue ReadValue(byte[] buffer, ref int offset, char valueTag, bool useAscii)
        {
            // get tag
            var type = OSCValue.GetValueType(valueTag);

            // read value by tag
            switch (type)
            {
                case OSCValueType.Int:
                    return OSCValue.Int(ReadInt(buffer, ref offset));
                case OSCValueType.Long:
                    return OSCValue.Long(ReadLong(buffer, ref offset));
                case OSCValueType.True:
                    return OSCValue.Bool(true);
                case OSCValueType.False:
                    return OSCValue.Bool(false);
                case OSCValueType.Float:
                    return OSCValue.Float(ReadFloat(buffer, ref offset));
                case OSCValueType.Double:
                    return OSCValue.Double(ReadDouble(buffer, ref offset));
                case OSCValueType.String:
                    return OSCValue.String(ReadString(buffer, ref offset, useAscii));
                case OSCValueType.Null:
                    return OSCValue.Null();
                case OSCValueType.Impulse:
                    return OSCValue.Impulse();
                case OSCValueType.Char:
                    return OSCValue.Char(ReadChar(buffer, ref offset));
                case OSCValueType.Color:
                    return OSCValue.Color(ReadColor(buffer, ref offset));
                case OSCValueType.Blob:
                    return OSCValue.Blob(ReadBlob(buffer, ref offset));
                case OSCValueType.TimeTag:
                    return OSCValue.TimeTag(ReadDateTime(buffer, ref offset));
                case OSCValueType.Midi:
                    return OSCValue.Midi(ReadMidi(buffer, ref offset));
                case OSCValueType.Unknown:
                    throw new NotImplementedException($"Reader. Type {type} not implemented!");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // blob
        private static byte[] ReadBlob(byte[] buffer, ref int offset)
        {
            var blobSize = ReadInt(buffer, ref offset);
            var blob = new byte[blobSize];
            
            Array.Copy(buffer, offset, blob, 0, blobSize);
            offset += blobSize;
            
            IncludeZeroBytes(blobSize, ref offset);

            return blob;
        }
        
        // char
        private static char ReadChar(byte[] buffer, ref int offset)
        {
            return (char)((buffer[offset++] << 8) | buffer[offset++]);
        }
        
        // color
        private static OSCColor ReadColor(byte[] buffer, ref int offset)
        {
            return new OSCColor(
                buffer[offset++],
                buffer[offset++],
                buffer[offset++],
                buffer[offset++]);
        }
        
        // double
        private static double ReadDouble(byte[] buffer, ref int offset)
        {
            var bits = ReadLong(buffer, ref offset);

            return BitConverter.Int64BitsToDouble(bits);
        }
        
        // float
        private static float ReadFloat(byte[] buffer, ref int offset)
        {
            var bits = ReadInt(buffer, ref offset);

            return BitConverter.Int32BitsToSingle(bits);
        }
        
        // parse int
        private static int ReadInt(byte[] buffer, ref int offset)
        {
            return (buffer[offset++] << 24) |
                   (buffer[offset++] << 16) |
                   (buffer[offset++] <<  8) |
                   (buffer[offset++] <<  0);
        }
        
        // long 
        private static long ReadLong(byte[] buffer, ref int offset)
        {
            return ((long)buffer[offset++] << 56) |
                   ((long)buffer[offset++] << 48) |
                   ((long)buffer[offset++] << 40) |
                   ((long)buffer[offset++] << 32) |
                   ((long)buffer[offset++] << 24) |
                   ((long)buffer[offset++] << 16) |
                   ((long)buffer[offset++] <<  8) |
                   ((long)buffer[offset++] <<  0);
        }
        
        // midi
        private static OSCMidi ReadMidi(byte[] buffer, ref int offset)
        {
            return new OSCMidi(
                buffer[offset++],
                buffer[offset++],
                buffer[offset++],
                buffer[offset++]);
        }
        
        // string ascii
        private static string ReadString_ASCII(byte[] buffer, ref int offset)
        {
            var stringLength = 0;
            for (; buffer[offset + stringLength] != 0; ++stringLength);
            
            var value = Encoding.ASCII.GetString(buffer, offset, stringLength);
            IncludeZeroBytes(stringLength, ref offset);
            return value;
        }

        // string
        private static string ReadString(byte[] buffer, ref int offset, bool useAscii)
        {
            if (useAscii)
            {
                return ReadString_ASCII(buffer, ref offset);
            }
            else
            {
                return ReadString_UTF8(buffer, ref offset);
            }
        }
        
        // string utf8
        private static string ReadString_UTF8(byte[] buffer, ref int offset)
        {
            var stringLength = 0;
            for (; buffer[offset + stringLength] != 0; ++stringLength);
            
            var value = Encoding.UTF8.GetString(buffer, offset, stringLength);
            IncludeZeroBytes(stringLength, ref offset);
            return value;
        } 
        
        // timetag
        private static DateTime ReadDateTime(byte[] buffer, ref int offset)
        {
            var timestamp = ((ulong)buffer[offset++] << 56) |
                            ((ulong)buffer[offset++] << 48) |
                            ((ulong)buffer[offset++] << 40) |
                            ((ulong)buffer[offset++] << 32) |
                            ((ulong)buffer[offset++] << 24) |
                            ((ulong)buffer[offset++] << 16) |
                            ((ulong)buffer[offset++] << 8) |
                            ((ulong)buffer[offset++] << 0);
            
            var part1 = (timestamp >> 32) & 0xFFFFFFFF; 
            var part2 = (timestamp >>  0) & 0xFFFFFFFF;
            var totalMSec = part1 * 1000 + part2 * 1000 / 0x100000000L;

            lock (_lock)
            {
                return _zeroTime.AddMilliseconds(totalMSec);
            }
        }
        
        // tools
        private static void IncludeZeroBytes(int size, ref int offset)
        {
            offset += (size + 4) & ~0x3;
        }
    }
}
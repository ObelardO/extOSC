using System;
using System.IO;
using System.Text;

namespace extOSC.Core
{
    internal static class OSCWriter
    {
        private static readonly byte[] _zeroBytes = { 0, 0, 0, 0 };
        private static readonly DateTime _zeroTime = new DateTime(1900, 1, 1, 0, 0, 0, 0);
        
        // write
        public static void Write(MemoryStream stream, IOSCPacket packet, bool useAscii)
        {
            switch (packet)
            {
                case OSCBundle bundle:
                    Write(stream, bundle, useAscii);
                    break;
                case OSCMessage message:
                    Write(stream, message, useAscii);
                    break;
            }
        }
        
        public static void Write(MemoryStream stream, OSCBundle bundle, bool useAscii)
        {
            // write address
            Write(stream, OSCBundle.BundleAddress, useAscii);
            
            // write timestamp
            Write(stream, bundle.TimeStamp);

            // write packets
            for (var i = 0; i < bundle.Packets.Count; i++) 
                Write(stream, bundle.Packets[i], useAscii);
        }
        
        public static void Write(MemoryStream stream, OSCMessage message, bool useAscii)
        {
            // write address
            Write(stream, message.Address, useAscii);

            // write types
            var typesString = ",";
            for (var i = 0; i < message.Values.Count; i++)
                typesString += message.Values[i].Tag;
            Write(stream, typesString, useAscii);

            // write values
            for (var i = 0; i < message.Values.Count; i++)
                Write(stream, message.Values[i], useAscii);
        }

        public static void Write(MemoryStream stream, OSCValue value, bool useAscii)
        {
            switch (value.Type)
            {
                case OSCValueType.Int:
                    Write(stream, value.IntValue);
                    break;
                case OSCValueType.Long:
                    Write(stream, value.LongValue);
                    break;

                case OSCValueType.Float:
                    Write(stream, value.FloatValue);
                    break;
                case OSCValueType.Double:
                    Write(stream, value.DoubleValue);
                    break;
                case OSCValueType.String:
                    Write(stream, value.StringValue, useAscii);
                    break;
                case OSCValueType.Char:
                    Write(stream, value.CharValue);
                    break;
                case OSCValueType.Color:
                    Write(stream, value.ColorValue);
                    break;
                case OSCValueType.Blob:
                    Write(stream, value.BlobValue);
                    break;
                case OSCValueType.TimeTag:
                    Write(stream, value.TimeTagValue);
                    break;
                case OSCValueType.Midi:
                    Write(stream, value.MidiValue);
                    break;
                case OSCValueType.Null:
                case OSCValueType.Impulse:
                case OSCValueType.True:
                case OSCValueType.False:
                    // That types does not have additional data
                    break;
                case OSCValueType.Unknown:
                    throw new NotImplementedException($"Writer. Type {value.Type} not implemented!");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        // blob
        private static void Write(MemoryStream stream, byte[] blob)
        {
            // write block length
            var length = blob.Length;
            Write(stream, length); // int
            
            // write blob
            stream.Write(blob, 0, length);
            
            // ---
            IncludeZeroBytes(stream, length);
        }
        
        // char
        private static void Write(MemoryStream stream, char value)
        {
            // write char
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 0) & 0xFF));
        }

        // color
        private static void Write(MemoryStream stream, OSCColor value)
        {
            // write color
            stream.WriteByte(value.R);
            stream.WriteByte(value.G);
            stream.WriteByte(value.B);
            stream.WriteByte(value.A);
        }
        
        // double
        private static void Write(MemoryStream stream, double value)
        {
            // convert double to long
            var bits = BitConverter.DoubleToInt64Bits(value);

            // write long
            Write(stream, bits);
        }
        
        // float
        private static void Write(MemoryStream stream, float value)
        {
            // convert float to int
            var bits = BitConverter.SingleToInt32Bits(value);
                
            // write int
            Write(stream, bits);
        }
        
        // int
        private static void Write(MemoryStream stream, int value)
        {
            // write int
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >>  8) & 0xFF));
            stream.WriteByte((byte)((value >>  0) & 0xFF));
        }
        
        // long
        private static void Write(MemoryStream stream, long value)
        {
            // write long
            stream.WriteByte((byte)((value >> 56) & 0xFF));
            stream.WriteByte((byte)((value >> 48) & 0xFF));
            stream.WriteByte((byte)((value >> 40) & 0xFF));
            stream.WriteByte((byte)((value >> 32) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >>  8) & 0xFF));
            stream.WriteByte((byte)((value >>  0) & 0xFF));
        }

        // midi
        private static void Write(MemoryStream stream, OSCMidi value)
        {
            stream.WriteByte(value.Channel);
            stream.WriteByte(value.Status);
            stream.WriteByte(value.Data1);
            stream.WriteByte(value.Data2);
        }
        
        // string
        private static void Write(MemoryStream stream, string value, bool useAscii)
        {
            if (useAscii)
            {
                Write_ASCII(stream, value);
            }
            else
            {
                Write_UTF8(stream, value);
            }
        }

        // string ascii
        private static void Write_ASCII(MemoryStream stream, string value)
        {
            // get ascii bytes
            var bytes = Encoding.ASCII.GetBytes(value);
            var bytesSize = bytes.Length;
            
            // write ascii
            stream.Write(bytes, 0, bytesSize);
            
            IncludeZeroBytes(stream, bytesSize);
        }
        
        // string utf8
        private static void Write_UTF8(MemoryStream stream, string value)
        {
            // get ascii bytes
            var bytes = Encoding.UTF8.GetBytes(value);
            var bytesSize = bytes.Length;
            
            // write ascii
            stream.Write(bytes, 0, bytesSize);
            
            IncludeZeroBytes(stream, bytesSize);
        }
        
        // timetag
        private static void Write(MemoryStream stream, DateTime value)
        {
            var totalMSec = (ulong)(value - _zeroTime).TotalMilliseconds;
            var part1 = totalMSec / 1000;
            var part2 = totalMSec % 1000 * 0x100000000L / 1000;
            var timestamp = (part1 << 32) | (part2 << 0);
            
            // write timestamp
            stream.WriteByte((byte)((timestamp >> 56) & 0xFF));
            stream.WriteByte((byte)((timestamp >> 48) & 0xFF));
            stream.WriteByte((byte)((timestamp >> 40) & 0xFF));
            stream.WriteByte((byte)((timestamp >> 32) & 0xFF));
            stream.WriteByte((byte)((timestamp >> 24) & 0xFF));
            stream.WriteByte((byte)((timestamp >> 16) & 0xFF));
            stream.WriteByte((byte)((timestamp >>  8) & 0xFF));
            stream.WriteByte((byte)((timestamp >>  0) & 0xFF));
        }
        
        // tools
        private static void IncludeZeroBytes(MemoryStream stream, int size)
        {
            var targetSize = (size + 4) & ~0x3;
            var offset = targetSize - size;
            if (offset > 0)
            {
                stream.Write(_zeroBytes, 0, offset);
            }
        }
    }
}
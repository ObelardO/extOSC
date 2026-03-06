/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;
using System.Net;
using System.Collections.Generic;
using extOSC.Core;

namespace extOSC
{
	public class OSCMessage : IOSCPacket
	{
		public static OSCMessage Create(string address, params OSCValue[] values) => new(address, values);

		public string Address { get; set; }
		public IPEndPoint From { get; set; }
		public List<OSCValue> Values { get; } = new List<OSCValue>();

		public OSCMessage(string address)
		{
			Address = address;
		}

		public OSCMessage(string address, params OSCValue[] values)
		{
			Address = address;
			Values.AddRange(values);
		}
		
		public IOSCPacket Clone()
		{
			var valuesCount = Values.Count;
			var values = new OSCValue[valuesCount];

			for (var i = 0; i < valuesCount; ++i)
			{
				values[i] = Values[i].Clone();
			}

			return new OSCMessage(Address, values);
		}
		
		public override string ToString()
		{
			var stringValues = string.Empty;

			if (Values.Count > 0)
			{
				foreach (var value in Values)
				{
					stringValues += $"{value.GetType().Name}({value.Type}) : \"{value.Value}\", ";
				}

				stringValues = $"({stringValues.Remove(stringValues.Length - 2)})";
			}

			return $"<{GetType().Name}:\"{Address}\"> : {(string.IsNullOrEmpty(stringValues) ? "null" : stringValues)}";
		}

		// VALUES
		public void AddValue(OSCValue value)
		{
			if (value == null)
				throw new NullReferenceException(nameof(value));

			Values.Add(value);
		}

		public bool TryGetValue(int index, out OSCValue value)
		{
			if( index < 0 || index >= Values.Count)
			{
				value = null;
				return false;
			}
			
			value = Values[index];
			return value != null;
		}

		// VALUE LONG
		public bool TryGetValue(out long value) => TryGetValue(0, out value);
		public bool TryGetValue(int index, out long value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Long)
				{
					value = oscValue.LongValue;
					return true;
				}
			}
			
			value = 0;
			return false;
		}
		
		// VALUE CHAR
		public bool TryGetValue(out char value) => TryGetValue(0, out value);
		public bool TryGetValue(int index, out char value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Char)
				{
					value = oscValue.CharValue;
					return true;
				}
			}
			
			value = '\0';
			return false;
		}
		
		// VALUE COLOR
		public bool TryGetValue(out OSCColor value) => TryGetValue(0, out value);
		public bool TryGetValue(int index, out OSCColor value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Color)
				{
					value = oscValue.ColorValue;
					return true;
				}
			}
			
			value = default;
			return false;
		}
		
		// VALUE BLOB
		public bool TryGetValue(out byte[] value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out byte[] value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Blob)
				{
					value = oscValue.BlobValue;
					return true;
				}
			}
			
			value = null;
			return false;
		}
		
		// VALUE INT
		public bool TryGetValue(out int value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out int value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Int)
				{
					value = oscValue.IntValue;
					return true;
				}
			}
			
			value = 0;
			return false;
		}
		
		// VALUE BOOL
		public bool TryGetValue(out bool value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out bool value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.True)
				{
					value = true;
					return true;
				}
				
				if (oscValue.Type == OSCValueType.False)
				{
					value = false;
					return true;
				}
			}
			
			value = false;
			return false;
		}
		
		// VALUE FLOAT
		public bool TryGetValue(out float value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out float value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Float)
				{
					value = oscValue.FloatValue;
					return true;
				}
			}
			
			value = 0;
			return false;
		}
		
		// VALUE DOUBLE
		public bool TryGetValue(out double value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out double value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Double)
				{
					value = oscValue.DoubleValue;
					return true;
				}
			}
			
			value = 0;
			return false;
		}
		
		// VALUE STRING
		public bool TryGetValue(out string value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out string value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.String)
				{
					value = oscValue.StringValue;
					return true;
				}
			}
			
			value = string.Empty;
			return false;
		}
		
		// VALUE NULL
		public bool TryGetNull() => TryGetNull(0);

		public bool TryGetNull(int index)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Null)
				{
					return true;
				}
			}
			
			return false;
		}
		
		// VALUE IMPULSE
		public bool TryGetImpulse() => TryGetImpulse(0);

		public bool TryGetImpulse(int index)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Impulse)
				{
					return true;
				}
			}
			
			return false;
		}
		
		// VALUE TIMETAG
		public bool TryGetValue(out DateTime value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out DateTime value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.TimeTag)
				{
					value = oscValue.TimeTagValue;
					return true;
				}
			}
			
			value = default;
			return false;
		}
		
		// VALUE MIDI
		public bool TryGetValue(out OSCMidi value) => TryGetValue(0, out value);

		public bool TryGetValue(int index, out OSCMidi value)
		{
			if (TryGetValue(index, out OSCValue oscValue))
			{
				if (oscValue.Type == OSCValueType.Midi)
				{
					value = oscValue.MidiValue;
					return true;
				}
			}
			
			value = default;
			return false;
		}
	}
}
/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;
using System.Net;
using System.Collections.Generic;
using extOSC.Core;

namespace extOSC
{
	public class OSCMessage : IOSCPacket
	{
		public static OSCMessage Create(string address, params OSCValue[] values)
		{
			return new OSCMessage(address, values);
		}
		

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

		public void AddValue(OSCValue value)
		{
			if (value == null)
				throw new NullReferenceException(nameof(value));

			Values.Add(value);
		}

		public OSCValue[] FindValues(params OSCValueType[] types)
		{
			var tempValues = new List<OSCValue>();

			foreach (var value in Values)
			{
				foreach (var type in types)
				{
					if (value.Type == type)
					{
						tempValues.Add(value);
					}
				}
			}

			return tempValues.ToArray();
		}
		
		public object Clone()
		{
			var valuesCount = Values.Count;
			var values = new OSCValue[valuesCount];

			for (var i = 0; i < valuesCount; ++i)
			{
				values[i] = Values[i].Copy();
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
	}
}
/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using UnityEngine;

using System;
using System.Collections.Generic;

namespace extOSC
{
	public class OSCValue
	{
		public static OSCValue Long(long value) => new OSCValue(OSCValueType.Long, value);

		public static OSCValue Char(char value) => new OSCValue(OSCValueType.Char, value);

		public static OSCValue Color(OSCColor value) => new OSCValue(OSCValueType.Color, value);

		public static OSCValue Blob(byte[] value) => new OSCValue(OSCValueType.Blob, value);

		public static OSCValue Int(int value) => new OSCValue(OSCValueType.Int, value);

		public static OSCValue Bool(bool value) => new OSCValue(value ? OSCValueType.True : OSCValueType.False, value);

		public static OSCValue Float(float value) => new OSCValue(OSCValueType.Float, value);

		public static OSCValue Double(double value) => new OSCValue(OSCValueType.Double, value);

		public static OSCValue String(string value) => new OSCValue(OSCValueType.String, value == null ? string.Empty : value);

		public static OSCValue Null() => new OSCValue(OSCValueType.Null, null);

		public static OSCValue Impulse() => new OSCValue(OSCValueType.Impulse, null);

		public static OSCValue TimeTag(DateTime value) => new OSCValue(OSCValueType.TimeTag, value);

		public static OSCValue Midi(OSCMidi value) => new OSCValue(OSCValueType.Midi, value);
		
		public static char GetTag(OSCValueType valueType)
		{
			switch (valueType)
			{
				case OSCValueType.Unknown:
					return 'N';
				case OSCValueType.Int:
					return 'i';
				case OSCValueType.Long:
					return 'h';
				case OSCValueType.True:
					return 'T';
				case OSCValueType.False:
					return 'F';
				case OSCValueType.Float:
					return 'f';
				case OSCValueType.Double:
					return 'd';
				case OSCValueType.String:
					return 's';
				case OSCValueType.Null:
					return 'N';
				case OSCValueType.Impulse:
					return 'I';
				case OSCValueType.Blob:
					return 'b';
				case OSCValueType.Char:
					return 'c';
				case OSCValueType.Color:
					return 'r';
				case OSCValueType.TimeTag:
					return 't';
				case OSCValueType.Midi:
					return 'm';
				// case OSCValueType.Array:
				// 	return 'N';
				default:
					return 'N';
			}
		}

		public static OSCValueType GetValueType(char valueTag)
		{
			switch (valueTag)
			{
				case 'i':
					return OSCValueType.Int;
				case 'h':
					return OSCValueType.Long;
				case 'T':
					return OSCValueType.True;
				case 'F':
					return OSCValueType.False;
				case 'f':
					return OSCValueType.Float;
				case 'd':
					return OSCValueType.Double;
				case 's':
					return OSCValueType.String;
				case 'N':
					return OSCValueType.Null;
				case 'I':
					return OSCValueType.Impulse;
				case 'b':
					return OSCValueType.Blob;
				case 'c':
					return OSCValueType.Char;
				case 'r':
					return OSCValueType.Color;
				case 't':
					return OSCValueType.TimeTag;
				case 'm':
					return OSCValueType.Midi;
				// case '[':
				// 	return OSCValueType.Array;
				//case ']':
				//	return OSCValueType.Array;
				default:
					return OSCValueType.Unknown;
			}
		}

		// INFO: Unused?
		// public static Type GetType(OSCValueType valueType)
		// {
		// 	if (valueType == OSCValueType.Unknown)
		// 		return null;
		// 	if (valueType == OSCValueType.Int)
		// 		return typeof(int);
		// 	if (valueType == OSCValueType.Long)
		// 		return typeof(long);
		// 	if (valueType == OSCValueType.True)
		// 		return typeof(bool);
		// 	if (valueType == OSCValueType.False)
		// 		return typeof(bool);
		// 	if (valueType == OSCValueType.Float)
		// 		return typeof(float);
		// 	if (valueType == OSCValueType.Double)
		// 		return typeof(double);
		// 	if (valueType == OSCValueType.String)
		// 		return typeof(string);
		// 	if (valueType == OSCValueType.Null)
		// 		return null;
		// 	if (valueType == OSCValueType.Impulse)
		// 		return null;
		// 	if (valueType == OSCValueType.Blob)
		// 		return typeof(byte[]);
		// 	if (valueType == OSCValueType.Char)
		// 		return typeof(char);
		// 	if (valueType == OSCValueType.Color)
		// 		return typeof(Color);
		// 	if (valueType == OSCValueType.TimeTag)
		// 		return typeof(DateTime);
		// 	if (valueType == OSCValueType.Midi)
		// 		return typeof(OSCMidi);
		// 	if (valueType == OSCValueType.Array)
		// 		return typeof(List<OSCValue>);
		//
		// 	return null;
		// }
		//
		// public static OSCValueType GetValueType(Type type)
		// {
		// 	if (type == typeof(int))
		// 		return OSCValueType.Int;
		// 	if (type == typeof(long))
		// 		return OSCValueType.Long;
		// 	if (type == typeof(bool))
		// 		return OSCValueType.False;
		// 	if (type == typeof(float))
		// 		return OSCValueType.Float;
		// 	if (type == typeof(double))
		// 		return OSCValueType.Double;
		// 	if (type == typeof(string))
		// 		return OSCValueType.String;
		// 	if (type == typeof(byte[]))
		// 		return OSCValueType.Blob;
		// 	if (type == typeof(char))
		// 		return OSCValueType.Char;
		// 	if (type == typeof(Color))
		// 		return OSCValueType.Color;
		// 	if (type == typeof(DateTime))
		// 		return OSCValueType.TimeTag;
		// 	if (type == typeof(OSCMidi))
		// 		return OSCValueType.Midi;
		// 	if (type == typeof(List<OSCValue>))
		// 		return OSCValueType.Array;
		//
		// 	return OSCValueType.Unknown;
		// }


		public OSCValueType Type => _type;
		public object Value => _value;
		public string Tag => GetTag(_type).ToString();

		
		public long LongValue
		{
			get => GetValue<long>(OSCValueType.Long);
			set => _value = value;
		}

		public char CharValue
		{
			get => GetValue<char>(OSCValueType.Char);
			set => _value = value;
		}

		public OSCColor ColorValue
		{
			get => GetValue<OSCColor>(OSCValueType.Color);
			set => _value = value;
		}

		public byte[] BlobValue
		{
			get => GetValue<byte[]>(OSCValueType.Blob);
			set => _value = value;
		}

		public int IntValue
		{
			get => GetValue<int>(OSCValueType.Int);
			set => _value = value;
		}

		public bool BoolValue
		{
			get => _type == OSCValueType.True;
			set => _type = value ? OSCValueType.True : OSCValueType.False;
		}

		public float FloatValue
		{
			get => GetValue<float>(OSCValueType.Float);
			set => _value = value;
		}

		public double DoubleValue
		{
			get => GetValue<double>(OSCValueType.Double);
			set => _value = value;
		}

		public string StringValue
		{
			get => GetValue<string>(OSCValueType.String);
			set => _value = value;
		}

		public bool IsNull => _type == OSCValueType.Null;

		public bool IsImpulse => _type == OSCValueType.Impulse;

		public DateTime TimeTagValue
		{
			get => GetValue<DateTime>(OSCValueType.TimeTag);
			set => _value = value;
		}

		public OSCMidi MidiValue
		{
			get => GetValue<OSCMidi>(OSCValueType.Midi);
			set => _value = value;
		}
		
		private OSCValueType _type;
		private object _value;


		public OSCValue(OSCValueType type, object value)
		{
			_value = value;
			_type = type;
		}

		public OSCValue Copy() => new OSCValue(_type, _value);

		public override string ToString()
		{
			if (_type == OSCValueType.True  ||
			    _type == OSCValueType.False ||
			    _type == OSCValueType.Null  ||
			    _type == OSCValueType.Impulse)
			{
				return $"<OSCValue {Tag}>";
			}

			return $"<OSCValue {Tag}: {_value}>";
		}

		private T GetValue<T>(OSCValueType requiredType)
		{
			if (requiredType == _type)
			{
				return (T) _value;
			}

			return default;
		}
	}
}
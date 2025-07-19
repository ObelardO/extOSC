/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;
using System.Runtime.InteropServices;

namespace extOSC
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct OSCMidi
	{
		[FieldOffset(0)] 
		private int _midi;
		[FieldOffset(0)]
		public byte Channel;
		[FieldOffset(1)]
		public byte Status;
		[FieldOffset(2)]
		public byte Data1;
		[FieldOffset(3)]
		public byte Data2;

		public OSCMidi(byte channel, byte status, byte data1, byte data2)
		{
			_midi = 0;
			Channel = channel;
			Status = status;
			Data1 = data1;
			Data2 = data2;
		}

		public override int GetHashCode() => _midi.GetHashCode();
		public override bool Equals(object other) => other is OSCMidi midi && Equals(midi);
		public bool Equals(OSCMidi other) => _midi == other._midi;

		public override string ToString()
		{
			return $"MIDI({Channel}, {Status}, {Data1}, {Data2})";
		}
	}
}
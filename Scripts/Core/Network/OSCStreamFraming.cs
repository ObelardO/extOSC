/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;

namespace extOSC.Core.Network
{
	internal static class OSCStreamFraming
	{
		#region Public Vars

		public const int MaxPacketSize = 65507;

		public const byte SlipEnd = 0xC0;

		public const byte SlipEsc = 0xDB;

		public const byte SlipEscEnd = 0xDC;

		public const byte SlipEscEsc = 0xDD;

		#endregion

		#region Public Methods

		public static int GetMaxEncodedSize(int packetLength)
		{
			return 2 + packetLength * 2;
		}

		public static int Encode(OSCTcpFraming framing, byte[] packet, int length, byte[] output)
		{
			if (packet == null)
				throw new ArgumentNullException(nameof(packet));
			if (output == null)
				throw new ArgumentNullException(nameof(output));
			if (length < 0 || length > packet.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			if (framing == OSCTcpFraming.SLIP)
				return EncodeSlip(packet, length, output);

			return EncodeSizePreamble(packet, length, output);
		}

		#endregion

		#region Private Methods

		private static int EncodeSizePreamble(byte[] packet, int length, byte[] output)
		{
			if (output.Length < length + 4)
				throw new ArgumentException("Output buffer is too small.", nameof(output));

			output[0] = (byte) (length >> 24);
			output[1] = (byte) (length >> 16);
			output[2] = (byte) (length >> 8);
			output[3] = (byte) length;

			Buffer.BlockCopy(packet, 0, output, 4, length);

			return length + 4;
		}

		private static int EncodeSlip(byte[] packet, int length, byte[] output)
		{
			if (output.Length < GetMaxEncodedSize(length))
				throw new ArgumentException("Output buffer is too small.", nameof(output));

			var index = 0;
			output[index++] = SlipEnd;

			for (var i = 0; i < length; i++)
			{
				var value = packet[i];

				if (value == SlipEnd)
				{
					output[index++] = SlipEsc;
					output[index++] = SlipEscEnd;
				}
				else if (value == SlipEsc)
				{
					output[index++] = SlipEsc;
					output[index++] = SlipEscEsc;
				}
				else
				{
					output[index++] = value;
				}
			}

			output[index++] = SlipEnd;

			return index;
		}

		#endregion
	}

	internal class OSCStreamDecoder
	{
		#region Public Vars

		public OSCTcpFraming Framing
		{
			get => _framing;
			set
			{
				if (_framing == value)
					return;

				_framing = value;
				Reset();
			}
		}

		#endregion

		#region Private Vars

		private OSCTcpFraming _framing;

		private readonly byte[] _packetBuffer = new byte[OSCStreamFraming.MaxPacketSize];

		private readonly byte[] _lengthBuffer = new byte[4];

		private int _lengthBytes;

		private int _bodyExpected;

		private int _bodyReceived;

		private bool _slipInPacket;

		private bool _slipEscaped;

		private bool _slipDiscard;

		#endregion

		#region Public Methods

		public OSCStreamDecoder(OSCTcpFraming framing)
		{
			_framing = framing;
		}

		public void Reset()
		{
			_lengthBytes = 0;
			_bodyExpected = 0;
			_bodyReceived = 0;
			_slipInPacket = false;
			_slipEscaped = false;
			_slipDiscard = false;
		}

		public bool Feed(byte[] data, int offset, int count, Action<byte[], int> onPacket)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (onPacket == null)
				throw new ArgumentNullException(nameof(onPacket));
			if (offset < 0 || count < 0 || offset + count > data.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_framing == OSCTcpFraming.SLIP)
				return FeedSlip(data, offset, count, onPacket);

			return FeedSizePreamble(data, offset, count, onPacket);
		}

		#endregion

		#region Private Methods

		private bool FeedSizePreamble(byte[] data, int offset, int count, Action<byte[], int> onPacket)
		{
			var end = offset + count;

			for (var i = offset; i < end; i++)
			{
				if (_bodyExpected == 0)
				{
					_lengthBuffer[_lengthBytes++] = data[i];

					if (_lengthBytes < 4)
						continue;

					_bodyExpected = (_lengthBuffer[0] << 24) | (_lengthBuffer[1] << 16) | (_lengthBuffer[2] << 8) | _lengthBuffer[3];
					_lengthBytes = 0;
					_bodyReceived = 0;

					if (_bodyExpected <= 0 || _bodyExpected > OSCStreamFraming.MaxPacketSize)
					{
						Reset();
						return false;
					}

					continue;
				}

				_packetBuffer[_bodyReceived++] = data[i];

				if (_bodyReceived != _bodyExpected)
					continue;

				onPacket(_packetBuffer, _bodyExpected);
				_bodyExpected = 0;
				_bodyReceived = 0;
			}

			return true;
		}

		private bool FeedSlip(byte[] data, int offset, int count, Action<byte[], int> onPacket)
		{
			var end = offset + count;

			for (var i = offset; i < end; i++)
			{
				var value = data[i];

				if (!_slipInPacket)
				{
					if (value == OSCStreamFraming.SlipEnd)
					{
						_slipInPacket = true;
						_slipEscaped = false;
						_slipDiscard = false;
						_bodyReceived = 0;
					}

					continue;
				}

				if (_slipEscaped)
				{
					if (!_slipDiscard)
					{
						if (value == OSCStreamFraming.SlipEscEnd)
							value = OSCStreamFraming.SlipEnd;
						else if (value == OSCStreamFraming.SlipEscEsc)
							value = OSCStreamFraming.SlipEsc;

						if (!AppendSlipByte(value))
							_slipDiscard = true;
					}

					_slipEscaped = false;
					continue;
				}

				if (value == OSCStreamFraming.SlipEsc)
				{
					_slipEscaped = true;
					continue;
				}

				if (value == OSCStreamFraming.SlipEnd)
				{
					if (!_slipDiscard && _bodyReceived > 0)
						onPacket(_packetBuffer, _bodyReceived);

					_slipInPacket = false;
					_slipEscaped = false;
					_slipDiscard = false;
					_bodyReceived = 0;
					continue;
				}

				if (!_slipDiscard && !AppendSlipByte(value))
					_slipDiscard = true;
			}

			return true;
		}

		private bool AppendSlipByte(byte value)
		{
			if (_bodyReceived >= OSCStreamFraming.MaxPacketSize)
				return false;

			_packetBuffer[_bodyReceived++] = value;
			return true;
		}

		#endregion
	}
}

/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;

namespace extOSC.Core
{
	public enum OSCConsolePacketType
	{
		Received,

		Transmitted,

		Queued
	}

	public enum OSCConsoleQueueState
	{
		Pending,

		Sent,

		Dropped,

		// Restored from a log file: the queue that owned this packet no longer exists.
		Unknown
	}

	public class OSCConsolePacket
	{
		#region Public Vars

		public IOSCPacket Packet
		{
			get => _packet;
			set
			{
				_packet = value;
				_description = null;
			}
		}

		public OSCConsolePacketType PacketType
		{
			get => _packetType;
			set
			{
				_packetType = value;
				_description = null;
			}
		}

		public string Info
		{
			get => _info;
			set
			{
				_info = value;
				_description = null;
			}
		}

		public OSCConsoleQueueState QueueState
		{
			get => _queueState;
			set
			{
				_queueState = value;
				_description = null;
			}
		}

		public string ResolveTimeStamp
		{
			get => _resolveTimeStamp;
			set
			{
				_resolveTimeStamp = value;
				_description = null;
			}
		}

		public string TimeStamp
		{
			get => _timeStamp;
			set => _timeStamp = value;
		}

		#endregion

		#region Private Vars

		private IOSCPacket _packet;

		private OSCConsolePacketType _packetType;

		private string _info;

		private string _description;

		private string _timeStamp;

		private OSCConsoleQueueState _queueState;

		private string _resolveTimeStamp;

		#endregion

		#region Public Methods

#if UNITY_EDITOR
		public override string ToString()
		{
			if (_description == null && _packet != null)
			{
				var packetDescription = string.Empty;

				if (_packet is OSCMessage)
				{
					packetDescription = $"<color=orange>Message:</color> {_packet.Address}";
				}
				else if (_packet is OSCBundle bundle)
				{
					packetDescription = $"<color=yellow>Bundle:</color> (Packets: {bundle.Packets.Count})";
				}

				_description = packetDescription + "\n" + _info + GetQueueStateDescription();
			}

			return _description;
		}
#endif

		#endregion

		#region Private Methods

#if UNITY_EDITOR
		private string GetQueueStateDescription()
		{
			if (_packetType != OSCConsolePacketType.Queued)
				return string.Empty;

			switch (_queueState)
			{
				case OSCConsoleQueueState.Sent:
					return $" <color=green>[sent {_resolveTimeStamp}]</color>";
				case OSCConsoleQueueState.Dropped:
					return $" <color=red>[dropped {_resolveTimeStamp}]</color>";
				case OSCConsoleQueueState.Unknown:
					return " <color=grey>[unknown]</color>";
				default:
					return " <color=orange>[pending]</color>";
			}
		}
#endif

		#endregion
	}
}
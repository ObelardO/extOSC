/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;
using System.Net;
using System.Collections.Generic;

using extOSC.Core;

namespace extOSC
{
	public class OSCBundle : IOSCPacket
	{
		#region Constants

		public const string BundleAddress = "#bundle";

		#endregion

		#region Public Vars

		public string Address => "#bundle";
		
		public IPEndPoint From
		{
			get => _from;
			set
			{
				_from = value;

				for (var i = 0; i < Packets.Count; i++)
					Packets[i].From = value;
			}
		}

		public List<IOSCPacket> Packets { get; }

		public DateTime TimeStamp { get; set; }

		#endregion

		#region Private Vars

		private IPEndPoint _from;

		#endregion

		#region Public Methods

		public OSCBundle()
		{
			Packets = new List<IOSCPacket>();
		}

		public OSCBundle(params IOSCPacket[] packets)
		{
			Packets = new List<IOSCPacket>(packets);
		}

		public void Append(IOSCPacket packet)
		{
			if (packet == null)
				throw new NullReferenceException(nameof(packet));

			Packets.Add(packet);
		}

		// TODO: Optimize.
		public object Clone()
		{
			var packetsCount = Packets.Count;
			var packets = new IOSCPacket[packetsCount];

			for (var i = 0; i < packetsCount; ++i)
			{
				packets[i] = Packets[i].Clone() as IOSCPacket;
			}

			return new OSCBundle(packets);
		}

		public override string ToString()
		{
			var stringValues = string.Empty;

			if (Packets.Count > 0)
			{
				foreach (var packet in Packets)
				{
					stringValues += $"[{packet}], ";
				}

				stringValues = $"({stringValues.Remove(stringValues.Length - 2)})";
			}

			return $"<{GetType().Name}> : {(string.IsNullOrEmpty(stringValues) ? "null" : stringValues)}";
		}

		#endregion
	}
}
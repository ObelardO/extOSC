/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;
using System.Collections.Generic;

namespace extOSC.Core
{
    public static class OSCConsole
    {
        #region Public Vars

        public static List<OSCConsolePacket> ConsoleBuffer { get; set; } = new List<OSCConsolePacket>();

		public static bool LogConsole { get; set; } = false;

		#endregion

        #region Public Methods

        public static void Received(OSCReceiver receiver, IOSCPacket packet)
        {
			var ip = packet.Ip != null ? $"{packet.Ip}:{packet.Port}" : "Debug";
			var protocol = GetProtocolLabel(receiver.Protocol, receiver.TcpFraming);

			var consolePacket = new OSCConsolePacket();
			consolePacket.Info = $"Receiver: {receiver.LocalPort} [{protocol}]. From: {ip}";
			consolePacket.TimeStamp = DateTime.Now.ToString("[HH:mm:ss]");
            consolePacket.PacketType = OSCConsolePacketType.Received;
            consolePacket.Packet = packet;

            Log(consolePacket);
        }

        public static void Transmitted(OSCTransmitter transmitter, IOSCPacket packet)
        {
            var protocol = GetProtocolLabel(transmitter.Protocol, transmitter.TcpFraming);

            var consolePacket = new OSCConsolePacket();
            consolePacket.Info = $"Transmitter: {transmitter.RemoteHost}:{transmitter.RemotePort} [{protocol}]";
			consolePacket.TimeStamp = DateTime.Now.ToString("[HH:mm:ss]");
            consolePacket.PacketType = OSCConsolePacketType.Transmitted;
            consolePacket.Packet = packet;

            Log(consolePacket);
        }

        public static void Queued(OSCTransmitter transmitter, IOSCPacket packet)
        {
            var protocol = GetProtocolLabel(transmitter.Protocol, transmitter.TcpFraming);

            var consolePacket = new OSCConsolePacket();
            consolePacket.Info = $"Transmitter: {transmitter.RemoteHost}:{transmitter.RemotePort} [{protocol}] (queued, no connection)";
            consolePacket.TimeStamp = DateTime.Now.ToString("[HH:mm:ss]");
            consolePacket.PacketType = OSCConsolePacketType.Queued;
            consolePacket.Packet = packet;

            Log(consolePacket);
        }

        #endregion

        #region Private Methods

        private static string GetProtocolLabel(OSCProtocol protocol, OSCTcpFraming framing)
        {
            if (protocol != OSCProtocol.TCP)
                return "UDP";

            return framing == OSCTcpFraming.SLIP ? "TCP/OSC 1.1" : "TCP/OSC 1.0";
        }

        private static void Log(OSCConsolePacket consolePacket)
        {
#if UNITY_EDITOR
            // COPY PACKET
	        consolePacket.Packet = consolePacket.Packet.Copy();
            
            ConsoleBuffer.Add(consolePacket);
#else
            if (LogConsole)
            {
                UnityEngine.Debug.Log($"[OSCConsole] Packed {consolePacket.PacketType}: {consolePacket.Packet}");
            }
#endif
        }

        #endregion
    }
}
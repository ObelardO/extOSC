using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;

namespace extOSC.Core
{
    public class PeerException : Exception
    {
        // TODO: Populate from peer.
        public string Operation { get; }
        public IPEndPoint LocalEndPoint { get; }
        public IPEndPoint RemoteEndPoint { get; }
        public DateTime ErrorTime { get; } = DateTime.UtcNow;
        
        public PeerException() {}
        public PeerException(string message) : base(message) {}
        public PeerException(string message, Exception innerException) : base(message, innerException) {}

        public PeerException(
            OSCPeer peer,
            string message,
            Exception innerException = null) : base(FormatMessage(peer, message), innerException)
        {
            LocalEndPoint = null;
            RemoteEndPoint = null;
        }

        protected PeerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
            Operation = info.GetString("Operation");
            LocalEndPoint = (IPEndPoint)info.GetValue("LocalEndPoint", typeof(IPEndPoint));
            RemoteEndPoint = (IPEndPoint)info.GetValue("RemoteEndPoint", typeof(IPEndPoint));
        }
        
        public override void GetObjectData(
            SerializationInfo info, 
            StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(Operation), Operation);
            info.AddValue(nameof(LocalEndPoint), LocalEndPoint);
            info.AddValue(nameof(RemoteEndPoint), RemoteEndPoint);
        }

        private static string FormatMessage(
            OSCPeer peer,
            string message)
        {
            return $"{message}";
        }
    }
}
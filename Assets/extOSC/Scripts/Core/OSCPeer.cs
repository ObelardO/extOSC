using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace extOSC.Core
{
    public class ConnectOptions
    {
        public IPAddress LocalIPAddress = IPAddress.Any;
        public ushort LocalPort = 0;
        
        public bool UseMulticast = false;
        public IPAddress MulticastIPAddress = IPAddress.Any;

        public int BufferSize = 4096; // Require research
        public int Ttl = 255; // Time to live
 
    }
    
    public class OSCPeer : IDisposable
    {
        private Socket _socket;
        private IPEndPoint _remoteIpEndPoint;
        
        private readonly MemoryStream _memoryStream = new MemoryStream();

        public void Connect(ConnectOptions options)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            // Setup socket layer.
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, 0);  // Multiple peers on one port.
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, options.BufferSize);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0);
            
            // Setup ip layer.
            if (_socket.AddressFamily == AddressFamily.InterNetwork)
            {
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, options.Ttl);
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, 1);
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, 0);
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, options.Ttl);
            }
            else // currently unused
            {
                _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IpTimeToLive, options.Ttl); 
                _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, 0);
                _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, options.Ttl);
            }

            if (options.UseMulticast)
            {
                var multicastOption = new MulticastOption(options.MulticastIPAddress);
                _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
            }
        }

        public void SetRemoteTarget(IPEndPoint remote)
        {
            _remoteIpEndPoint = remote;
            
            var value = Equals(_remoteIpEndPoint.Address, IPAddress.Broadcast) ? 1 : 0;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, value);
        }

        // TODO: Окультурить ошибки.
        public void TrySendPacket(IOSCPacket packet)
        {
            try
            {
                // Reset memory stream.
                _memoryStream.Position = 0;
                _memoryStream.SetLength(0);

                OSCWriter.Write(_memoryStream, packet, false);

                _socket.SendTo(_memoryStream.GetBuffer(), _remoteIpEndPoint);
            }
            catch (SocketException socketException)
            {
                
            }
            catch (Exception ex)
            {
                
            }
        }

        public IOSCPacket IsReceive()
        {
            
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _socket = null;
        }
    }
}
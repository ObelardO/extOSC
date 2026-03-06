using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using JetBrains.Annotations;
using UnityEngine;

namespace extOSC.Core
{
    
    public class ConnectOptions
    {
        public IPAddress LocalIPAddress = IPAddress.Any;
        public ushort LocalPort = 0;
        
        public IPAddress MulticastIPAddress = null;

        public int BufferSize = 4096; // Require research
        public int Ttl = 255; // Time to live
    }
    
    public class OSCPeer : IDisposable
    {
        private Socket _socket;
        private IPEndPoint _localIpEndPoint;
        private IPEndPoint _remoteIpEndPoint;
        private IPAddress _multicastIPAddress;
        private int _bufferSize = 4096;
        private int _ttl = 255;

        private readonly MemoryStream _memoryStream = new MemoryStream();
        
        public IPEndPoint LocalEndPoint
        {
            get => _localIpEndPoint;
            set
            {
                ConnectValidateProperty();
                _localIpEndPoint = value;
            }
        }

        public IPEndPoint RemoteEndPoint
        {
            get => _remoteIpEndPoint;
            set
            {
                _remoteIpEndPoint = value;
                BroadcastOptionRefresh();
            }
        }

        public IPAddress MulticastIPAddress
        {
            get =>  _multicastIPAddress;
            set
            {
                ConnectValidateProperty();
                _multicastIPAddress = value;
            }
        }

        public int BufferSize
        {
            get => _bufferSize;
            set
            {
                ConnectValidateProperty();
                _bufferSize = value;
            }
        }

        public int Ttl
        {
            get => _ttl;
            set 
            {
                ConnectValidateProperty();
                _ttl = value;
            }
        }

        public OSCPeer()
        {
            
        }

        public void Connect()
        {
            try
            {
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Setup socket layer.
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, 0); // Multiple peers on one port.
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1); // Multiple peers on one port.
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, _bufferSize);
                _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 0);

                // Setup ip layer.
                if (_socket.AddressFamily == AddressFamily.InterNetwork)
                {
                    _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.IpTimeToLive, _ttl);
                    _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, 1);

                    // Multicast support.
                    _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, 0);
                    _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, _ttl);

                    if (_multicastIPAddress != null)
                    {
                        var multicastOption = new MulticastOption(_multicastIPAddress);

                        _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, multicastOption);
                    }
                }
                else if (_socket.AddressFamily == AddressFamily.InterNetworkV6) // currently unused
                {
                    _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IpTimeToLive, _ttl);

                    // Multicast support.
                    _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, 0);
                    _socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastTimeToLive, _ttl);

                    if (_multicastIPAddress != null)
                    {
                        // Currently IPv6 not support for multicast in .NET.
                        // var multicastOption = new IPv6MulticastOption(_multicastIPAddress);

                        throw new PeerException(this, "IPv6 Multicast not supported");
                    }
                }
                else
                {
                    // TODO ERR: Unknown AddressFamily
                }
                
                BroadcastOptionRefresh();

                _socket.Bind(_localIpEndPoint);
            }
            catch (SocketException socketException)
            {
                // > If you want to write portable code, always use SocketException.SocketErrorCode and compare it with the values of SocketError.
                // > Never use raw numerical error codes.
                // https://blog.jetbrains.com/dotnet/2020/04/27/socket-error-codes-depend-runtime-operating-system/
                if (socketException.SocketErrorCode == SocketError.AddressAlreadyInUse)
                {
                    Debug.LogWarningFormat("Unable to send message. Local address already in use: {0}", _localIpEndPoint);
                }
            }
            catch (Exception e)
            {
                // TODO: More
                Debug.LogException(e);
            }
        }
        
        private void BroadcastOptionRefresh()
        {
            if (_socket == null)
                return;
            
            var value = Equals(_remoteIpEndPoint.Address, IPAddress.Broadcast) ? 1 : 0;
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, value);
        }

        public void Close()
        {
            if (_socket == null) 
                return;

            // Drop multicast membership.
            if (_multicastIPAddress != null)
            {
                try
                {
                    if (_socket.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var multicastOption = new MulticastOption(_multicastIPAddress);
                        
                        _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, multicastOption);
                    }
                    else if (_socket.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // Currently IPv6 not support for multicast in .NET.
                        // var multicastOption = new IPv6MulticastOption(_multicastIPAddress);
                        
                        throw new PeerException(this, "IPv6 Multicast not supported. Unknown behaviour.");
                    }
                }
                catch (Exception e)
                {
                    // Ignore
                }
               
            }
        }
        
        public void Dispose()
        {
            _socket?.Dispose();
            _socket = null;
        }

        // TODO: Remove or replace?
        
        
        public void SendPacket(IOSCPacket packet)
        {
            if (_socket == null)
                throw new PeerException(this, "Peer is not connected.");
            
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
                if (socketException.ErrorCode == (int)SocketError.MessageSize)
                {
                    Debug.LogWarning($"Unable to send message. Message too long. Packet size: {_memoryStream.Length}. UDP buffer size: {_socket.SendBufferSize}.");
                }
                else if (socketException.SocketErrorCode == SocketError.AddressNotAvailable ||
                         socketException.SocketErrorCode == SocketError.NetworkUnreachable ||
                         socketException.SocketErrorCode == SocketError.ConnectionRefused ||
                         socketException.SocketErrorCode == SocketError.HostDown || 
                         socketException.SocketErrorCode == SocketError.HostUnreachable)
                {
                    // TODO: Add more LogWarning invokes.
                    // Ignore.
                }
                else
                {
                    Debug.LogWarning($"Unable to send message. Unknown socket exception: {socketException}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Unable to send message. Unknown exception: {exception}");
            }
        }

        public IOSCPacket IsReceivePacket()
        {
            return null;
        }
        
        
        // Utils
        private void ConnectValidateProperty()
        {
            if (_socket != null)
                throw new Exception("Cannot change property while peer is connected.");
        }
        
        // Other
        public override string ToString()
        {
            return $"<OSCPeer>";
        }

        public string ToString(bool verbose)
        {
            if (verbose)
            {
                // TODO: Add verbose ToString.
                return ToString();
            }

            return ToString();
        }
    }
}
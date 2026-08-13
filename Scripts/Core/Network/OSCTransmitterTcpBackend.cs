/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace extOSC.Core.Network
{
	internal class OSCTransmitterTcpBackend : OSCTransmitterBackend
	{
#if UNITY_WSA && !UNITY_EDITOR

		#region Public Vars

		public override bool IsAvailable => false;

		#endregion

		#region Public Methods

		public override void Connect(string localHost, int localPort)
		{
			Debug.LogError("[OSCTransmitter] TCP is not supported on UWP.");
		}

		public override void RefreshRemote(string remoteHost, int remotePort)
		{ }

		public override void Close()
		{ }

		public override void Send(byte[] data, int length)
		{ }

		#endregion

#else

		#region Public Vars

		public override bool IsAvailable => _sessionActive;

		public override bool IsConnected
		{
			get
			{
				lock (_lock)
				{
					return _sessionActive && _isConnected && _stream != null;
				}
			}
		}

		#endregion

		#region Private Vars

		private const int kMaxQueuedPackets = 32;

		private TcpClient _client;

		private NetworkStream _stream;

		private IPEndPoint _remoteEndPoint;

		private IPEndPoint _localEndPoint;

		private bool _sessionActive;

		private bool _isConnected;

		private bool _connecting;

		private float _connectStartedAt;

		private readonly object _lock = new object();

		private readonly byte[] _encodeBuffer = new byte[OSCStreamFraming.GetMaxEncodedSize(OSCStreamFraming.MaxPacketSize)];

		private readonly Queue<byte[]> _sendQueue = new Queue<byte[]>();

		private readonly Queue<bool> _queueResults = new Queue<bool>();

		#endregion

		#region Public Methods

		public override void Connect(string localHost, int localPort)
		{
			lock (_lock)
			{
				CloseSockets();
				DropQueue();
				_connecting = false;
				_isConnected = false;

				if (_remoteEndPoint == null)
				{
					Debug.LogError("[OSCTransmitter] Remote endpoint is not set.");
					_sessionActive = false;
					return;
				}

				try
				{
					_localEndPoint = new IPEndPoint(IPAddress.Parse(localHost), localPort);
					_sessionActive = true;
					BeginConnect();
				}
				catch (SocketException e)
				{
					if (e.ErrorCode == 10048)
					{
						Debug.LogError($"[OSCTransmitter] Socket Error: Could not use local port {localPort} because another application is listening on it.");
					}
					else if (e.ErrorCode == 10049)
					{
						Debug.LogError($"[OSCTransmitter] Socket Error: Could not use local host \"{localHost}\". Cannot assign requested address.");
					}
					else
					{
						Debug.LogError($"[OSCTransmitter] Socket Error: Error Code {e.ErrorCode}.");
					}

					_sessionActive = false;
					CloseSockets();
				}
				catch (ArgumentOutOfRangeException)
				{
					Debug.LogError($"[OSCTransmitter] Invalid port: {localPort}");
					_sessionActive = false;
					CloseSockets();
				}
				catch (Exception)
				{
					Debug.LogError("[OSCTransmitter] Error while opening TCP socket.");
					_sessionActive = false;
					CloseSockets();
				}
			}
		}

		public override void RefreshRemote(string remoteHost, int remotePort)
		{
			_remoteEndPoint = new IPEndPoint(IPAddress.Parse(remoteHost), remotePort);
		}

		public override void Close()
		{
			lock (_lock)
			{
				_sessionActive = false;
				_connecting = false;
				_isConnected = false;
				DropQueue();
				CloseSockets();
			}
		}

		public override void Send(byte[] data, int length)
		{
			lock (_lock)
			{
				if (!_sessionActive) return;

				var encodedLength = OSCStreamFraming.Encode(TcpFraming, data, length, _encodeBuffer);

				if (_isConnected && _stream != null)
				{
					try
					{
						_stream.Write(_encodeBuffer, 0, encodedLength);
						return;
					}
					catch (Exception)
					{
						CloseSockets();
						_isConnected = false;
					}
				}

				Enqueue(_encodeBuffer, encodedLength);

				if (!_connecting && !_isConnected)
					BeginConnect();
			}
		}

		public override void Tick()
		{
			if (TcpReconnectTimeout <= 0f) return;

			lock (_lock)
			{
				if (!_connecting || _isConnected) return;

				if (Time.realtimeSinceStartup - _connectStartedAt < TcpReconnectTimeout)
					return;

				CloseSockets();
				_connecting = false;
				DropQueue();
			}
		}

		public override bool TryTakeQueueResult(out bool sent)
		{
			lock (_lock)
			{
				if (_queueResults.Count == 0)
				{
					sent = false;

					return false;
				}

				sent = _queueResults.Dequeue();

				return true;
			}
		}

		#endregion

		#region Private Methods

		private void BeginConnect()
		{
			if (!_sessionActive || _connecting || _isConnected) return;
			if (_remoteEndPoint == null || _localEndPoint == null) return;

			CloseSockets();

			try
			{
				_client = new TcpClient();
				_client.NoDelay = true;
				_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				_client.Client.Bind(_localEndPoint);

				_connecting = true;
				_connectStartedAt = Time.realtimeSinceStartup;
				_client.BeginConnect(_remoteEndPoint.Address, _remoteEndPoint.Port, ConnectCallback, _client);
			}
			catch (Exception)
			{
				_connecting = false;
				DropQueue();
				CloseSockets();
			}
		}

		private void ConnectCallback(IAsyncResult result)
		{
			var client = result.AsyncState as TcpClient;

			try
			{
				if (client == null) return;

				client.EndConnect(result);

				lock (_lock)
				{
					if (!_sessionActive || client != _client)
					{
						try
						{
							client.Close();
						}
						catch (Exception)
						{ }

						return;
					}

					_stream = client.GetStream();
					_isConnected = true;
					_connecting = false;
					FlushQueue();
				}
			}
			catch (ObjectDisposedException)
			{
				lock (_lock)
				{
					if (client != null && client == _client)
					{
						_connecting = false;
						DropQueue();
					}
				}
			}
			catch (Exception)
			{
				lock (_lock)
				{
					if (client != null && client == _client)
					{
						CloseSockets();
						_isConnected = false;
						_connecting = false;
						DropQueue();
					}
				}
			}
		}

		private void Enqueue(byte[] data, int length)
		{
			while (_sendQueue.Count >= kMaxQueuedPackets)
			{
				_sendQueue.Dequeue();
				_queueResults.Enqueue(false);
			}

			var copy = new byte[length];
			Buffer.BlockCopy(data, 0, copy, 0, length);
			_sendQueue.Enqueue(copy);
		}

		private void FlushQueue()
		{
			if (_stream == null) return;

			while (_sendQueue.Count > 0)
			{
				var packet = _sendQueue.Peek();

				try
				{
					_stream.Write(packet, 0, packet.Length);
				}
				catch (Exception)
				{
					CloseSockets();
					_isConnected = false;
					_connecting = false;
					DropQueue();

					return;
				}

				_sendQueue.Dequeue();
				_queueResults.Enqueue(true);
			}
		}

		private void DropQueue()
		{
			while (_sendQueue.Count > 0)
			{
				_sendQueue.Dequeue();
				_queueResults.Enqueue(false);
			}
		}

		private void CloseSockets()
		{
			if (_stream != null)
			{
				try
				{
					_stream.Close();
				}
				catch (Exception)
				{ }

				_stream = null;
			}

			if (_client != null)
			{
				try
				{
					_client.Close();
				}
				catch (Exception)
				{ }

				_client = null;
			}
		}

		#endregion

#endif
	}
}

/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using extOSC.Core;

namespace extOSC.Core.Network
{
	internal class OSCReceiverTcpBackend : OSCReceiverBackend
	{
#if UNITY_WSA && !UNITY_EDITOR

		#region Public Vars

		public override OSCReceivedCallback ReceivedCallback
		{
			get => _receivedCallback;
			set => _receivedCallback = value;
		}

		public override bool IsAvailable => false;

		public override bool IsRunning => false;

		#endregion

		#region Private Vars

		private OSCReceivedCallback _receivedCallback;

		#endregion

		#region Public Methods

		public override void Connect(string localHost, int localPort)
		{
			Debug.LogError("[OSCReceiver] TCP is not supported on UWP.");
		}

		public override void Close()
		{ }

		#endregion

#else

		#region Nested Types

		private class ClientState
		{
			public TcpClient Client;

			public NetworkStream Stream;

			public byte[] Buffer;

			public OSCStreamDecoder Decoder;

			public IPEndPoint RemoteEndPoint;
		}

		#endregion

		#region Public Vars

		public override OSCReceivedCallback ReceivedCallback
		{
			get => _receivedCallback;
			set => _receivedCallback = value;
		}

		public override bool IsAvailable => _listener != null;

		public override bool IsRunning => _isRunning;

		#endregion

		#region Private Vars

		private const int kReadBufferSize = 8192;

		private bool _isRunning;

		private TcpListener _listener;

		private AsyncCallback _acceptCallback;

		private OSCReceivedCallback _receivedCallback;

		private readonly List<ClientState> _clients = new List<ClientState>();

		private readonly object _lock = new object();

		#endregion

		#region Public Methods

		public override void Connect(string localHost, int localPort)
		{
			if (_listener != null)
				Close();

			try
			{
				var localEndPoint = new IPEndPoint(IPAddress.Parse(localHost), localPort);

				_listener = new TcpListener(localEndPoint);
				_listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
				_listener.Start();

				_acceptCallback = AcceptCallback;
				_listener.BeginAcceptTcpClient(_acceptCallback, _listener);

				_isRunning = true;
			}
			catch (SocketException e)
			{
				if (e.ErrorCode == 10048)
				{
					Debug.LogErrorFormat($"[OSCReceiver] Socket Error: Could not use port {localPort} because another application is listening on it.");
				}
				else if (e.ErrorCode == 10049)
				{
					Debug.LogError($"[OSCReceiver] Socket Error: Could not use local host \"{localHost}\". Cannot assign requested address.");
				}
				else
				{
					Debug.LogErrorFormat($"[OSCReceiver] Socket Error: Error Code {e.ErrorCode}.");
				}

				Close();
			}
			catch (ArgumentOutOfRangeException)
			{
				Debug.LogErrorFormat($"[OSCReceiver] Invalid port: {localPort}!");

				Close();
			}
			catch (Exception)
			{
				Debug.LogError("[OSCReceiver] Error while opening TCP socket.");

				Close();
			}
		}

		public override void Close()
		{
			_isRunning = false;

			lock (_lock)
			{
				foreach (var state in _clients)
				{
					CloseClient(state);
				}

				_clients.Clear();
			}

			if (_listener != null)
			{
				try
				{
					_listener.Stop();
				}
				catch (Exception)
				{ }

				_listener = null;
			}
		}

		#endregion

		#region Private Methods

		private void AcceptCallback(IAsyncResult result)
		{
			if (!_isRunning) return;

			try
			{
				var listener = result.AsyncState as TcpListener;
				if (listener == null) return;

				var client = listener.EndAcceptTcpClient(result);
				client.NoDelay = true;

				var remote = client.Client.RemoteEndPoint as IPEndPoint;
				var state = new ClientState
				{
					Client = client,
					Stream = client.GetStream(),
					Buffer = new byte[kReadBufferSize],
					Decoder = new OSCStreamDecoder(TcpFraming),
					RemoteEndPoint = remote ?? new IPEndPoint(IPAddress.Any, 0)
				};

				lock (_lock)
				{
					_clients.Add(state);
				}

				state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, ReadCallback, state);

				if (IsAvailable)
					listener.BeginAcceptTcpClient(_acceptCallback, listener);
			}
			catch (ObjectDisposedException)
			{ }
			catch (Exception)
			{ }
		}

		private void ReadCallback(IAsyncResult result)
		{
			if (!_isRunning) return;

			var state = result.AsyncState as ClientState;
			if (state == null) return;

			try
			{
				var read = state.Stream.EndRead(result);
				if (read <= 0)
				{
					RemoveClient(state);
					return;
				}

				var valid = state.Decoder.Feed(state.Buffer, 0, read, (packetData, packetLength) =>
				{
					var packet = OSCConverter.Unpack(packetData, packetLength);
					if (packet == null)
						return;

					packet.Ip = state.RemoteEndPoint.Address;
					packet.Port = state.RemoteEndPoint.Port;

					if (_receivedCallback != null)
						_receivedCallback.Invoke(packet);
				});

				if (!valid)
				{
					Debug.LogWarning("[OSCReceiver] TCP client sent an invalid OSC frame and was disconnected.");
					RemoveClient(state);
					return;
				}

				if (_isRunning && state.Client.Connected)
					state.Stream.BeginRead(state.Buffer, 0, state.Buffer.Length, ReadCallback, state);
			}
			catch (ObjectDisposedException)
			{
				RemoveClient(state);
			}
			catch (Exception)
			{
				RemoveClient(state);
			}
		}

		private void RemoveClient(ClientState state)
		{
			lock (_lock)
			{
				_clients.Remove(state);
			}

			CloseClient(state);
		}

		private void CloseClient(ClientState state)
		{
			if (state == null) return;

			try
			{
				if (state.Stream != null)
					state.Stream.Close();
			}
			catch (Exception)
			{ }

			try
			{
				if (state.Client != null)
					state.Client.Close();
			}
			catch (Exception)
			{ }
		}

		#endregion

#endif
	}
}

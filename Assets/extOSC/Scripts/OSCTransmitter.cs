/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using UnityEngine;
using UnityEngine.Serialization;

using System;
using System.Collections.Generic;

using extOSC.Core;
using extOSC.Core.Network;

namespace extOSC
{
    [AddComponentMenu("extOSC/OSC Transmitter")]
	public class OSCTransmitter : OSCBase
	{
		#region Public Vars

		public override bool IsStarted => _transmitterBackend.IsAvailable;

		public OSCProtocol Protocol
		{
			get => _protocol;
			set
			{
				if (_protocol == value)
					return;

				var wasStarted = __transmitterBackend != null && __transmitterBackend.IsAvailable;

				if (wasStarted)
					Close();

				_protocol = value;
				RecreateBackend();

				if (wasStarted)
					Connect();
			}
		}

		public OSCTcpFraming TcpFraming
		{
			get => _tcpFraming;
			set
			{
				if (_tcpFraming == value)
					return;

				_tcpFraming = value;
				if (__transmitterBackend != null)
					__transmitterBackend.TcpFraming = _tcpFraming;

				if (_protocol == OSCProtocol.TCP && __transmitterBackend != null && __transmitterBackend.IsAvailable)
				{
					Close();
					Connect();
				}
			}
		}

		public OSCLocalHostMode LocalHostMode
		{
			get => _localHostMode;
			set
			{
				if (_localHostMode == value)
					return;

				_localHostMode = value;

				LocalRefresh();
			}
		}

		public OSCLocalPortMode LocalPortMode
		{
			get => _localPortMode;
			set
			{
				if (_localPortMode == value)
					return;

				_localPortMode = value;

				LocalRefresh();
			}
		}

		public OSCReceiver SourceReceiver
		{
			get => _localReceiver;
			set
			{
				if (_localReceiver == value)
					return;

				_localReceiver = value;

				LocalRefresh();
			}
		}

		public string LocalHost
		{
			get => GetLocalHost();
			set
			{
				if (_localHost == value)
					return;

				_localHost = value;

				LocalRefresh();
			}
		}

		public int LocalPort
		{
			get => GetLocalPort();
			set
			{
				if (_localPort == value)
					return;

				_localPort = value;

				LocalRefresh();
			}
		}

		public string RemoteHost
		{
			get => _remoteHost;
			set
			{
				if (_remoteHost == value)
					return;

				_remoteHost = value;

				RemoteRefresh();
			}
		}

		public int RemotePort
		{
			get => _remotePort;
			set
			{
				value = OSCUtilities.ClampPort(value);

				if (_remotePort == value)
					return;

				_remotePort = value;

				RemoteRefresh();
			}
		}

		public bool UseBundle
		{
			get => _useBundle;
			set => _useBundle = value;
		}

		public float TcpReconnectTimeout
		{
			get => _tcpReconnectTimeout;
			set
			{
				if (value < 0f)
					value = 0f;

				if (Mathf.Approximately(_tcpReconnectTimeout, value))
					return;

				_tcpReconnectTimeout = value;

				if (__transmitterBackend != null)
					__transmitterBackend.TcpReconnectTimeout = _tcpReconnectTimeout;
			}
		}

		#endregion

		#region Private Vars

		[SerializeField]
		private OSCProtocol _protocol = OSCProtocol.UDP;

		[SerializeField]
		private OSCTcpFraming _tcpFraming = OSCTcpFraming.SizePreamble;

		[SerializeField]
		[FormerlySerializedAs("localHostMode")]
		private OSCLocalHostMode _localHostMode = OSCLocalHostMode.Any;

		[SerializeField]
		[FormerlySerializedAs("localPortMode")]
		private OSCLocalPortMode _localPortMode = OSCLocalPortMode.Random;

		[OSCSelector]
		[SerializeField]
		[FormerlySerializedAs("localReceiver")]
		private OSCReceiver _localReceiver;

		[OSCHost]
		[SerializeField]
		[FormerlySerializedAs("localHost")]
		private string _localHost;

		[SerializeField]
		[FormerlySerializedAs("localPort")]
		private int _localPort = 7000;

		[OSCHost]
		[SerializeField]
		[FormerlySerializedAs("remoteHost")]
		private string _remoteHost = "127.0.0.1";

		[SerializeField]
		[FormerlySerializedAs("remotePort")]
		private int _remotePort = 7000;

		[SerializeField]
		[FormerlySerializedAs("useBundle")]
		private bool _useBundle;

		[SerializeField]
		private float _tcpReconnectTimeout = 3f;

		private readonly List<IOSCPacket> _bundleBuffer = new List<IOSCPacket>();

		// Console rows for packets the backend had to queue, in the same order as the backend queue.
		private readonly Queue<OSCConsolePacket> _queuedConsolePackets = new Queue<OSCConsolePacket>();

		private OSCTransmitterBackend _transmitterBackend
		{
			get
			{
				EnsureBackend();
				return __transmitterBackend;
			}
		}

		private OSCTransmitterBackend __transmitterBackend;

		private OSCProtocol _backendProtocol;

		#endregion

		#region Unity Methods

		protected virtual void Update()
		{
			if (__transmitterBackend != null)
				__transmitterBackend.Tick();

			DrainQueueResults();

			if (_bundleBuffer.Count > 0)
			{
				var bundle = new OSCBundle();

				foreach (var packet in _bundleBuffer)
				{
					bundle.AddPacket(packet);
				}

				Send(bundle);

				_bundleBuffer.Clear();
			}
		}

#if UNITY_EDITOR
		protected void OnValidate()
		{
			_remotePort = OSCUtilities.ClampPort(_remotePort);

			if (string.IsNullOrEmpty(_localHost))
				_localHost = OSCUtilities.GetLocalHost();

			if (_localPort > 0)
				_localPort = OSCUtilities.ClampPort(_localPort);

			if (_tcpReconnectTimeout < 0f)
				_tcpReconnectTimeout = 0f;

			var wasStarted = __transmitterBackend != null && __transmitterBackend.IsAvailable;
			if (wasStarted)
			{
				Close();
				Connect();
			}
			else if (__transmitterBackend != null)
			{
				_transmitterBackend.RefreshRemote(_remoteHost, _remotePort);
			}
		}
#endif

		#endregion

		#region Public Methods

		public override void Connect()
		{
			EnsureBackend();
			_transmitterBackend.TcpFraming = _tcpFraming;
			_transmitterBackend.TcpReconnectTimeout = _tcpReconnectTimeout;
			_transmitterBackend.RefreshRemote(_remoteHost, _remotePort);
			_transmitterBackend.Connect(GetLocalHost(), GetLocalPort());
		}

		public override void Close()
		{
			if (_transmitterBackend.IsAvailable)
				_transmitterBackend.Close();

			DrainQueueResults();
		}

		public override string ToString()
		{
			return $"<{nameof(OSCTransmitter)} (Protocol: {_protocol} LocalHost: {_localHost} LocalPort: {_localPort} | RemoteHost: {_remoteHost}, RemotePort: {_remotePort})>";
		}

		public void Send(IOSCPacket packet, OSCSendOptions options = OSCSendOptions.None)
		{
			if ((options & OSCSendOptions.IgnoreBundle) == 0)
			{
				if (_useBundle && packet is OSCMessage)
				{
					_bundleBuffer.Add(packet);

					return;
				}
			}

			if (!_transmitterBackend.IsAvailable)
				return;

			if ((options & OSCSendOptions.IgnoreMap) == 0)
			{
				if (MapBundle != null)
					MapBundle.Map(packet);
			}

			var length = OSCConverter.Pack(packet, out var buffer);
			
			_transmitterBackend.Send(buffer, length);

			if (_transmitterBackend.IsConnected)
				OSCConsole.Transmitted(this, packet);
			else
				_queuedConsolePackets.Enqueue(OSCConsole.Queued(this, packet));
		}

		#endregion

		#region Private Methods

		private void LocalRefresh()
		{
			if (IsStarted)
			{
				Close();
				Connect();
			}
		}

		private void RemoteRefresh()
		{
			if (_protocol == OSCProtocol.TCP && IsStarted)
			{
				Close();
				Connect();
				return;
			}

			_transmitterBackend.RefreshRemote(_remoteHost, _remotePort);
		}

		private string GetLocalHost()
		{
			if (_protocol != OSCProtocol.TCP && _localReceiver != null)
				return _localReceiver.LocalHost;

			if (_localHostMode == OSCLocalHostMode.Any)
				return "0.0.0.0";

			return _localHost;
		}

		private int GetLocalPort()
		{
			if (_protocol != OSCProtocol.TCP && _localReceiver != null)
				return _localReceiver.LocalPort;

			if (_localPortMode == OSCLocalPortMode.Random)
				return 0;

			if (_localPortMode == OSCLocalPortMode.FromReceiver)
				throw new Exception("[OSCTransmitter] Local Port Mode does not support \"FromReceiver\" option.");

			if (_localPortMode == OSCLocalPortMode.Custom)
				return _localPort;

			return _remotePort;
		}

		private void EnsureBackend()
		{
			if (__transmitterBackend == null || _backendProtocol != _protocol)
				RecreateBackend();
			else
			{
				__transmitterBackend.TcpFraming = _tcpFraming;
				__transmitterBackend.TcpReconnectTimeout = _tcpReconnectTimeout;
			}
		}

		private void DrainQueueResults()
		{
			if (__transmitterBackend == null)
				return;

			while (__transmitterBackend.TryTakeQueueResult(out var sent) && _queuedConsolePackets.Count > 0)
			{
				OSCConsole.ResolveQueued(_queuedConsolePackets.Dequeue(), sent);
			}
		}

		private void RecreateBackend()
		{
			if (__transmitterBackend != null)
			{
				if (__transmitterBackend.IsAvailable)
					__transmitterBackend.Close();

				DrainQueueResults();

				__transmitterBackend = null;
			}

			__transmitterBackend = OSCTransmitterBackend.Create(_protocol);
			__transmitterBackend.TcpFraming = _tcpFraming;
			__transmitterBackend.TcpReconnectTimeout = _tcpReconnectTimeout;
			_backendProtocol = _protocol;
		}

		#endregion
	}
}
/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

namespace extOSC.Core.Network
{
    public abstract class OSCTransmitterBackend
    {
        #region Static Public Methods

        public static OSCTransmitterBackend Create(OSCProtocol protocol)
        {
            if (protocol == OSCProtocol.TCP)
                return new OSCTransmitterTcpBackend();

#if UNITY_WSA && !UNITY_EDITOR
            return new OSCTransmitterWindowsStoreBackend();
#else
            return new OSCTransmitterStandaloneBackend();
#endif
        }

        #endregion

        #region Public Vars

        public virtual OSCTcpFraming TcpFraming { get; set; }

        public virtual float TcpReconnectTimeout { get; set; }

        public abstract bool IsAvailable { get; }

        // Connectionless backends are "connected" whenever they can send.
        public virtual bool IsConnected => IsAvailable;

        #endregion

        #region Public Methods

        public abstract void Connect(string localHost, int localPort);

        public abstract void RefreshRemote(string remoteHost, int remotePort);

        public abstract void Close();

        public abstract void Send(byte[] data, int length);

        public virtual void Tick()
        { }

        #endregion
    }
}
/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;
using System.Net;

namespace extOSC.Core
{
	// TODO: Remove IClonable, replace on custom Clone method.
	public interface IOSCPacket
	{
		string Address { get; }
		IPEndPoint From { get; set; }
		IOSCPacket Clone();
	}
}
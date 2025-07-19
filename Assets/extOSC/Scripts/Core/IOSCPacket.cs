/* Copyright (c) 2024 dr. ext (Vladimir Sigalkin) */

using System;
using System.Net;

namespace extOSC.Core
{
	public interface IOSCPacket : ICloneable
	{
		string Address { get; }
		IPEndPoint From { get; set; }
	}
}
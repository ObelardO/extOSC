using System.Runtime.InteropServices;

namespace extOSC
{
    // INFO: This class provides cross-platform compatibility.
    [StructLayout(LayoutKind.Explicit)]
    public struct OSCColor
    {
        [FieldOffset(0)] 
        private int _rgba;
        [FieldOffset(0)]
        public byte R;
        [FieldOffset(1)]
        public byte G;
        [FieldOffset(2)]
        public byte B;
        [FieldOffset(3)]
        public byte A;

        public OSCColor(byte r, byte g, byte b, byte a)
        {
            _rgba = 0;
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
        public override int GetHashCode() => _rgba.GetHashCode();
        public override bool Equals(object other) => other is OSCColor midi && Equals(midi);
        public bool Equals(OSCColor other) => _rgba == other._rgba;

        public override string ToString()
        {
            return $"RGBA({R}, {G}, {B}, {A})";
        }
		
        // TODO: Unity specific. Transfer to another file.
        public static implicit operator UnityEngine.Color(OSCColor c) => new UnityEngine.Color(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        public static implicit operator OSCColor(UnityEngine.Color v) => new OSCColor((byte)(v.r * 255), (byte)(v.g * 255), (byte)(v.b * 255), (byte)(v.a * 255));
        public static implicit operator UnityEngine.Color32(OSCColor c) => new UnityEngine.Color32(c.R, c.G, c.B, c.A);
        public static implicit operator OSCColor(UnityEngine.Color32 v) => new OSCColor(v.r, v.g, v.b, v.a);
    }
}
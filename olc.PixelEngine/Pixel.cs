using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace olc {
	[StructLayout( layoutKind: LayoutKind.Explicit, Size = 4 )]
	public class Pixel : IEquatable<Pixel> {
		public enum Mode {
			Normal,
			Mask,
			Alpha,
			Custom
		}

		[FieldOffset( 0 )] public byte A;
		[FieldOffset( 1 )] public byte B;
		[FieldOffset( 2 )] public byte G;
		[FieldOffset( 3 )] public byte R;

		public int Value {
			get { return (A << 24) | (B << 16) | (G << 8) | (R << 0); }
			set {
				var v = value;

				A = (byte)((v & (0xff << 24)) >> 24);
				B = (byte)((v & (0xff << 16)) >> 16);
				G = (byte)((v & (0xff <<  8)) >>  8);
				R = (byte)((v & (0xff <<  0)) >>  0);
			}
		}

		public Pixel() {
			A = 0;
			R = 0;
			G = 0;
			B = 0;
		}

		public Pixel(uint val) {
			A = (byte)((val & (0xff << 24)) >> 24);
			B = (byte)((val & (0xff << 16)) >> 16);
			G = (byte)((val & (0xff << 8)) >> 8);
			R = (byte)((val & (0xff << 0)) >> 0);
		}

		public Pixel( byte r, byte g, byte b, byte a = 0xff ) {
			A = a;
			B = b;
			G = g;
			R = r;
		}

		public bool Equals( Pixel other ) {
			return Value == other.Value;
		}

		#region Preset Static Values
		public static readonly Pixel White = new Pixel( 255, 255, 255 );
		public static readonly Pixel Grey = new Pixel( 192, 192, 192 );
		public static readonly Pixel Dark_Grey = new Pixel( 128, 128, 128 );
		public static readonly Pixel Very_Dark_Grey = new Pixel( 64, 64, 64 );
		public static readonly Pixel Red = new Pixel( 255, 0, 0 );
		public static readonly Pixel Dark_Red = new Pixel( 128, 0, 0 );
		public static readonly Pixel Very_Dark_Red = new Pixel( 64, 0, 0 );
		public static readonly Pixel Yellow = new Pixel( 255, 255, 0 );
		public static readonly Pixel Dark_Yellow = new Pixel( 128, 128, 0 );
		public static readonly Pixel Very_Dark_Yellow = new Pixel( 64, 64, 0 );
		public static readonly Pixel Green = new Pixel( 0, 255, 0 );
		public static readonly Pixel Dark_Green = new Pixel( 0, 128, 0 );
		public static readonly Pixel Very_Dark_Green = new Pixel( 0, 64, 0 );
		public static readonly Pixel Cyan = new Pixel( 0, 255, 255 );
		public static readonly Pixel Dark_Cyan = new Pixel( 0, 128, 128 );
		public static readonly Pixel Very_Dark_Cyan = new Pixel( 0, 64, 64 );
		public static readonly Pixel Blue = new Pixel( 0, 0, 255 );
		public static readonly Pixel Dark_Blue = new Pixel( 0, 0, 128 );
		public static readonly Pixel Very_Dark_Blue = new Pixel( 0, 0, 64 );
		public static readonly Pixel Magenta = new Pixel( 255, 0, 255 );
		public static readonly Pixel Dark_Magenta = new Pixel( 128, 0, 128 );
		public static readonly Pixel Very_Dark_Magenta = new Pixel( 64, 0, 64 );
		public static readonly Pixel Black = new Pixel( 0, 0, 0 );
		public static readonly Pixel Blank = new Pixel( 0, 0, 0, 0 );
		#endregion
	}
} 

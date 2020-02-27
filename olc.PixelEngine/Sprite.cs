using System;
using System.Drawing;
using System.IO;

namespace olc {
	public class Sprite {
		public enum SpriteMode {
			Normal,
			Periodic
		}

		public static ReturnCode LoadFromFile( string sImageFile, out Sprite ret ) {
			ret = new Sprite();
			if( !File.Exists( sImageFile ) )
				return ReturnCode.No_File;

			Bitmap retImg;
			try {
				retImg = Image.FromFile( sImageFile ) as Bitmap;
			} catch( Exception ) {
				retImg = null;
			}

			if( retImg == null )
				return ReturnCode.Fail;

			ret = new Sprite( retImg );

			return ReturnCode.OK;
		}

		public static ReturnCode LoadFromStream( Stream sImageFile, out Sprite ret ) {
			ret = new Sprite();
			if( sImageFile == null || sImageFile.Length == 0 )
				return ReturnCode.No_File;

			Bitmap retImg;
			try {
				retImg = Image.FromStream( sImageFile ) as Bitmap;
			} catch( Exception ) {
				retImg = null;
			}

			if( retImg == null )
				return ReturnCode.Fail;

			ret = new Sprite( retImg );

			return ReturnCode.OK;
		}

		public int Width { get; protected set; }
		public int Height { get; protected set; }
		//public Pixel[,] Pixels { get; set; }
		public Memory<Pixel> Pixels { get; protected set; }
		public SpriteMode Mode { get; protected set; }

		public Pixel this[int x, int y] {
			get { return Pixels.Span[(y * Width) + x]; }
			set { Pixels.Span[(y * Width) + x] = value; }
		}

		public Pixel this[Point p] {
			get { return this[p.X, p.Y]; }
			set { this[p.X, p.Y] = value; }
		}

		public Sprite() {
			Width = 0; Height = 0;
			Pixels = new Memory<Pixel>();
			Mode = SpriteMode.Normal;
		}

		public Sprite( int w, int h ) {
			Width = w;
			Height = h;
			Pixels = new Memory<Pixel>(new Pixel[w * h]);
			Mode = SpriteMode.Normal;
		}

		public Sprite( Bitmap img ) {
			Width = img.Width;
			Height = img.Height;
			Pixels = new Memory<Pixel>( new Pixel[Width * Height] );
			Mode = SpriteMode.Normal;

			for( var x = 0; x < img.Width; x++ ) {
				for( var y = 0; y < img.Width; y++ ) {
					var c = img.GetPixel( x, y );
					this[x, y] = Pixel.FromColor(c);
				}
			}
		}

		public void SetSampleMode( SpriteMode mode = SpriteMode.Normal ) {
			Mode = mode;
		}

		public Pixel GetPixel( int x, int y ) {
			return this[x, y];
		}

		public void SetPixel( int x, int y, Pixel p ) {
			this[x, y] = p;
		}

		public Pixel Sample( float x, float y ) {
			return this[(int)Math.Min( Width - 1, x * Width ), (int)Math.Min( Height - 1, y * Height )];
		}

		public Pixel SampleBL( float u, float v ) {
			u = u * Width - 0.5f;
			v = v * Height - 0.5f;
			int x = (int)Math.Floor( u ); // cast to int rounds toward zero, not downward
			int y = (int)Math.Floor( v ); // Thanks @joshinils
			float u_ratio = u - x;
			float v_ratio = v - y;
			float u_opposite = 1 - u_ratio;
			float v_opposite = 1 - v_ratio;

			var p1 = GetPixel( Math.Max( x, 0 ), Math.Max( y, 0 ) );
			var p2 = GetPixel( Math.Min( x + 1, Width - 1 ), Math.Max( y, 0 ) );
			var p3 = GetPixel( Math.Max( x, 0 ), Math.Min( y + 1, Height - 1 ) );
			var p4 = GetPixel( Math.Min( x + 1, Width - 1 ), Math.Min( y + 1, Height - 1 ) );

			return new Pixel(
				(byte)((p1.R * u_opposite + p2.R * u_ratio) * v_opposite + (p3.R * u_opposite + p4.R * u_ratio) * v_ratio),
				(byte)((p1.G * u_opposite + p2.G * u_ratio) * v_opposite + (p3.G * u_opposite + p4.G * u_ratio) * v_ratio),
				(byte)((p1.B * u_opposite + p2.B * u_ratio) * v_opposite + (p3.B * u_opposite + p4.B * u_ratio) * v_ratio) );
		}

		public Pixel[] GetData() { return Pixels.ToArray(); }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole {
	class Program : olc.PixelEngine.WinForms.BasePixelEngine {
		static void Main( string[] args ) {
			var demo = new Program(args);
			if( demo.Construct( 150, 100, 4, 4 ) == olc.ReturnCode.OK )
				_ = demo.Start();
		}

		private Random rand;
		private olc.Pixel randP;

		public Program( string[] args ) : base() {
		}

		public override bool OnUserCreate() {
			rand = new Random();
			randP = new olc.Pixel();

			return true;
		}
		public override bool OnUserUpdate( float fElapsedTime ) {
			Clear();

			for( int i = 0; i < Renderer.Width; i++ ) {
				for( int j = 0; j < Renderer.Height; j++ ) {
					
					randP.R = (byte)rand.Next( 0, 256 );
					randP.G = (byte)rand.Next( 0, 256 );
					randP.B = (byte)rand.Next( 0, 256 );
					Draw( i, j, randP ); // Draw a random pixel
				}
			}

			return true;
		}
		public override bool OnUserDestroy() {
			return true;
		}
	}
}

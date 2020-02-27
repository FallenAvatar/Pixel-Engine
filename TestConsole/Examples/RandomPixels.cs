using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.Examples {
	class RandomPixels : olc.PixelEngine.WinForms.BasePixelEngine {
		private Random rand;
		private olc.Pixel randP;

		public RandomPixels( string[] args ) : base( args ) {
			AppName = "RandomPixels";
		}

		public override bool OnUserCreate() {
			rand = new Random();
			randP = new olc.Pixel( 0, 0, 0 );

			return true;
		}
		public override bool OnUserUpdate( float fElapsedTime ) {
			//Clear(); // Really don't need this if we are writing every pixel every frame
			int r;

			for( int i = 0; i < Renderer.Width; i++ ) {
				for( int j = 0; j < Renderer.Height; j++ ) {
					r = rand.Next() | (0xff << 24);
					randP.Value = r;
					Draw( i, j, randP );
				}
			}

			return true;
		}
	}
}

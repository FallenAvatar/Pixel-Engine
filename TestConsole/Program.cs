using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole {
	class Program : olc.PixelEngine.WinForms.BasePixelEngine {
		static void Main( string[] args ) {
			var demo = new Program(args);
			if( demo.Construct( 400, 300, 1, 1 ) == olc.ReturnCode.OK )
				_ = demo.Start();
		}

		public Program( string[] args ) : base() { }

		public override bool OnUserCreate() {
			return true;
		}
		public override bool OnUserUpdate( float fElapsedTime ) {
			Clear();

			DrawLine( 10, 10, 390, 10, olc.Pixel.Green );

			return true;
		}
		public override bool OnUserDestroy() {
			return true;
		}
	}
}

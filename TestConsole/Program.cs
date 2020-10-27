using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole {
	class Program {
		static void Main( string[] args ) {
			//RunDemo_RandomPixels( args );
			RunDemo_Maze(args);
			//RunDemo_Simple3D(args);
		}

		private static void RunDemo_RandomPixels( string[] args ) {
			var demo = new Examples.RandomPixels( args );

			if( demo.Construct( 150, 100, 4, 4 ) == olc.ReturnCode.OK )
			//if( demo.Construct( 600, 400, 1, 1 ) == olc.ReturnCode.OK )
				_ = demo.Start();
		}

		private static void RunDemo_Maze( string[] args ) {
			var demo = new Examples.Maze( args );

			if( demo.Construct( 100, 100, 5, 5 ) == olc.ReturnCode.OK )
				_ = demo.Start();
		}

		private static void RunDemo_Simple3D( string[] args ) {
			var demo = new Examples.Simple3D.Game( args );
			if( demo.Construct( 600, 400, 1, 1 ) == olc.ReturnCode.OK )
				_ = demo.Start();
		}
	}
}

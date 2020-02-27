using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole {
	class Program {
		static void Main( string[] args ) {
			var demo = new Examples.RandomPixels(args);
			if( demo.Construct( 150, 100, 4, 4 ) == olc.ReturnCode.OK )
				_ = demo.Start();
		}
	}
}

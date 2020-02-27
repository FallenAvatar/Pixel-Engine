using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole.Examples.Simple3D {
	class Game : olc.PixelEngine.WinForms.BasePixelEngine {
		public Game( string[] args ) : base( args ) { }
		public override bool OnUserCreate() {
			// Load Textures and World

			return true;
		}
		public override bool OnUserUpdate( float fElapsedTime ) {
			// https://github.com/OneLoneCoder/olcPixelGameEngine/blob/master/Videos/OneLoneCoder_PGE_olcEngine3D.cpp

			return true;
		}
	}
}

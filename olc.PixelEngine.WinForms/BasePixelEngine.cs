using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace olc.PixelEngine.WinForms {
	public abstract class BasePixelEngine : olc.PixelGameEngine, IDisposable {
		protected BasePixelEngine(string[] args) : base( new Renderer() ) {
			// TODO: Parse args
		}

		public void Dispose() {
			_ = OnUserDestroy();
		}
	}
}

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace olc.PixelEngine.WinForms {
	public class Renderer : BaseRenderer {
		private frmEngine window;
		private Thread uiThread;
		public override bool Running { get; protected set; }

		public Renderer() : base() { }

		private string appName;
		public override string AppName {
			get {
				return appName;
			}
			set {
				appName = value;
				if( window != null ) {
					Invoke( () => {
						window.Text = appName;
					});
				}
			}
		}

		public override float FPS {
			set {
				if( window != null ) {
					Invoke( () => { 
						window.Text = string.Format( "{0} - {1:#####0.00}fps", appName, value );
					});
				}
			}
		}

		public override bool ConstructWindow( uint w, uint h ) {
			Running = true;

			var loading = new ManualResetEvent( false );
			uiThread = new Thread( () => {
				window = new frmEngine( this, loading );
				window.Size = new Size( (int)w, (int)h );

				if( !string.IsNullOrWhiteSpace( appName ) )
					window.Text = appName;

				Application.Run( window );
			} );
			uiThread.Start();

			if( !loading.WaitOne( 5000 ) )
				return false;

			return true;
		}

		public override void Draw(Point pos, Pixel p) {
			window.buffGraphics.Graphics.FillRectangle( new SolidBrush( Color.FromArgb( p.A, p.R, p.G, p.B ) ), pos.X, pos.Y, 1, 1 );
		}

		public override void Clear(Pixel p) {
			window.buffGraphics.Graphics.Clear( Color.FromArgb( p.A, p.R, p.G, p.B ) );
		}

		public override void SwapBuffers() {
			Invoke( () => { window.Refresh(); } );
		}

		public void Closing() {
			Running = false;
		}

		private void Invoke( Action a ) {
			try {
				if( window.InvokeRequired )
					_ = window.Invoke( a );
				else
					a();
			} catch( Exception e ) {
				Console.WriteLine( "[EXC] {0}", e.Message );
			}
		}
	}
}

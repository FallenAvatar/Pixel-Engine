using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace olc.PixelEngine.WinForms {
	public class Renderer : BaseRenderer {
		private frmEngine window;
		private Thread uiThread;
		public override bool Running { get; protected set; }

		protected Size pixSize;
		public override Size PixelSize {
			get { return pixSize;  }
			protected set { pixSize = value; if( window != null ) window.PixelSize = pixSize; }
		}

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
					} );
				}
			}
		}

		public override float FPS {
			set {
				if( window != null ) {
					Invoke( () => {
						window.Text = string.Format( "{0} - {1:#####0.00}fps", appName, value );
					} );
				}
			}
		}

		protected override bool PlatformConstructWindow() {
			var loading = new ManualResetEvent( false );
			uiThread = new Thread( () => {
				window = new frmEngine();
				window.PixelSize = PixelSize;
				window.Load += ( obj, e ) => {
					window.Size = new Size( Width * PixelSize.Width, Height * PixelSize.Height );
					_ = loading.Set();
				};
				window.FormClosing += ( obj, e ) => {
					Running = false;
				};

				if( !string.IsNullOrWhiteSpace( appName ) )
					window.Text = appName;

				Application.Run( window );
			} );
			uiThread.Start();

			if( !loading.WaitOne( 5000 ) )
				return false;

			return true;
		}

		public override void StartFrame() {
			if( window.NeedsNewGraphics )
				Invoke( () => { window.CreateGraphics(); } );
		}

		public override void UpdateScreen() {
			Invoke( () => {
				window.SetPixels( RenderTarget.Pixels );

				window.Refresh();
			} );
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

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
		private Bitmap imgBackBuffer;
		private System.Drawing.Imaging.BitmapData bd;

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

		public override bool ConstructWindow( int w, int h, int pixel_w, int pixel_h ) {
			Running = true;
			Width = w;
			Height = h;
			PixelSize = new Size( pixel_w, pixel_h );
			imgBackBuffer = new Bitmap( w * pixel_w, h * pixel_h, System.Drawing.Imaging.PixelFormat.Format32bppArgb );

			var loading = new ManualResetEvent( false );
			uiThread = new Thread( () => {
				window = new frmEngine();
				window.Load += ( obj, e ) => {
					_ = loading.Set();
				};
				window.FormClosing += ( obj, e ) => {
					Running = false;
				};
				window.ClientSize = new Size( w*pixel_w, h*pixel_h );

				if( !string.IsNullOrWhiteSpace( appName ) )
					window.Text = appName;

				Application.Run( window );
			} );
			uiThread.Start();

			if( !loading.WaitOne( 5000 ) )
				return false;

			return true;
		}

		public unsafe override void Draw(Point pos, Pixel p) {
			var s = bd.Stride / 4;
			//var pixSize = 4;
			int* pStart = (int*)bd.Scan0.ToPointer();
			var pixVal = p.ToArgb();

			var sx = pos.X * PixelSize.Width;
			var sy = pos.Y * PixelSize.Height;

			for( var x = sx; x < sx + PixelSize.Width; x++ )
				for( var y = sy; y < sy + PixelSize.Height; y++ )
					*(pStart + (y * s) + (x)) = pixVal;
		}

		public override void StartFrame() {
			bd = imgBackBuffer.LockBits( new Rectangle( 0, 0, Width * PixelSize.Width, Height * PixelSize.Height ), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
		}

		public override void UpdateScreen() {
			imgBackBuffer.UnlockBits( bd );
			try {
				window.BufferedGraphics.DrawImageUnscaled( imgBackBuffer, 0, 0 );
			} catch( Exception _ ) { }

			Invoke( () => {
				
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

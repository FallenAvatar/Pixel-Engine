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
				addNewFpsRecord( value );
				if( window != null ) {
					Invoke( () => {
						window.Text = string.Format( "{0} - {1:#####0.00}fps", appName, value );
					} );
				}
			}
		}

		private Queue<float> fpsRecords = new Queue<float>();
		private int fpsUpdateCounter = 0;
		private float fps_avg;
		private float fps_var;
		private float fps_stddev;
		private float fps_min = float.MaxValue;
		private float fps_max = float.MinValue;
		private void addNewFpsRecord( float newFps ) {
			return;
			fpsRecords.Enqueue( newFps );

			//fps_min = Math.Min( fps_min, newFps );
			//fps_max = Math.Max( fps_max, newFps );
			fps_min = fpsRecords.Min();
			fps_max = fpsRecords.Max();

			if( fpsRecords.Count < 20 )
				return;

			var oldFps = fpsRecords.Dequeue();
			var oldAvg = fps_avg;

			if( oldAvg == 0f ) {
				oldAvg = fpsRecords.Average();
			}

			fps_avg = oldAvg + (newFps - oldFps) / fpsRecords.Count;
			fps_var += (newFps - oldFps) * (newFps - fps_avg + oldFps - oldAvg) / (fpsRecords.Count - 1);
			fps_stddev = (float)Math.Sqrt( Math.Abs( fps_var ) );

			fpsUpdateCounter--;

			if( fpsUpdateCounter <= 0 ) {
				fpsUpdateCounter = 20;

				if( float.IsNaN( fps_stddev ) )
					System.Diagnostics.Debugger.Break();

				// TODO; Display current vals
				Console.WriteLine( "FPS: {0:0.00} min - {1:0.00} max - {2:0.00} avg - {3:0.00} std dev", fps_min, fps_max, fps_avg, fps_stddev );
			}
		}

		protected override bool PlatformConstructWindow() {
			var loading = new ManualResetEvent( false );
			uiThread = new Thread( () => {
				window = new frmEngine();
				window.Load += ( obj, e ) => {
					window.Size = new Size( RenderTarget.Width, RenderTarget.Height );
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

		protected override void DrawRaw( int x, int y, Pixel p ) {
			RenderTarget[x, y] = p;
		}

		public override void StartFrame() {
			if( window.NeedsNewGraphics )
				Invoke( () => { window.CreateGraphics(); } );
		}

		public override void UpdateScreen() {
			window.SetPixels( RenderTarget.Pixels );

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

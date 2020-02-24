using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace olc.PixelEngine.WinForms {
	public partial class frmEngine : Form {
		BufferedGraphicsContext buffContext;
		public BufferedGraphics buffGraphics;
		ManualResetEvent loading;
		Renderer renderer;

		public frmEngine(Renderer r, ManualResetEvent m) {
			renderer = r;
			loading = m;
			InitializeComponent();

			SetStyle( ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true );
			UpdateStyles();

			buffContext = BufferedGraphicsManager.Current;
			buffContext.MaximumBuffer = Size + new Size( 1, 1 );
			buffGraphics = buffContext.Allocate( CreateGraphics(), DisplayRectangle );
			buffGraphics.Graphics.Clear( Color.Black );
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing ) {
			if( disposing && (components != null) ) {
				components.Dispose();
				buffGraphics.Dispose();
				buffGraphics = null;
			}
			base.Dispose( disposing );
		}

		private void frmEngine_Paint( object sender, PaintEventArgs e ) {
			buffGraphics.Render( e.Graphics );
		}

		private void frmEngine_ResizeEnd( object sender, EventArgs e ) {
			if( buffGraphics != null ) {
				buffGraphics.Dispose();
				buffGraphics = null;
			}

			buffContext.MaximumBuffer = Size + new Size( 1, 1 );
			buffGraphics = buffContext.Allocate( CreateGraphics(), DisplayRectangle );
			buffGraphics.Graphics.Clear( Color.Black );

			Refresh();
		}

		private void frmEngine_KeyDown( object sender, KeyEventArgs e ) {
			var k = e.KeyCode;
		}

		private void frmEngine_KeyUp( object sender, KeyEventArgs e ) {
			var k = e.KeyCode;
		}

		private void frmEngine_FormClosing( object sender, FormClosingEventArgs e ) {
			renderer.Closing();
		}

		private void frmEngine_MouseDown( object sender, MouseEventArgs e ) {
			//var k = e.Button;
		}

		private void frmEngine_MouseUp( object sender, MouseEventArgs e ) {
			//var k = e.Button;
		}

		private void frmEngine_MouseMove( object sender, MouseEventArgs e ) {
			//var x = e.X;
			//var y = e.Y;
			//var ms = e.Delta;
		}

		private void frmEngine_Load( object sender, EventArgs e ) {
			loading.Set();
		}
	}
}

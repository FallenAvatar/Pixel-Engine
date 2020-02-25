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
		private BufferedGraphics buffGraphics;
		public Graphics BufferedGraphics {
			get {
				if( buffGraphics == null )
					return null;

				return buffGraphics.Graphics;
			}
		}

		public frmEngine() {
			InitializeComponent();

			SetStyle( ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true );
			UpdateStyles();

			CreateBufferedGraphics();
		}

		private void CreateBufferedGraphics() {
			if( buffGraphics != null ) {
				buffGraphics.Dispose();
				buffGraphics = null;
			}

			BufferedGraphicsManager.Current.MaximumBuffer = Size + new Size( 1, 1 );
			buffGraphics = BufferedGraphicsManager.Current.Allocate( CreateGraphics(), DisplayRectangle );

			buffGraphics.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
			buffGraphics.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
			buffGraphics.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
			buffGraphics.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			buffGraphics.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

			buffGraphics.Graphics.Clear( Color.Black );
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing ) {
			if( disposing && (components != null) ) {
				components.Dispose();
				if( buffGraphics != null ) {
					buffGraphics.Dispose();
					buffGraphics = null;
				}
			}

			base.Dispose( disposing );
		}

		private void frmEngine_Paint( object sender, PaintEventArgs e ) {
			try {
				buffGraphics.Render( e.Graphics );
			} catch( Exception _ ) { }
		}

		private void frmEngine_ResizeEnd( object sender, EventArgs e ) {
			CreateBufferedGraphics();
		}

		private void frmEngine_KeyDown( object sender, KeyEventArgs e ) {
			//var k = e.KeyCode;
		}

		private void frmEngine_KeyUp( object sender, KeyEventArgs e ) {
			//var k = e.KeyCode;
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
	}
}

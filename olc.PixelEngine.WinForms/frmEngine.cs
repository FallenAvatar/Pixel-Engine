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
		private bool needsSetModes;
		public bool NeedsNewGraphics { get; protected set; }
		public Size PixelSize { get; set; }

		private IntPtr myDc;
		private IntPtr mem_dc;
		private IntPtr mem_bmp;

		public frmEngine() {
			InitializeComponent();

			this.DoubleBuffered = false;

			SetStyle( ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true );
			UpdateStyles();

			NeedsNewGraphics = true;
		}

		public void CreateGraphics() {
			myDc = Windows.GetDC( Handle );
			mem_dc = Windows.CreateCompatibleDC( myDc );
			mem_bmp = Windows.CreateCompatibleBitmap( myDc, Size.Width / PixelSize.Width, Size.Height / PixelSize.Height );
			_ = Windows.SelectObject( mem_dc, mem_bmp );

			NeedsNewGraphics = false;
			needsSetModes = true;
		}

		public unsafe void SetPixels( Memory<Pixel> pixels ) {
			// Assumes 32bpp ARGB mode
			var memHandle = pixels.Pin();
			_ = Windows.SetBitmapBits( mem_bmp, (uint)(pixels.Length * 4), (byte*)memHandle.Pointer );
			memHandle.Dispose();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing ) {
			if( disposing && (components != null) ) {
				components.Dispose();
				// TODO: Clean up HDCs and what not
			}

			base.Dispose( disposing );
		}

		private void frmEngine_Paint( object sender, PaintEventArgs e ) {
			if( NeedsNewGraphics )
				return;

			if( needsSetModes ) {
				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
				e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
				e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.AssumeLinear;
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

				needsSetModes = false;
			}

			// TODO: Figure out why StretchBlt flickers like crazy
			if( PixelSize.Width == 1 && PixelSize.Height == 1 )
				_ = Windows.BitBlt( myDc, 0, 0, Size.Width, Size.Height, mem_dc, 0, 0, Windows.TernaryRasterOperations.SRCCOPY );
			else
				_ = Windows.StretchBlt( myDc, 0, 0, Size.Width, Size.Height, mem_dc, 0, 0, Size.Width / PixelSize.Width, Size.Height / PixelSize.Height, Windows.TernaryRasterOperations.SRCCOPY );
		}

		private void frmEngine_ResizeEnd( object sender, EventArgs e ) {
			NeedsNewGraphics = true;
		}

		private void frmEngine_KeyDown( object sender, KeyEventArgs e ) {
			//var k = e.KeyCode;
		}

		private void frmEngine_KeyUp( object sender, KeyEventArgs e ) {
			var k = e.KeyCode;
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

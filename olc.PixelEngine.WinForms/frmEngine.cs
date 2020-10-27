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
		public Size WindowSize { get; set; }
		private Bitmap test;

		private IntPtr myDc;
		private IntPtr mem_dc;
		private IntPtr mem_bmp;

		public frmEngine() {
			InitializeComponent();

			//this.DoubleBuffered = true;

			SetStyle( ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserMouse, true );
			UpdateStyles();

			NeedsNewGraphics = true;
			needsSetModes = true;
		}

		public void CreateNewGraphics() {
			myDc = Windows.GetDC( Handle );
			mem_dc = Windows.CreateCompatibleDC( myDc );
			mem_bmp = Windows.CreateCompatibleBitmap( myDc, ClientSize.Width / PixelSize.Width, ClientSize.Height / PixelSize.Height );
			_ = Windows.SelectObject( mem_dc, mem_bmp );

			NeedsNewGraphics = false;
			needsSetModes = true;

			WindowSize = ClientSize;

			
		}

		public unsafe void SetPixels( Memory<Pixel> pixels ) {
			// Assumes 32bpp ARGB mode
			var memHandle = pixels.Pin();
			_ = Windows.SetBitmapBits( mem_bmp, (uint)(pixels.Length * 4u), (byte*)memHandle.Pointer );
			//test = new Bitmap(ClientSize.Width / PixelSize.Width, ClientSize.Height / PixelSize.Height, ClientSize.Width / PixelSize.Width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, new IntPtr(memHandle.Pointer));
			memHandle.Dispose();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing ) {
			if( disposing && (components != null) ) {
				components.Dispose();
				_ = Windows.SelectObject(mem_dc, IntPtr.Zero);

				_ = Windows.DeleteObject(mem_bmp);
				_ = Windows.DeleteDC(mem_dc);
			}

			base.Dispose( disposing );
		}

		protected override void OnPaintBackground(PaintEventArgs e) {
			//base.OnPaintBackground(e);
		}

		protected override void OnPaint(PaintEventArgs e) {
			//base.OnPaint(e);
			frmEngine_Paint(this, e);
		}

		private void frmEngine_Paint( object sender, PaintEventArgs e ) {
			if( NeedsNewGraphics )
				CreateNewGraphics();

			//needsSetModes = true;
			if( needsSetModes ) {
				e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
				e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
				e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.AssumeLinear;
				e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;

				needsSetModes = false;
			}

			//e.Graphics.Clear(this.BackColor);
			//if( test != null )
			//	e.Graphics.DrawImage(test, ClientRectangle);
			// TODO: Figure out why StretchBlt flickers like crazy
			if( PixelSize.Width == 1 && PixelSize.Height == 1 )
				_ = Windows.BitBlt( myDc, 0, 0, ClientSize.Width, ClientSize.Height, mem_dc, 0, 0, Windows.TernaryRasterOperations.SRCCOPY );
			else
				_ = Windows.StretchBlt( myDc, 0, 0, ClientSize.Width, ClientSize.Height, mem_dc, 0, 0, ClientSize.Width / PixelSize.Width, ClientSize.Height / PixelSize.Height, Windows.TernaryRasterOperations.SRCCOPY );
		}

		private void frmEngine_ResizeEnd( object sender, EventArgs e ) {
			if( ClientSize != WindowSize ) {
				NeedsNewGraphics = true;
				WindowSize = ClientSize;
			}
		}

		private void frmEngine_KeyDown( object sender, KeyEventArgs e ) {
			var k = e.KeyCode;
		}

		private void frmEngine_KeyUp( object sender, KeyEventArgs e ) {
			var k = e.KeyCode;
		}

		private void frmEngine_MouseDown( object sender, MouseEventArgs e ) {
			var k = e.Button;
		}

		private void frmEngine_MouseUp( object sender, MouseEventArgs e ) {
			var k = e.Button;
		}

		private void frmEngine_MouseMove( object sender, MouseEventArgs e ) {
			var x = e.X;
			var y = e.Y;
			var z = e.Delta;
		}
	}
}

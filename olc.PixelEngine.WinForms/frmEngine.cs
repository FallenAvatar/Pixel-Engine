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
		public bool NeedsNewGraphics { get; protected set; }

		private IntPtr myDc;
		private IntPtr mem_dc;
		private IntPtr mem_bmp;

		public frmEngine() {
			InitializeComponent();

			SetStyle( ControlStyles.UserPaint, true );
			UpdateStyles();

			NeedsNewGraphics = true;
		}

		public void CreateGraphics() {
			myDc = Windows.GetDC( Handle );
			mem_dc = Windows.CreateCompatibleDC( myDc );
			mem_bmp = Windows.CreateCompatibleBitmap( myDc, Size.Width, Size.Height );
			_ = Windows.SelectObject( mem_dc, mem_bmp );

			NeedsNewGraphics = false;
		}

		public unsafe void SetPixels(Memory<Pixel> pixels) {
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

			_ = Windows.BitBlt( myDc, 0, 0, Size.Width, Size.Height, mem_dc, 0, 0, Windows.TernaryRasterOperations.SRCCOPY );
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

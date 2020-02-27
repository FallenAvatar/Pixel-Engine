using System;
using System.Collections.Generic;
using System.Drawing;

namespace olc {
	public enum ReturnCode {
		Fail = 0,
		OK = 1,
		No_File = -1
	}

	public abstract class PixelGameEngine {
		public bool IsFocused { get; set; }
		public string AppName {
			get { return Renderer.AppName; }
			set { Renderer.AppName = value; }
		}
		protected IRenderer Renderer;

		public virtual Pixel DefaultBGPixel { get { return Pixel.Black; } }
		public virtual Pixel DefaultFGPixel { get { return Pixel.White; } }

		public PixelMode PixelMode {
			get { return Renderer.PixelMode; }
			set { Renderer.PixelMode = value; }
		}

		public PixelGameEngine( IRenderer r ) {
			Renderer = r;
		}

		public ReturnCode Construct( int screen_w, int screen_h, int pixel_w = 1, int pixel_h = 1/*, bool fullscreen = false, bool vsync = false */) {
			if( !Renderer.ConstructWindow( screen_w, screen_h, pixel_w, pixel_h ) )
				return ReturnCode.Fail;

			if( !OnUserCreate() )
				return ReturnCode.Fail;

			return ReturnCode.OK;
		}

		public ReturnCode Start() {
			var sw = new System.Diagnostics.Stopwatch();
			var running = true;
			var lastFrameTime = DateTime.Now;
			DateTime currFrameTime;
			float frameDT;

			while( running ) {
				sw.Restart();

				currFrameTime = DateTime.Now;
				frameDT = (float)(currFrameTime - lastFrameTime).TotalSeconds;
				Renderer.StartFrame();

				running = OnUserUpdate( frameDT ) && Renderer.Running;

				Renderer.UpdateScreen();

				Renderer.FPS = (float)(1000f / sw.Elapsed.TotalMilliseconds);
				lastFrameTime = currFrameTime;
			}

			_ = OnUserDestroy();

			return ReturnCode.OK;
		}

		public abstract bool OnUserCreate();
		public abstract bool OnUserUpdate( float fElapsedTime );
		public virtual bool OnUserDestroy() { return true; }



		// TODO: Implement Input
		// Get the state of a specific keyboard button
		public HWButton GetKey( Key k ) {
			return HWButton.None;
		}
		// Get the state of a specific mouse button
		public HWButton GetMouse( uint b ) {
			return HWButton.None;
		}
		// Get Mouse X coordinate in "pixel" space
		public int GetMouseX() => 0;
		// Get Mouse Y coordinate in "pixel" space
		public int GetMouseY() => 0;
		// Get Mouse Wheel Delta
		public int GetMouseWheel() => 0;

		// Use a custom blend function
		//public void SetPixelMode( Func<int, int, Pixel, Pixel, Pixel> pixelMode ) { }
		// Change the blend factor form between 0.0f to 1.0f;
		//public void SetPixelBlend( float fBlend ) { }
		// Offset texels by sub-pixel amount (advanced, do not use)
		//public void SetSubPixelOffset( float ox, float oy ) { }

		// Draws a single Pixel
		public void Draw( int x, int y, Pixel? p = null ) {
			p = p ?? DefaultFGPixel;
			Renderer.Draw( x,y, p.Value );
		}
		public void Draw( Point pos, Pixel? p = null ) {
			Draw( pos.X,pos.Y, p );
		}
		// Draws a line from (x1,y1) to (x2,y2)
		public void DrawLine( int x1, int y1, int x2, int y2, Pixel? p = null, uint pattern = 0xFFFFFFFF ) {
			p = p ?? DefaultFGPixel;
			Renderer.DrawLine( x1, y1, x2, y2, p.Value, pattern );
		}
		public void DrawLine( Point pos1, Point pos2, Pixel? p = null, uint pattern = 0xFFFFFFFF ) {
			DrawLine( pos1.X, pos1.Y, pos2.X, pos2.Y, p, pattern );
		}
		// Draws a circle located at (x,y) with radius
		public void DrawCircle( int x, int y, int radius, Pixel? p = null, byte mask = 0xFF ) {
			p = p ?? DefaultFGPixel;
			Renderer.DrawCircle( x, y, radius, p.Value, mask );
		}
		public void DrawCircle( Point pos, int radius, Pixel? p = null, byte mask = 0xFF ) {
			DrawCircle( pos.X, pos.Y, radius, p, mask );
		}
		// Fills a circle located at (x,y) with radius
		public void FillCircle( int x, int y, int radius, Pixel? p = null ) {
			p = p ?? DefaultFGPixel;
			Renderer.FillCircle( x, y, radius, p.Value );
		}
		public void FillCircle( Point pos, int radius, Pixel? p = null ) {
			FillCircle( pos.X, pos.Y, radius, p );
		}
		// Draws a rectangle at (x,y) to (x+w,y+h)
		public void DrawRect( int x, int y, int w, int h, Pixel? p = null ) {
			p = p ?? DefaultFGPixel;
			Renderer.DrawRect( x, y, w, h, p.Value );
		}
		public void DrawRect( Point pos, Size size, Pixel? p = null ) {
			DrawRect( pos.X, pos.Y, size.Width, size.Height, p );
		}
		public void DrawRect( Rectangle rect, Pixel? p = null ) {
			DrawRect( rect.X, rect.Y, rect.Width, rect.Height, p );
		}
		// Fills a rectangle at (x,y) to (x+w,y+h)
		public void FillRect( int x, int y, int w, int h, Pixel? p = null ) {
			p = p ?? DefaultFGPixel;
			Renderer.FillRect( x, y, w, h, p.Value );
		}
		public void FillRect( Point pos, Size size, Pixel? p = null ) {
			FillRect( pos.X, pos.Y, size.Width, size.Height, p );
		}
		public void FillRect( Rectangle rect, Pixel? p = null ) {
			FillRect( rect.X, rect.Y, rect.Width, rect.Height, p );
		}
		// Draws a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		public void DrawTriangle( int x1, int y1, int x2, int y2, int x3, int y3, Pixel? p = null ) {
			p = p ?? DefaultFGPixel;
			Renderer.DrawTriangle( x1, y1, x2, y2, x3, y3, p.Value );
		}
		public void DrawTriangle( Point pos1, Point pos2, Point pos3, Pixel? p = null ) {
			DrawTriangle( pos1.X, pos1.Y, pos2.X, pos2.Y, pos3.X, pos3.Y, p );
		}
		// Flat fills a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		public void FillTriangle( int x1, int y1, int x2, int y2, int x3, int y3, Pixel? p = null ) {
			p = p ?? DefaultFGPixel;
			Renderer.FillTriangle( x1, y1, x2, y2, x3, y3, p.Value );
		}
		public void FillTriangle( Point pos1, Point pos2, Point pos3, Pixel? p = null ) {
			FillTriangle( pos1.X, pos1.Y, pos2.X, pos2.Y, pos3.X, pos3.Y, p );
		}
		// Draws an entire sprite at location (x,y)
		public void DrawSprite( int x, int y, Sprite sprite, int scale = 1 ) {
			Renderer.DrawSprite( x, y, sprite, scale );
		}
		public void DrawSprite( Point pos, Sprite sprite, int scale = 1 ) {
			DrawSprite( pos.X, pos.Y, sprite, scale );
		}
		// Draws an area of a sprite at location (x,y), where the
		// selected area is (ox,oy) to (ox+w,oy+h)
		public void DrawPartialSprite( int x, int y, Sprite sprite, int ox, int oy, int w, int h, int scale = 1 ) {
			Renderer.DrawPartialSprite( x, y, sprite, ox, oy, w, h, scale );
		}
		public void DrawPartialSprite( Point pos, Sprite sprite, Point sourcepos, Size size, int scale = 1 ) {
			DrawPartialSprite( pos.X, pos.Y, sprite, sourcepos.X, sourcepos.Y, size.Width, size.Height, scale );
		}
		// Draws a single line of text
		public void DrawString( int x, int y, string sText, Pixel? p = null, int scale = 1 ) {
			p = p ?? DefaultFGPixel;
			Renderer.DrawString(x, y, sText, p.Value, scale );
		}
		public void DrawString( Point pos, string sText, Pixel? p = null, int scale = 1 ) {
			DrawString( pos.X, pos.Y, sText, p, scale );
		}
		// Clears entire draw target to Pixel
		public void Clear( Pixel? p = null ) {
			p = p ?? DefaultBGPixel;
			Renderer.Clear( p.Value );
		}
		// Resize the primary screen sprite
		public void SetScreenSize( int w, int h ) { }
	}
}

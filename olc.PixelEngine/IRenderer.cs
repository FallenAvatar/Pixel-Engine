using System;
using System.Collections.Generic;
using System.Drawing;

namespace olc {
	public interface IRenderer {
		int Width { get; }
		int Height { get; }
		string AppName { get; set; }
		float FPS { set; }
		bool Running { get; }
		PixelMode PixelMode { get; set; }
		Size PixelSize { get; }
		Sprite RenderTarget { get; set; }

		bool ConstructWindow( int screen_w, int screen_h, int pixel_w, int pixel_h );

		void Clear( Pixel p );

		void Draw( int x, int y, Pixel p );

		// Draws a line from (x1,y1) to (x2,y2)
		void DrawLine( int x1, int y1, int x2, int y2, Pixel p, uint pattern = 0xFFFFFFFF );

		// Draws a circle located at (x,y) with radius
		void DrawCircle( int x, int y, int radius, Pixel p, byte mask = 0xFF );

		// Fills a circle located at (x,y) with radius
		void FillCircle( int x, int y, int radius, Pixel p );

		// Draws a rectangle at (x,y) to (x+w,y+h)
		void DrawRect( int x, int y, int w, int h, Pixel p );

		// Fills a rectangle at (x,y) to (x+w,y+h)
		void FillRect( int x, int y, int w, int h, Pixel p );

		// Draws a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		void DrawTriangle( int x1, int y1, int x2, int y2, int x3, int y3, Pixel p );

		// Flat fills a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		void FillTriangle( int x1, int y1, int x2, int y2, int x3, int y3, Pixel p );

		// Draws an entire sprite at location (x,y)
		void DrawSprite( int x, int y, Sprite sprite, int scale = 1 );

		// Draws an area of a sprite at location (x,y), where the
		// selected area is (ox,oy) to (ox+w,oy+h)
		void DrawPartialSprite( int xDest, int yDest, Sprite sprite, int xSrc, int ySrc, int wSrc, int hSrc, int scale = 1 );

		// Draws a single line of text
		void DrawString( int x, int y, string sText, Pixel p, int scale = 1 );

		void StartFrame();
		void UpdateScreen();
	}
}

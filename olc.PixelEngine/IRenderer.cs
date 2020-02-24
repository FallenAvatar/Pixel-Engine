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
		Pixel.Mode PixelMode { get; set; }

		bool ConstructWindow( uint w, uint h );

		void Clear( Pixel p );

		void Draw( Point pos, Pixel p );

		// Draws a line from (x1,y1) to (x2,y2)
		void DrawLine( Point pos1, Point pos2, Pixel p, uint pattern = 0xFFFFFFFF );

		// Draws a circle located at (x,y) with radius
		void DrawCircle( Point pos, int radius, Pixel p, byte mask = 0xFF );

		// Fills a circle located at (x,y) with radius
		void FillCircle( Point pos, int radius, Pixel p );

		// Draws a rectangle at (x,y) to (x+w,y+h)
		void DrawRect( Point pos, Size size, Pixel p );

		// Fills a rectangle at (x,y) to (x+w,y+h)
		void FillRect( Point pos, Size size, Pixel p );

		// Draws a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		void DrawTriangle( Point pos1, Point pos2, Point pos3, Pixel p );

		// Flat fills a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		void FillTriangle( Point pos1, Point pos2, Point pos3, Pixel p );

		// Draws an entire sprite at location (x,y)
		void DrawSprite( Point pos, Sprite sprite, int scale = 1 );

		// Draws an area of a sprite at location (x,y), where the
		// selected area is (ox,oy) to (ox+w,oy+h)
		void DrawPartialSprite( Point pos, Sprite sprite, Point sourcepos, Size size, int scale = 1 );

		// Draws a single line of text
		void DrawString( Point pos, string sText, Pixel col = null, int scale = 1 );

		void SwapBuffers();
	}
}

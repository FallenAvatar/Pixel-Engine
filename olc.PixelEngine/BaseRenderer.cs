using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace olc {
	public abstract class BaseRenderer : IRenderer {
		protected enum ScreenBufferStatus : byte {
			Invalid = 0,
			Ready = 1,
			Painting,
			Painted,
			Rendering,
			Rendered
		}
		public int Width { get { return RenderTarget.Width; } }
		public int Height { get { return RenderTarget.Height; } }

		public abstract string AppName { get; set; }
		public abstract float FPS { set; }
		public abstract bool Running { get; protected set; }
		public PixelMode PixelMode { get; set; }
		public virtual Size PixelSize { get; protected set; }

		//protected Queue<Sprite> spareBuffers;
		protected Sprite nextFrameBuffer;
		protected Sprite frameBuffer;
		public Sprite RenderTarget { get { return frameBuffer; } }
		public Sprite FrameBuffer { get { return frameBuffer; } }

		protected BaseRenderer() { }

		public virtual bool ConstructWindow( int screen_w, int screen_h, int pixel_w, int pixel_h ) {
			PixelSize = new Size( pixel_w, pixel_h );

			//spareBuffers = new Queue<Sprite>();
			//for( var i=0; i<3; i++ ) {
			//	spareBuffers.Enqueue(new Sprite(screen_w, screen_h));
			//}

			//frameBuffer = spareBuffers.Dequeue();
			//nextFrameBuffer = spareBuffers.Dequeue();

			frameBuffer = new Sprite(screen_w, screen_h);
			nextFrameBuffer = new Sprite(screen_w, screen_h);

			return Running = PlatformConstructWindow();
		}

		public void StartFrame() {
			PlatformStartFrame();
		}
		public void UpdateScreen() {

			//nextFrameBuffer = Interlocked.Exchange(ref frameBuffer, nextFrameBuffer);
			

			PlatformUpdateScreen();

			//spareBuffers.Enqueue(frameBuffer);
		}

		protected abstract bool PlatformConstructWindow();
		public abstract void PlatformStartFrame();
		public abstract void PlatformUpdateScreen();

		public virtual void Draw( int x, int y, Pixel p ) {
			RenderTarget[x, y] = p;
		}

		// Draws a line from (x1,y1) to (x2,y2)
		public virtual void DrawLine( int x1, int y1, int x2, int y2, Pixel p, uint pattern = 0xFFFFFFFF ) {
			int x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
			dx = x2 - x1; dy = y2 - y1;

			Func<uint> rol = (() => {
				pattern = (pattern << 1) | (pattern >> 31);
				return pattern & 1;
			});

			// straight lines idea by gurkanctn
			if( dx == 0 ) // Line is vertical
			{
				if( y2 < y1 )
					Utils.Swap( ref y1, ref y2 );
				
				for( y = y1; y <= y2; y++ )
					if( rol() > 0 )
						Draw( x1, y, p );

				return;
			} else if( dy == 0 ) // Line is horizontal
			{
				if( x2 < x1 )
					Utils.Swap( ref x1, ref x2 );
				
				for( x = x1; x <= x2; x++ )
					if( rol() > 0 )
						Draw( x, y1, p );

				return;
			}

			// Line is Funk-aye
			dx1 = Math.Abs( dx ); dy1 = Math.Abs( dy );
			px = 2 * dy1 - dx1; py = 2 * dx1 - dy1;
			if( dy1 <= dx1 ) {
				if( dx >= 0 ) {
					x = x1; y = y1; xe = x2;
				} else {
					x = x2; y = y2; xe = x1;
				}

				if( rol() > 0 ) Draw( x, y, p );

				for( i = 0; x < xe; i++ ) {
					x = x + 1;
					if( px < 0 )
						px = px + 2 * dy1;
					else {
						if( (dx < 0 && dy < 0) || (dx > 0 && dy > 0) ) y = y + 1; else y = y - 1;
						px = px + 2 * (dy1 - dx1);
					}
					if( rol() > 0 ) Draw( x, y, p );
				}
			} else {
				if( dy >= 0 ) {
					x = x1; y = y1; ye = y2;
				} else {
					x = x2; y = y2; ye = y1;
				}

				if( rol() > 0 )
					Draw( x, y, p );

				for( i = 0; y < ye; i++ ) {
					y = y + 1;
					if( py <= 0 )
						py += 2 * dx1;
					else {
						if( (dx < 0 && dy < 0) || (dx > 0 && dy > 0) )
							x++;
						else
							x--;
						py += 2 * (dx1 - dy1);
					}

					if( rol() > 0 )
						Draw( x, y, p );
				}
			}
		}

		// Draws a circle located at (x,y) with radius
		public virtual void DrawCircle( int x, int y, int radius, Pixel p, byte mask = 0xFF ) {
			int x0 = 0;
			int y0 = radius;
			int d = 3 - 2 * radius;

			if( radius <= 0 )
				return;

			while( y0 >= x0 ) // only formulate 1/8 of circle
			{
				if( 0 < (mask & 0x01) ) Draw( x + x0, y - y0, p );
				if( 0 < (mask & 0x02) ) Draw( x + y0, y - x0, p );
				if( 0 < (mask & 0x04) ) Draw( x + y0, y + x0, p );
				if( 0 < (mask & 0x08) ) Draw( x + x0, y + y0, p );
				if( 0 < (mask & 0x10) ) Draw( x - x0, y + y0, p );
				if( 0 < (mask & 0x20) ) Draw( x - y0, y + x0, p );
				if( 0 < (mask & 0x40) ) Draw( x - y0, y - x0, p );
				if( 0 < (mask & 0x80) ) Draw( x - x0, y - y0, p );

				if( d < 0 )
					d += 4 * x0++ + 6;
				else
					d += 4 * (x0++ - y0--) + 10;
			}
		}

		// Fills a circle located at (x,y) with radius
		public virtual void FillCircle( int x, int y, int radius, Pixel p ) {
			// Taken from wikipedia
			int x0 = 0;
			int y0 = radius;
			int d = 3 - 2 * radius;
			if( radius <= 0 ) return;

			Action<int, int, int> drawline = ( int sx, int ex, int ny ) => {
				for( int i = sx; i <= ex; i++ )
					Draw( i, ny, p );
			};

			while( y0 >= x0 ) {
				// Modified to draw scan-lines instead of edges
				drawline( x - x0, x + x0, y - y0 );
				drawline( x - y0, x + y0, y - x0 );
				drawline( x - x0, x + x0, y + y0 );
				drawline( x - y0, x + y0, y + x0 );

				if( d < 0 )
					d += 4 * x0++ + 6;
				else
					d += 4 * (x0++ - y0--) + 10;
			}
		}

		// Draws a rectangle at (x,y) to (x+w,y+h)
		public virtual void DrawRect( int x, int y, int w, int h, Pixel p ) {
			DrawLine( x, y, x + w, y, p );
			DrawLine( x + w, y, x + w, y + h, p );
			DrawLine( x + w, y + h, x, y + h, p );
			DrawLine( x, y + h, x, y, p );
		}

		// Fills a rectangle at (x,y) to (x+w,y+h)
		public virtual void FillRect( int x, int y, int w, int h, Pixel p ) {
			var x2 = Utils.Clamp(0, x + w, Width);
			var y2 = Utils.Clamp( 0, y + h, Height);

			x = Utils.Clamp( 0, x, Width );
			y = Utils.Clamp( 0, y, Height );

			for( int i = x; i < x2; i++ )
				for( int j = y; j < y2; j++ )
					Draw( i, j, p );
		}

		// Draws a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		public virtual void DrawTriangle( int x1, int y1, int x2, int y2, int x3, int y3, Pixel p ) {
			DrawLine( x1, y1, x2, y2, p );
			DrawLine( x2, y2, x3, y3, p );
			DrawLine( x3, y3, x1, y1, p );
		}

		// Flat fills a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		public virtual void FillTriangle( int x1, int y1, int x2, int y2, int x3, int y3, Pixel p ) {
			Action<int, int, int> drawline = ( sx, ex, ny ) => { for( int i = sx; i <= ex; i++ ) Draw( i, ny, p ); };

			int t1x, t2x, y, minx, maxx, t1xp, t2xp;
			bool changed1 = false;
			bool changed2 = false;
			int signx1, signx2, dx1, dy1, dx2, dy2;
			int e1, e2;
			// Sort vertices
			if( y1 > y2 ) {
				Utils.Swap( ref x1, ref x2 );
				Utils.Swap( ref y1, ref y2 );
			}
			if( y1 > y3 ) {
				Utils.Swap( ref x1, ref x3 );
				Utils.Swap( ref y1, ref y3 );
			}
			if( y2 > y3 ) {
				Utils.Swap( ref x2, ref x3 );
				Utils.Swap( ref y2, ref y3 );
			}

			t1x = t2x = x1; y = y1;   // Starting points
			dx1 = (x2 - x1);
			if( dx1 < 0 ) { dx1 = -dx1; signx1 = -1; } else signx1 = 1;
			dy1 = (y2 - y1);

			dx2 = (x3 - x1);
			if( dx2 < 0 ) { dx2 = -dx2; signx2 = -1; } else signx2 = 1;
			dy2 = (y3 - y1);

			if( dy1 > dx1 ) {   // swap values
				Utils.Swap( ref dx1, ref dy1 );
				changed1 = true;
			}
			if( dy2 > dx2 ) {   // swap values
				Utils.Swap( ref dx2, ref dy2 );
				changed2 = true;
			}

			e2 = dx2 >> 1;
			// Flat top, just process the second half
			if( y1 == y2 ) goto next;
			e1 = dx1 >> 1;

			for( int i = 0; i < dx1; ) {
				t1xp = 0; t2xp = 0;
				if( t1x < t2x ) { minx = t1x; maxx = t2x; } else { minx = t2x; maxx = t1x; }
				// process first line until y value is about to change
				while( i < dx1 ) {
					i++;
					e1 += dy1;
					while( e1 >= dx1 ) {
						e1 -= dx1;
						if( changed1 ) t1xp = signx1;//t1x += signx1;
						else goto next1;
					}
					if( changed1 ) break;
					else t1x += signx1;
				}
			// Move line
			next1:
				// process second line until y value is about to change
				while( true ) {
					e2 += dy2;
					while( e2 >= dx2 ) {
						e2 -= dx2;
						if( changed2 ) t2xp = signx2;//t2x += signx2;
						else goto next2;
					}
					if( changed2 ) break;
					else t2x += signx2;
				}
			next2:
				if( minx > t1x ) minx = t1x;
				if( minx > t2x ) minx = t2x;
				if( maxx < t1x ) maxx = t1x;
				if( maxx < t2x ) maxx = t2x;
				drawline( minx, maxx, y );    // Draw line from min to max points found on the y
											  // Now increase y
				if( !changed1 ) t1x += signx1;
				t1x += t1xp;
				if( !changed2 ) t2x += signx2;
				t2x += t2xp;
				y += 1;
				if( y == y2 ) break;

			}
		next:
			// Second half
			dx1 = x3 - x2;
			if( dx1 < 0 ) { dx1 = -dx1; signx1 = -1; } else signx1 = 1;
			dy1 = y3 - y2;
			t1x = x2;

			if( dy1 > dx1 ) {   // swap values
				var t = dy1; dy1 = dx1; dx1 = t;
				changed1 = true;
			} else changed1 = false;

			e1 = dx1 >> 1;

			for( int i = 0; i <= dx1; i++ ) {
				t1xp = 0; t2xp = 0;
				if( t1x < t2x ) { minx = t1x; maxx = t2x; } else { minx = t2x; maxx = t1x; }
				// process first line until y value is about to change
				while( i < dx1 ) {
					e1 += dy1;
					while( e1 >= dx1 ) {
						e1 -= dx1;
						if( changed1 ) { t1xp = signx1; break; }//t1x += signx1;
						else goto next3;
					}
					if( changed1 ) break;
					else t1x += signx1;
					if( i < dx1 ) i++;
				}
			next3:
				// process second line until y value is about to change
				while( t2x != x3 ) {
					e2 += dy2;
					while( e2 >= dx2 ) {
						e2 -= dx2;
						if( changed2 ) t2xp = signx2;
						else goto next4;
					}
					if( changed2 ) break;
					else t2x += signx2;
				}
			next4:

				if( minx > t1x ) minx = t1x;
				if( minx > t2x ) minx = t2x;
				if( maxx < t1x ) maxx = t1x;
				if( maxx < t2x ) maxx = t2x;
				drawline( minx, maxx, y );
				if( !changed1 ) t1x += signx1;
				t1x += t1xp;
				if( !changed2 ) t2x += signx2;
				t2x += t2xp;
				y += 1;
				if( y > y3 ) return;
			}
		}

		// Draws an entire sprite at location (x,y)
		public virtual void DrawSprite( int x, int y, Sprite sprite, int scale = 1 ) {
			if( sprite == null )
				return;

			if( scale > 1 ) {
				for( var i = 0; i < sprite.Width; i++ )
					for( var j = 0; j < sprite.Height; j++ )
						for( var k = 0; k < scale; k++ )
							for( var l = 0; l < scale; l++ )
								Draw( x + (i * scale) + k, y + (j * scale) + l, sprite[i, j] );
			} else {
				for( var i = 0; i < sprite.Width; i++ )
					for( var j = 0; j < sprite.Height; j++ )
						Draw( x + i, y + j, sprite[i, j] );
			}
		}

		// Draws an area of a sprite at location (x,y), where the
		// selected area is (ox,oy) to (ox+w,oy+h)
		public virtual void DrawPartialSprite( int xDest, int yDest, Sprite sprite, int xSrc, int ySrc, int wSrc, int hSrc, int scale = 1 ) {
			if( sprite == null )
				return;

			if( scale > 1 ) {
				for( var i = 0; i < wSrc; i++ )
					for( var j = 0; j < hSrc; j++ )
						for( var k = 0; k < scale; k++ )
							for( var l = 0; l < scale; l++ )
								Draw( xDest + (i * scale) + k, yDest + (j * scale) + l, sprite[i + xSrc, j + ySrc] );
			} else {
				for( var i = 0; i < wSrc; i++ )
					for( var j = 0; j < hSrc; j++ )
						Draw( xDest + i, yDest + j, sprite[i + xSrc, j + ySrc] );
			}
		}

		// Draws a single line of text
		public virtual void DrawString( int x, int y, string sText, Pixel p, int scale = 1 ) {
			// TODO: Load fontSprite from font file
			var fontSprite = new Sprite();
			var sx = 0;
			var sy = 0;
			var savedPM = PixelMode;

			if( p.A != 255 )
				PixelMode = PixelMode.Alpha;
			else
				PixelMode = PixelMode.Mask;

			foreach( var c in sText ) {
				if( c == '\n' ) {
					sx = 0; sy += 8 * scale;
				} else {
					var ox = (c - 32) % 16;
					var oy = (c - 32) / 16;

					if( scale > 1 ) {
						for( var i = 0; i < 8; i++ )
							for( var j = 0; j < 8; j++ )
								if( fontSprite[i + ox * 8, j + oy * 8].R > 0 )
									for( var k = 0; k < scale; k++ )
										for( var l = 0; l < scale; l++ )
											Draw( x + sx + (i * scale) + k, y + sy + (j * scale) + l, p );
					} else {
						for( var i = 0; i < 8; i++ )
							for( var j = 0; j < 8; j++ )
								if( fontSprite[i + ox * 8, j + oy * 8].R > 0 )
									Draw( x + sx + i, y + sy + j, p );
					}
					sx += 8 * scale;
				}
			}

			PixelMode = savedPM;
		}

		public virtual void Clear( Pixel p ) {
			for( var x = 0; x < Width; x++ )
				for( var y = 0; y < Height; y++ )
					Draw( x, y, p );
		}
	}
}

using System;
using System.Collections.Generic;
using System.Drawing;

namespace olc {
	public abstract class BaseRenderer : IRenderer {
		public int Width { get; protected set; }
		public int Height { get; protected set; }

		public abstract string AppName { get; set; }
		public abstract float FPS { set; }
		public abstract bool Running { get; protected set; }
		public PixelMode PixelMode { get; set; }
		public Size PixelSize { get; protected set; }

		public Sprite RenderTarget { get; set; }

		protected BaseRenderer() { }

		public virtual bool ConstructWindow( int screen_w, int screen_h, int pixel_w, int pixel_h ) {
			Width = screen_w;
			Height = screen_h;
			PixelSize = new Size( pixel_w, pixel_h );
			RenderTarget = new Sprite( screen_w * pixel_w, screen_h * pixel_h );

			var ret = PlatformConstructWindow();

			if( ret ) {
				Running = true;
			}

			return ret;
		}

		protected abstract bool PlatformConstructWindow();
		public abstract void StartFrame();
		public abstract void UpdateScreen();

		protected abstract void DrawRaw( int x, int y, Pixel p );
		public virtual void Draw( int x, int y, Pixel p ) {
			var pw = PixelSize.Width;
			var ph = PixelSize.Height;
			var sx = x * pw;
			var sy = y * ph;

			for( var xi = 0; xi < pw; xi++ )
				for( var yi = 0; yi < ph; yi++ )
					DrawRaw(sx + xi, sy + yi, p);
		}

		// Draws a line from (x1,y1) to (x2,y2)
		public void DrawLine( Point pos1, Point pos2, Pixel p, uint pattern = 0xFFFFFFFF ) {
			int x1 = pos1.X, y1 = pos1.Y, x2 = pos2.X, y2 = pos2.Y;
			int x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;
			dx = x2 - x1; dy = y2 - y1;

			Func<uint> rol = (() => {
				pattern = (pattern << 1) | (pattern >> 31);
				return pattern & 1;
			});

			// straight lines idea by gurkanctn
			if( dx == 0 ) // Line is vertical
			{
				if( y2 < y1 ) {
					var t = y1; y1 = y2; y2 = t;
				}
				for( y = y1; y <= y2; y++ )
					if( rol() > 0 ) Draw( x1, y , p );
				return;
			}

			if( dy == 0 ) // Line is horizontal
			{
				if( x2 < x1 ) {
					var t = x1; x1 = x2; x2 = t;
				}
				for( x = x1; x <= x2; x++ )
					if( rol() > 0 ) Draw( x, y1 , p );
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

				if( rol() > 0 ) Draw( x, y, p );

				for( i = 0; y < ye; i++ ) {
					y = y + 1;
					if( py <= 0 )
						py = py + 2 * dx1;
					else {
						if( (dx < 0 && dy < 0) || (dx > 0 && dy > 0) ) x = x + 1; else x = x - 1;
						py = py + 2 * (dx1 - dy1);
					}
					if( rol() > 0 ) Draw( x, y, p );
				}
			}
		}

		// Draws a circle located at (x,y) with radius
		public void DrawCircle( Point pos, int radius, Pixel p, byte mask = 0xFF ) {
			int x = pos.X, y = pos.Y;
			int x0 = 0;
			int y0 = radius;
			int d = 3 - 2 * radius;
			if( radius <= 0 ) return;

			while( y0 >= x0 ) // only formulate 1/8 of circle
			{
				if( 0 < (mask & 0x01) ) Draw( x + x0, y - y0 , p );
				if( 0 < (mask & 0x02) ) Draw( x + y0, y - x0 , p );
				if( 0 < (mask & 0x04) ) Draw( x + y0, y + x0 , p );
				if( 0 < (mask & 0x08) ) Draw( x + x0, y + y0 , p );
				if( 0 < (mask & 0x10) ) Draw( x - x0, y + y0 , p );
				if( 0 < (mask & 0x20) ) Draw( x - y0, y + x0 , p );
				if( 0 < (mask & 0x40) ) Draw( x - y0, y - x0 , p );
				if( 0 < (mask & 0x80) ) Draw( x - x0, y - y0 , p );
				if( d < 0 ) d += 4 * x0++ + 6;
				else d += 4 * (x0++ - y0--) + 10;
			}
		}

		// Fills a circle located at (x,y) with radius
		public void FillCircle( Point pos, int radius, Pixel p ) {
			int x = pos.X, y = pos.Y;
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
				if( d < 0 ) d += 4 * x0++ + 6;
				else d += 4 * (x0++ - y0--) + 10;
			}
		}

		// Draws a rectangle at (x,y) to (x+w,y+h)
		public void DrawRect( Rectangle rect, Pixel p ) {
			var tl = rect.Location;
			var tr = rect.Location + new Size( rect.Width, 0 );
			var br = rect.Location + rect.Size;
			var bl = rect.Location + new Size( 0, rect.Height );
			
			DrawLine( tl, tr, p );
			DrawLine( tr, br, p );
			DrawLine( br, bl, p );
			DrawLine( bl, tl, p );
		}

		// Fills a rectangle at (x,y) to (x+w,y+h)
		public void FillRect( Rectangle rect, Pixel p ) {
			int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
			var x2 = x + w;
			var y2 = y + h;

			if( x < 0 ) x = 0;
			if( x >= Width ) x = Width;
			if( y < 0 ) y = 0;
			if( y >= Height ) y = Height;

			if( x2 < 0 ) x2 = 0;
			if( x2 >= Width ) x2 = Width;
			if( y2 < 0 ) y2 = 0;
			if( y2 >= Height ) y2 = Height;

			for( int i = x; i < x2; i++ )
				for( int j = y; j < y2; j++ )
					Draw( i, j, p );
		}

		// Draws a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		public void DrawTriangle( Point pos1, Point pos2, Point pos3, Pixel p ) {
			DrawLine( pos1, pos2, p );
			DrawLine( pos2, pos3, p );
			DrawLine( pos3, pos1, p );
		}

		// Flat fills a triangle between points (x1,y1), (x2,y2) and (x3,y3)
		public void FillTriangle( Point pos1, Point pos2, Point pos3, Pixel p ) {
			Action<int, int, int> drawline = ( sx, ex, ny ) => { for( int i = sx; i <= ex; i++ ) Draw( i, ny, p ); };
			int x1 = pos1.X, y1 = pos1.Y, x2 = pos2.X, y2 = pos2.Y, x3 = pos3.X, y3 = pos3.Y;

			int t1x, t2x, y, minx, maxx, t1xp, t2xp;
			bool changed1 = false;
			bool changed2 = false;
			int signx1, signx2, dx1, dy1, dx2, dy2;
			int e1, e2;
			// Sort vertices
			if( y1 > y2 ) {
				var t = y1; y1 = y2; y2 = t;
				t = x1; x1 = x2; x2 = t;
			}
			if( y1 > y3 ) {
				var t = y1; y1 = y3; y3 = t;
				t = x1; x1 = x3; x3 = t;
			}
			if( y2 > y3 ) {
				var t = y2; y2 = y3; y3 = t;
				t = x2; x2 = x3; x3 = t;
			}

			t1x = t2x = x1; y = y1;   // Starting points
			dx1 = (x2 - x1);
			if( dx1 < 0 ) { dx1 = -dx1; signx1 = -1; }
			else signx1 = 1;
			dy1 = (y2 - y1);

			dx2 = (x3 - x1);
			if( dx2 < 0 ) { dx2 = -dx2; signx2 = -1; }
			else signx2 = 1;
			dy2 = (y3 - y1);

			if( dy1 > dx1 ) {   // swap values
				var t = dx1; dx1 = dy1; dy1 = t;
				changed1 = true;
			}
			if( dy2 > dx2 ) {   // swap values
				var t = dy1; dy1 = dx1; dx1 = t;
				changed2 = true;
			}

			e2 = dx2 >> 1;
			// Flat top, just process the second half
			if( y1 == y2 ) goto next;
			e1 = dx1 >> 1;

			for( int i = 0; i < dx1; ) {
				t1xp = 0; t2xp = 0;
				if( t1x < t2x ) { minx = t1x; maxx = t2x; }
				else { minx = t2x; maxx = t1x; }
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
			if( dx1 < 0 ) { dx1 = -dx1; signx1 = -1; }
			else signx1 = 1;
			dy1 = y3 - y2;
			t1x = x2;

			if( dy1 > dx1 ) {   // swap values
				var t = dy1; dy1 = dx1; dx1 = t;
				changed1 = true;
			} else changed1 = false;

			e1 = dx1 >> 1;

			for( int i = 0; i <= dx1; i++ ) {
				t1xp = 0; t2xp = 0;
				if( t1x < t2x ) { minx = t1x; maxx = t2x; }
				else { minx = t2x; maxx = t1x; }
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
		public void DrawSprite( Point pos, Sprite sprite, int scale = 1 ) {
			if( sprite == null )
				return;

			if( scale > 1 ) {
				for( var i = 0; i < sprite.Width; i++ )
					for( var j = 0; j < sprite.Height; j++ )
						for( var k = 0; k < scale; k++ )
							for( var l = 0; l < scale; l++ )
								Draw( pos.X + (i * scale) + k, pos.Y + (j * scale) + l, sprite[i, j] );
			} else {
				for( var i = 0; i < sprite.Width; i++ )
					for( var j = 0; j < sprite.Height; j++ )
						Draw( pos.X + i, pos.Y + j, sprite[i, j] );
			}
		}

		// Draws an area of a sprite at location (x,y), where the
		// selected area is (ox,oy) to (ox+w,oy+h)
		public void DrawPartialSprite( Point pos, Sprite sprite, Point src, Size size, int scale = 1 ) {
			if( sprite == null )
				return;

			if( scale > 1 ) {
				for( var i = 0; i < size.Width; i++ )
					for( var j = 0; j < size.Height; j++ )
						for( var k = 0; k < scale; k++ )
							for( var l = 0; l < scale; l++ )
								Draw( pos.X + (i * scale) + k, pos.Y + (j * scale) + l , sprite[i + src.X, j + src.Y] );
			} else {
				for( var i = 0; i < size.Width; i++ )
					for( var j = 0; j < size.Height; j++ )
						Draw( pos.X + i, pos.Y + j , sprite[i + src.X, j + src.Y] );
			}
		}

		// Draws a single line of text
		public void DrawString( Point pos, string sText, Pixel col, int scale = 1 ) {
			throw new NotImplementedException();
			int x = pos.X, y = pos.Y;
			var fontSprite = new Sprite();
			var sx = 0;
			var sy = 0;
			var savedPM = PixelMode;
			if( col.A != 255 ) PixelMode = PixelMode.Alpha;
			else PixelMode = PixelMode.Mask;

			foreach( var c in sText ) {
				if( c == '\n' ) {
					sx = 0; sy += (int)(8 * scale);
				} else {
					var ox = (c - 32) % 16;
					var oy = (c - 32) / 16;

					if( scale > 1 ) {
						for( var i = 0; i < 8; i++ )
							for( var j = 0; j < 8; j++ )
								if( fontSprite[i + ox * 8, j + oy * 8].R > 0 )
									for( var k = 0; k < scale; k++ )
										for( var l = 0; l < scale; l++ )
											Draw( x + sx + (i * scale) + k, y + sy + (j * scale) + l, col );
					} else {
						for( var i = 0; i < 8; i++ )
							for( var j = 0; j < 8; j++ )
								if( fontSprite[i + ox * 8, j + oy * 8].R > 0 )
									Draw( x + sx + i, y + sy + j, col );
					}
					sx += (int)(8 * scale);
				}
			}

			PixelMode = savedPM;
		}

		public virtual void Clear( Pixel p ) {
			for( var x = 0; x < Width; x++ )
				for( var y = 0; y < Height; y++ )
					Draw(x, y, p );
		}
	}
}

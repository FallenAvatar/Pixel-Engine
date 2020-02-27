using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using olc;

namespace TestConsole.Examples {
	public class Maze : olc.PixelEngine.WinForms.BasePixelEngine {
		[Flags]
		private enum Cells {
			PathNorth = 1 << 0,
			PathEast = 1 << 1,
			PathSouth = 1 << 2,
			PathWest = 1 << 3,
			Visited = 1 << 4
		}

		private const int MazeWidth = 20;
		private const int MazeHeight = 20;
		private const int PathWidth = 4;

		private Cells[,] maze;

		private int visited;

		private Stack<Point> pointStack = new Stack<Point>();
		private Random rand;

		public Maze( string[] args ) : base( args ) {
			AppName = "Maze";
		}

		public override bool OnUserCreate() {
			Reset();

			return true;
		}

		private void Reset() {
			rand = new Random();
			maze = new Cells[MazeWidth, MazeHeight];

			int x = rand.Next( MazeWidth );
			int y = rand.Next( MazeHeight );

			pointStack.Push( new Point( x, y ) );

			maze[x,y] = Cells.Visited;

			visited = 1;
		}

		public override bool OnUserUpdate( float fElapsedTime ) {
			if( visited < MazeWidth * MazeHeight ) {
				var neighbours = new List<Cells>();

				var current = pointStack.Peek();

				// North neighbour
				if( current.Y > 0 && (maze[current.X, current.Y-1] & Cells.Visited) == 0 ) {
					neighbours.Add( Cells.PathNorth );
				}

				// East neighbour
				if( current.X < MazeWidth - 1 && (maze[current.X+1, current.Y] & Cells.Visited) == 0 ) {
					neighbours.Add( Cells.PathEast );
				}

				// South neighbour
				if( current.Y < MazeHeight - 1 && (maze[current.X, current.Y+1] & Cells.Visited) == 0 ) {
					neighbours.Add( Cells.PathSouth );
				}

				// West neighbour
				if( current.X > 0 && (maze[current.X-1, current.Y] & Cells.Visited) == 0 ) {
					neighbours.Add( Cells.PathWest );
				}

				if( neighbours.Count > 0 ) {

					var nextDir = neighbours[rand.Next( neighbours.Count )];

					switch( nextDir ) {
					case Cells.PathNorth:
						maze[current.X, current.Y-1] |= Cells.Visited | Cells.PathSouth;
						maze[current.X, current.Y] |= Cells.PathNorth;
						pointStack.Push( new Point( current.X, current.Y - 1 ) );
						break;
					case Cells.PathEast:
						maze[current.X+1, current.Y] |= Cells.Visited | Cells.PathWest;
						maze[current.X, current.Y] |= Cells.PathEast;
						pointStack.Push( new Point( current.X + 1, current.Y ) );
						break;

					case Cells.PathSouth:
						maze[current.X, current.Y+1] |= Cells.Visited | Cells.PathNorth;
						maze[current.X, current.Y] |= Cells.PathSouth;
						pointStack.Push( new Point( current.X, current.Y + 1 ) );
						break;

					case Cells.PathWest:
						maze[current.X-1, current.Y] |= Cells.Visited | Cells.PathEast;
						maze[current.X, current.Y] |= Cells.PathWest;
						pointStack.Push( new Point( current.X - 1, current.Y ) );
						break;
					}

					visited++;
				} else {
					_ = pointStack.Pop();
				}
			}


			Clear( Pixel.Black );

			for( int x = 0; x < MazeWidth; x++ ) {
				for( int y = 0; y < MazeHeight; y++ ) {
					for( int py = 0; py < PathWidth; py++ ) {
						for( int px = 0; px < PathWidth; px++ ) {
							var p = (maze[x, y] & Cells.Visited) != 0 ? Pixel.White : Pixel.Blue;
							Draw( x * (PathWidth + 1) + px, y * (PathWidth + 1) + py, p );
						}
					}

					for( int p = 0; p < PathWidth; p++ ) {
						if( (maze[x,y] & Cells.PathSouth) != 0 ) {
							Draw( x * (PathWidth + 1) + p, y * (PathWidth + 1) + PathWidth, Pixel.White );
						}
						if( (maze[x,y] & Cells.PathEast) != 0 ) {
							Draw( x * (PathWidth + 1) + PathWidth, y * (PathWidth + 1) + p, Pixel.White );
						}
					}
				}
			}

			for( int py = 0; py < PathWidth; py++ ) {
				for( int px = 0; px < PathWidth; px++ ) {
					Draw( pointStack.Peek().X * (PathWidth + 1) + px, pointStack.Peek().Y * (PathWidth + 1) + py, Pixel.Green );
				}
			}

			return true;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;

namespace olc {
	public class Utils {
		public static void Swap<T>(ref T one, ref T two) {
			T t = one;
			one = two;
			two = t;
		}

		public static void Clamp<T>( T min, ref T val, T max) where T : IComparable<T> {
			if( min.CompareTo(val) > 0 )
				val = min;
			else if( max.CompareTo(val) < 0 )
				val = max;
		}

		public static T Clamp<T>( T min, T val, T max ) where T : IComparable<T> {
			if( min.CompareTo( val ) > 0 )
				return min;
			else if( max.CompareTo( val ) < 0 )
				return max;

			return val;
		}
	}
}

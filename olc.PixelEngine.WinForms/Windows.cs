using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace olc.PixelEngine.WinForms {
	static class Windows {
		[DllImport( "user32.dll", SetLastError = true )]
		internal static extern IntPtr GetDC( [In] IntPtr hWnd );

		[DllImport( "gdi32.dll" )]
		internal static extern uint SetPixel( IntPtr hdc, int X, int Y, uint crColor );

		[DllImport( "gdi32.dll" )]
		internal static extern uint GetPixel( IntPtr hdc, int nXPos, int nYPos );

		[DllImport( "gdi32.dll" )]
		internal static unsafe extern int SetBitmapBits( IntPtr hbmp, uint cBytes, byte* lpBits );

		/// <summary>
		///        Creates a memory device context (DC) compatible with the specified device.
		/// </summary>
		/// <param name="hdc">A handle to an existing DC. If this handle is NULL,
		///        the function creates a memory DC compatible with the application's current screen.</param>
		/// <returns>
		///        If the function succeeds, the return value is the handle to a memory DC.
		///        If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.
		/// </returns>
		[DllImport( "gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true )]
		internal static extern IntPtr CreateCompatibleDC( [In] IntPtr hdc );

		/// <summary>
		///        Creates a bitmap compatible with the device that is associated with the specified device context.
		/// </summary>
		/// <param name="hdc">A handle to a device context.</param>
		/// <param name="nWidth">The bitmap width, in pixels.</param>
		/// <param name="nHeight">The bitmap height, in pixels.</param>
		/// <returns>If the function succeeds, the return value is a handle to the compatible bitmap (DDB). If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.</returns>
		[DllImport( "gdi32.dll", EntryPoint = "CreateCompatibleBitmap" )]
		internal static extern IntPtr CreateCompatibleBitmap( [In] IntPtr hdc, int nWidth, int nHeight );

		/// <summary>Deletes the specified device context (DC).</summary>
		/// <param name="hdc">A handle to the device context.</param>
		/// <returns><para>If the function succeeds, the return value is nonzero.</para><para>If the function fails, the return value is zero.</para></returns>
		/// <remarks>An application must not delete a DC whose handle was obtained by calling the <c>GetDC</c> function. Instead, it must call the <c>ReleaseDC</c> function to free the DC.</remarks>
		[DllImport( "gdi32.dll", EntryPoint = "DeleteDC" )]
		internal static extern bool DeleteDC( [In] IntPtr hdc );

		/// <summary>Deletes a logical pen, brush, font, bitmap, region, or palette, freeing all system resources associated with the object. After the object is deleted, the specified handle is no longer valid.</summary>
		/// <param name="hObject">A handle to a logical pen, brush, font, bitmap, region, or palette.</param>
		/// <returns>
		///   <para>If the function succeeds, the return value is nonzero.</para>
		///   <para>If the specified handle is not valid or is currently selected into a DC, the return value is zero.</para>
		/// </returns>
		/// <remarks>
		///   <para>Do not delete a drawing object (pen or brush) while it is still selected into a DC.</para>
		///   <para>When a pattern brush is deleted, the bitmap associated with the brush is not deleted. The bitmap must be deleted independently.</para>
		/// </remarks>
		[DllImport( "gdi32.dll", EntryPoint = "DeleteObject" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		internal static extern bool DeleteObject( [In] IntPtr hObject );

		/// <summary>Selects an object into the specified device context (DC). The new object replaces the previous object of the same type.</summary>
		/// <param name="hdc">A handle to the DC.</param>
		/// <param name="hgdiobj">A handle to the object to be selected.</param>
		/// <returns>
		///   <para>If the selected object is not a region and the function succeeds, the return value is a handle to the object being replaced. If the selected object is a region and the function succeeds, the return value is one of the following values.</para>
		///   <para>SIMPLEREGION - Region consists of a single rectangle.</para>
		///   <para>COMPLEXREGION - Region consists of more than one rectangle.</para>
		///   <para>NULLREGION - Region is empty.</para>
		///   <para>If an error occurs and the selected object is not a region, the return value is <c>NULL</c>. Otherwise, it is <c>HGDI_ERROR</c>.</para>
		/// </returns>
		/// <remarks>
		///   <para>This function returns the previously selected object of the specified type. An application should always replace a new object with the original, default object after it has finished drawing with the new object.</para>
		///   <para>An application cannot select a single bitmap into more than one DC at a time.</para>
		///   <para>ICM: If the object being selected is a brush or a pen, color management is performed.</para>
		/// </remarks>
		[DllImport( "gdi32.dll", EntryPoint = "SelectObject" )]
		internal static extern IntPtr SelectObject( [In] IntPtr hdc, [In] IntPtr hgdiobj );

		/// <summary>
		///    Performs a bit-block transfer of the color data corresponding to a
		///    rectangle of pixels from the specified source device context into
		///    a destination device context.
		/// </summary>
		/// <param name="hdc">Handle to the destination device context.</param>
		/// <param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
		/// <param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
		/// <param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
		/// <param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
		/// <param name="hdcSrc">Handle to the source device context.</param>
		/// <param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
		/// <param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
		/// <param name="dwRop">A raster-operation code.</param>
		/// <returns>
		///    <c>true</c> if the operation succeedes, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
		/// </returns>
		[DllImport( "gdi32.dll", EntryPoint = "BitBlt", SetLastError = true )]
		[return: MarshalAs( UnmanagedType.Bool )]
		internal static extern bool BitBlt( [In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop );

		/// <summary>
		///     Specifies a raster-operation code. These codes define how the color data for the
		///     source rectangle is to be combined with the color data for the destination
		///     rectangle to achieve the final color.
		/// </summary>
		internal enum TernaryRasterOperations : uint {
			/// <summary>dest = source</summary>
			SRCCOPY = 0x00CC0020,
			/// <summary>dest = source OR dest</summary>
			SRCPAINT = 0x00EE0086,
			/// <summary>dest = source AND dest</summary>
			SRCAND = 0x008800C6,
			/// <summary>dest = source XOR dest</summary>
			SRCINVERT = 0x00660046,
			/// <summary>dest = source AND (NOT dest)</summary>
			SRCERASE = 0x00440328,
			/// <summary>dest = (NOT source)</summary>
			NOTSRCCOPY = 0x00330008,
			/// <summary>dest = (NOT src) AND (NOT dest)</summary>
			NOTSRCERASE = 0x001100A6,
			/// <summary>dest = (source AND pattern)</summary>
			MERGECOPY = 0x00C000CA,
			/// <summary>dest = (NOT source) OR dest</summary>
			MERGEPAINT = 0x00BB0226,
			/// <summary>dest = pattern</summary>
			PATCOPY = 0x00F00021,
			/// <summary>dest = DPSnoo</summary>
			PATPAINT = 0x00FB0A09,
			/// <summary>dest = pattern XOR dest</summary>
			PATINVERT = 0x005A0049,
			/// <summary>dest = (NOT dest)</summary>
			DSTINVERT = 0x00550009,
			/// <summary>dest = BLACK</summary>
			BLACKNESS = 0x00000042,
			/// <summary>dest = WHITE</summary>
			WHITENESS = 0x00FF0062,
			/// <summary>
			/// Capture window as seen on screen.  This includes layered windows
			/// such as WPF windows with AllowsTransparency="true"
			/// </summary>
			CAPTUREBLT = 0x40000000
		}
	}
}

/*! MoonPdfLib - Provides a WPF user control to display PDF files
Copyright (C) 2013  (see AUTHORS file)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
!*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MoonPdfLib.Helper
{
	public static class BitmapExtensionMethods
	{
		public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap source)
		{
			BitmapSource bitSrc = null;

			var hBitmap = source.GetHbitmap();

			try
			{
				bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
					hBitmap,
					IntPtr.Zero,
					Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
			}
			catch (Win32Exception)
			{
				bitSrc = null;
			}
			finally
			{
				NativeMethods.DeleteObject(hBitmap);
			}

			return bitSrc;
		}

		private static class NativeMethods
		{
			[DllImport("gdi32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool DeleteObject(IntPtr hObject);
		}
	}
}

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
/*
 * 2013 - Modified and extended version of W. Jordan's code (see AUTHORS file)
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MoonPdfLib.MuPdf
{
	internal static class MuPdfWrapper
	{
		/// <summary>
		/// Extracts a PDF page as a Bitmap for a given pdf filename and a page number.
		/// </summary>
		/// <param name="pdfFilename">Must be the full path for an existing PDF file</param>
		/// <param name="pageNumber">Page number, starting at 1</param>
		/// <param name="zoomFactor">Used to get a smaller or bigger Bitmap, depending on the specified value</param>
		public static Bitmap ExtractPage(string pdfFilename, int pageNumber, float zoomFactor = 1.0f)
		{
			var pageNumberIndex = Math.Max(0, pageNumber - 1); // pages start at index 0

			using (var stream = new PdfFileStream(pdfFilename))
			{
				IntPtr p = NativeMethods.LoadPage(stream.Document, pageNumberIndex); // loads the page
				var bmp = RenderPage(stream.Context, stream.Document, p, zoomFactor);
				NativeMethods.FreePage(stream.Document, p); // releases the resources consumed by the page

				return bmp;
			}
		}

		/// <summary>
		/// Gets the page bounds for all pages of the given PDF. If a relevant rotation is supplied, the bounds will
		/// be rotated accordingly before returning.
		/// </summary>
		/// <param name="pdfFilename">Must be the full path for an existing PDF file</param>
		/// <param name="rotation">The rotation that should be applied</param>
		/// <returns></returns>
		public static System.Windows.Size[] GetPageBounds(string pdfFilename, ImageRotation rotation = ImageRotation.None)
		{
			var result = new List<System.Windows.Size>();
			Func<double, double, System.Windows.Size> sizeCallback = (width, height) => new System.Windows.Size(width, height);
			
			if( rotation == ImageRotation.Rotate90 || rotation == ImageRotation.Rotate270 )
				sizeCallback = (width, height) => new System.Windows.Size(height, width); // switch width and height

			using (var stream = new PdfFileStream(pdfFilename))
			{
				var pageCount = NativeMethods.CountPages(stream.Document); // gets the number of pages in the document

				for (int i = 0; i < pageCount; i++)
				{
					IntPtr p = NativeMethods.LoadPage(stream.Document, i); // loads the page
					Rectangle pageBound = NativeMethods.BoundPage(stream.Document, p);

					result.Add(sizeCallback(pageBound.Width, pageBound.Height));

					NativeMethods.FreePage(stream.Document, p); // releases the resources consumed by the page
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Return the total number of pages for a give PDF filename.
		/// </summary>
		public static int CountPages(string pdfFilename)
		{
			using (var stream = new PdfFileStream(pdfFilename))
			{
				return NativeMethods.CountPages(stream.Document); // gets the number of pages in the document
			}
		}

		static Bitmap RenderPage(IntPtr context, IntPtr document, IntPtr page, float zoomFactor)
		{
			Rectangle pageBound = NativeMethods.BoundPage(document, page);
			Matrix ctm = new Matrix();
			IntPtr pix = IntPtr.Zero;
			IntPtr dev = IntPtr.Zero;

			// gets the size of the page and multiplies it with zoom factors
			int width = (int)(pageBound.Width * zoomFactor);
			int height = (int)(pageBound.Height * zoomFactor);

			// sets the matrix as a scaling matrix (zoomX,0,0,zoomY,0,0)
			ctm.A = zoomFactor;
			ctm.D = zoomFactor;

			// creates a pixmap the same size as the width and height of the page
			pix = NativeMethods.NewPixmap(context, NativeMethods.FindDeviceColorSpace(context, "DeviceRGB"), width, height);
			// sets white color as the background color of the pixmap
			NativeMethods.ClearPixmap(context, pix, 0xFF);

			// creates a drawing device
			dev = NativeMethods.NewDrawDevice(context, pix);
			// draws the page on the device created from the pixmap
			NativeMethods.RunPage(document, page, dev, ctm, IntPtr.Zero);

			NativeMethods.FreeDevice(dev); // frees the resources consumed by the device
			dev = IntPtr.Zero;

			// creates a colorful bitmap of the same size of the pixmap
			Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
			var imageData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, bmp.PixelFormat);
			unsafe
			{ // converts the pixmap data to Bitmap data
				byte* ptrSrc = (byte*)NativeMethods.GetSamples(context, pix); // gets the rendered data from the pixmap
				byte* ptrDest = (byte*)imageData.Scan0;
				for (int y = 0; y < height; y++)
				{
					byte* pl = ptrDest;
					byte* sl = ptrSrc;
					for (int x = 0; x < width; x++)
					{
						//Swap these here instead of in MuPDF because most pdf images will be rgb or cmyk.
						//Since we are going through the pixels one by one anyway swap here to save a conversion from rgb to bgr.
						pl[2] = sl[0]; //b-r
						pl[1] = sl[1]; //g-g
						pl[0] = sl[2]; //r-b
						//sl[3] is the alpha channel, we will skip it here
						pl += 3;
						sl += 4;
					}
					ptrDest += imageData.Stride;
					ptrSrc += width * 4;
				}
			}

			NativeMethods.DropPixmap(context, pix);

			return bmp;
		}

		/// <summary>
		/// Helper class for an easier disposing of unmanaged resources
		/// </summary>
		private sealed class PdfFileStream : IDisposable
		{
			const uint FZ_STORE_DEFAULT = 256 << 20;

			public IntPtr Context { get; private set; }
			public IntPtr Stream { get; private set; }
			public IntPtr Document { get; private set; }

			public PdfFileStream(string pdfFilename)
			{
				Context = NativeMethods.NewContext(IntPtr.Zero, IntPtr.Zero, FZ_STORE_DEFAULT); // Creates the context
				Stream = NativeMethods.OpenFile(Context, pdfFilename); // opens file as a stream
				Document = NativeMethods.OpenDocumentStream(Context, ".pdf", Stream); // opens the document
			}

			public void Dispose()
			{
				NativeMethods.CloseDocument(Document); // releases the resources
				NativeMethods.CloseStream(Stream);
				NativeMethods.FreeContext(Context);
			}
		}

		private static class NativeMethods
		{
			const string DLL = "libmupdf.dll";

			[DllImport(DLL, EntryPoint = "fz_new_context", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr NewContext(IntPtr alloc, IntPtr locks, uint max_store);

			[DllImport(DLL, EntryPoint = "fz_free_context", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr FreeContext(IntPtr ctx);

			[DllImport(DLL, EntryPoint = "fz_open_file_w", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr OpenFile(IntPtr ctx, string fileName);

			[DllImport(DLL, EntryPoint = "fz_open_document_with_stream", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr OpenDocumentStream(IntPtr ctx, string magic, IntPtr stm);

			[DllImport(DLL, EntryPoint = "fz_close", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr CloseStream(IntPtr stm);

			[DllImport(DLL, EntryPoint = "fz_close_document", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr CloseDocument(IntPtr doc);

			[DllImport(DLL, EntryPoint = "fz_count_pages", CallingConvention = CallingConvention.Cdecl)]
			public static extern int CountPages(IntPtr doc);

			[DllImport(DLL, EntryPoint = "fz_bound_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern Rectangle BoundPage(IntPtr doc, IntPtr page);

			[DllImport(DLL, EntryPoint = "fz_clear_pixmap_with_value", CallingConvention = CallingConvention.Cdecl)]
			public static extern void ClearPixmap(IntPtr ctx, IntPtr pix, int byteValue);

			[DllImport(DLL, EntryPoint = "fz_find_device_colorspace", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr FindDeviceColorSpace(IntPtr ctx, string colorspace);

			[DllImport(DLL, EntryPoint = "fz_free_device", CallingConvention = CallingConvention.Cdecl)]
			public static extern void FreeDevice(IntPtr dev);

			[DllImport(DLL, EntryPoint = "fz_free_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern void FreePage(IntPtr doc, IntPtr page);

			[DllImport(DLL, EntryPoint = "fz_load_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr LoadPage(IntPtr doc, int pageNumber);

			[DllImport(DLL, EntryPoint = "fz_new_draw_device", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr NewDrawDevice(IntPtr ctx, IntPtr pix);

			[DllImport(DLL, EntryPoint = "fz_new_pixmap", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr NewPixmap(IntPtr ctx, IntPtr colorspace, int width, int height);

			[DllImport(DLL, EntryPoint = "fz_run_page", CallingConvention = CallingConvention.Cdecl)]
			public static extern void RunPage(IntPtr doc, IntPtr page, IntPtr dev, Matrix transform, IntPtr cookie);

			[DllImport(DLL, EntryPoint = "fz_drop_pixmap", CallingConvention = CallingConvention.Cdecl)]
			public static extern void DropPixmap(IntPtr ctx, IntPtr pix);

			[DllImport(DLL, EntryPoint = "fz_pixmap_samples", CallingConvention = CallingConvention.Cdecl)]
			public static extern IntPtr GetSamples(IntPtr ctx, IntPtr pix);
		}
	}

	internal struct BBox
	{
		public int Left, Top, Right, Bottom;
	}

	internal struct Rectangle
	{
		public float Left, Top, Right, Bottom;

		public float Width { get { return this.Right - this.Left; } }
		public float Height { get { return this.Bottom - this.Top; } }
	}

	internal struct Matrix
	{
		public float A, B, C, D, E, F;
	}
}

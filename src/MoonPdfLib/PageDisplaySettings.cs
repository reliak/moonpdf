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
using System.Linq;
using System.Text;

namespace MoonPdfLib
{
	internal class PageDisplaySettings
	{
		public int ImagesPerRow { get; set; }
		public double HorizontalOffsetBetweenPages { get; set; }
		public ViewType ViewType { get; set; }
		public float ZoomFactor { get; set; }
		public ImageRotation Rotation { get; set; }

		public PageDisplaySettings(int imagesPerRow, ViewType viewType, double horizontalOffsetBetweenPages, ImageRotation rotation = ImageRotation.None, float zoomFactor = 1.0f)
		{
			this.ImagesPerRow = imagesPerRow;
			this.ZoomFactor = zoomFactor;
			this.ViewType = viewType;
			this.HorizontalOffsetBetweenPages = viewType == ViewType.SinglePage ? 0 : horizontalOffsetBetweenPages;
			this.Rotation = rotation;
		}
	}
}

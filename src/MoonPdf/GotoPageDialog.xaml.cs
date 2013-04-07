/*! MoonPdf - A WPF-based PDF Viewer application that uses the MoonPdfLib library
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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MoonPdfLib.Helper;

namespace MoonPdf
{
	public partial class GotoPageDialog : Window
	{
		private int MaxPageNumber { get; set; }
		public int? SelectedPageNumber { get; private set; }

		public GotoPageDialog(int currentPageNumber, int maxPageNumber)
		{
			InitializeComponent();

			this.Icon = MoonPdf.Resources.moon.ToBitmap().ToBitmapSource();
			this.MaxPageNumber = maxPageNumber;
			this.txtPage.Text = currentPageNumber.ToString();
			this.lblMaxPageNumber.Content = maxPageNumber;
			this.Loaded += GotoPageDialog_Loaded;
		}

		void GotoPageDialog_Loaded(object sender, RoutedEventArgs e)
		{
			this.txtPage.Focus();
			this.txtPage.SelectAll();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			int page;

			if (!int.TryParse(this.txtPage.Text, out page) || page > MaxPageNumber || page < 1)
			{
				MessageBox.Show("Please enter a valid page number.");
				return;
			}

			this.SelectedPageNumber = page;
			this.DialogResult = true;
			this.Close();
		}

		private void Button_Click_2(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}

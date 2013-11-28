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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MoonPdfLib;
using MoonPdfLib.Helper;
using MoonPdfLib.MuPdf;

namespace MoonPdf
{
	public partial class MainWindow : Window
	{
		private static string appName;
		private MainWindowDataContext dataContext;

		internal MoonPdfPanel MoonPdfPanel { get { return this.moonPdfPanel; } }

		public MainWindow()
		{
			InitializeComponent();

			this.dataContext = new MainWindowDataContext(this);
			this.Icon = MoonPdf.Resources.moon.ToBitmapSource();
			this.DataContext = dataContext;
			this.UpdateTitle();

			moonPdfPanel.ViewTypeChanged += moonPdfPanel_ViewTypeChanged;
			moonPdfPanel.ZoomTypeChanged += moonPdfPanel_ZoomTypeChanged;
			moonPdfPanel.PageRowDisplayChanged += moonPdfPanel_PageDisplayChanged;
			moonPdfPanel.PdfLoaded += moonPdfPanel_PdfLoaded;
            moonPdfPanel.PasswordRequired += moonPdfPanel_PasswordRequired;

			this.UpdatePageDisplayMenuItem();
			this.UpdateZoomMenuItems();
			this.UpdateViewTypeItems();

			this.Loaded += MainWindow_Loaded;
		}

        void moonPdfPanel_PasswordRequired(object sender, PasswordRequiredEventArgs e)
        {
            var dlg = new PdfPasswordDialog();

            if (dlg.ShowDialog() == true)
                e.Password = dlg.Password;
            else
                e.Cancel = true;
        }

		void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			var args = Environment.GetCommandLineArgs();

			// if a filename was given via command line
            if (args.Length > 1 && File.Exists(args[1]))
            {
                try
                {
                    this.moonPdfPanel.OpenFile(args[1]);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("An error occured: " + ex.Message));
                }
            }
		}

		void moonPdfPanel_PageDisplayChanged(object sender, EventArgs e)
		{
			this.UpdatePageDisplayMenuItem();
		}

		private void UpdatePageDisplayMenuItem()
		{
			this.itmContinuously.IsChecked = (this.moonPdfPanel.PageRowDisplay == MoonPdfLib.PageRowDisplayType.ContinuousPageRows);
		}

		void moonPdfPanel_ZoomTypeChanged(object sender, EventArgs e)
		{
			this.UpdateZoomMenuItems();
		}

		private void UpdateZoomMenuItems()
		{
			this.itmFitHeight.IsChecked = moonPdfPanel.ZoomType == ZoomType.FitToHeight;
			this.itmFitWidth.IsChecked = moonPdfPanel.ZoomType == ZoomType.FitToWidth;
			this.itmCustomZoom.IsChecked = moonPdfPanel.ZoomType == ZoomType.Fixed;
		}

		void moonPdfPanel_ViewTypeChanged(object sender, EventArgs e)
		{
			this.UpdateViewTypeItems();
		}

		private void UpdateViewTypeItems()
		{
			switch (this.moonPdfPanel.ViewType)
			{
				case MoonPdfLib.ViewType.SinglePage:
					this.viewSingle.IsChecked = true;
					break;
				case MoonPdfLib.ViewType.Facing:
					this.viewFacing.IsChecked = true;
					break;
				case MoonPdfLib.ViewType.BookView:
					this.viewBook.IsChecked = true;
					break;
				default:
					break;
			}
		}

		void moonPdfPanel_PdfLoaded(object sender, EventArgs e)
		{
			this.UpdateTitle();
		}

		private void UpdateTitle()
		{
			if( appName == null )
				appName = ((AssemblyProductAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), true).First()).Product;

            if (IsPdfLoaded())
            {
                var fs = moonPdfPanel.CurrentSource as FileSource;

                if( fs != null )
                {
                    this.Title = string.Format("{0} - {1}", System.IO.Path.GetFileName(fs.Filename), appName);
                    return;
                }
            }
            
			this.Title = appName;
		}

		internal bool IsPdfLoaded()
		{
            return moonPdfPanel.CurrentSource != null;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			
			if (e.SystemKey == Key.LeftAlt)
			{
				this.mainMenu.Visibility = (this.mainMenu.Visibility == System.Windows.Visibility.Collapsed ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed);

				if (this.mainMenu.Visibility == System.Windows.Visibility.Collapsed)
					e.Handled = true;
			}
		}

		internal void OnFullscreenChanged(bool isFullscreen)
		{
			this.itmFullscreen.IsChecked = isFullscreen;
		}
	}
}

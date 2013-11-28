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
using System.Windows.Input;

namespace MoonPdf
{
	public class Commands
	{
		public DelegateCommand OpenCommand { get; private set; }
		public DelegateCommand ExitCommand { get; private set; }

		public DelegateCommand RotateRightCommand { get; private set; }
		public DelegateCommand RotateLeftCommand { get; private set; }

		public DelegateCommand NextPageCommand { get; private set; }
		public DelegateCommand PreviousPageCommand { get; private set; }
		public DelegateCommand FirstPageCommand { get; private set; }
		public DelegateCommand LastPageCommand { get; private set; }
		
		public DelegateCommand SinglePageCommand { get; private set; }
		public DelegateCommand FacingCommand { get; private set; }
		public DelegateCommand BookViewCommand { get; private set; }

		public DelegateCommand TogglePageDisplayCommand { get; private set; }

		public FullscreenCommand FullscreenCommand { get; private set; }

		public DelegateCommand ZoomInCommand { get; private set; }
		public DelegateCommand ZoomOutCommand { get; private set; }
		public DelegateCommand FitWidthCommand { get; private set; }
		public DelegateCommand FitHeightCommand { get; private set; }
		public DelegateCommand CustomZoomCommand { get; private set; }

		public DelegateCommand ShowAboutCommand { get; private set; }
		public DelegateCommand GotoPageCommand { get; private set; }

		public Commands(MainWindow wnd)
		{
			var pdfPanel = wnd.MoonPdfPanel;
			Predicate<object> isPdfLoaded = f => wnd.IsPdfLoaded(); // used for the CanExecute callback

			this.OpenCommand = new DelegateCommand("Open...", f =>
				{
					var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Select PDF file...", DefaultExt = ".pdf", Filter = "PDF file (.pdf)|*.pdf",CheckFileExists = true };

                    if (dlg.ShowDialog() == true)
                    {
                        try
                        {
                            pdfPanel.OpenFile(dlg.FileName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(string.Format("An error occured: " + ex.Message));
                        }
                    }
				}, f => true, new KeyGesture(Key.O, ModifierKeys.Control));

			this.ExitCommand = new DelegateCommand("Exit", f => wnd.Close(), f => true, new KeyGesture(Key.Q, ModifierKeys.Control));

			this.PreviousPageCommand = new DelegateCommand("Previous page", f => pdfPanel.GotoPreviousPage(), isPdfLoaded, new KeyGesture(Key.Left));
			this.NextPageCommand = new DelegateCommand("Next page", f => pdfPanel.GotoNextPage(), isPdfLoaded, new KeyGesture(Key.Right));
			this.FirstPageCommand = new DelegateCommand("First page", f => pdfPanel.GotoFirstPage(), isPdfLoaded, new KeyGesture(Key.Home));
			this.LastPageCommand = new DelegateCommand("Last page", f => pdfPanel.GotoLastPage(), isPdfLoaded, new KeyGesture(Key.End));
			this.GotoPageCommand = new DelegateCommand("Goto page...", f =>
			{
				var dlg = new GotoPageDialog(pdfPanel.GetCurrentPageNumber(), pdfPanel.TotalPages);

				if (dlg.ShowDialog() == true)
					pdfPanel.GotoPage(dlg.SelectedPageNumber.Value);
			}, isPdfLoaded, new KeyGesture(Key.G, ModifierKeys.Control));

			this.RotateRightCommand = new DelegateCommand("Rotate right", f => pdfPanel.RotateRight(), isPdfLoaded, new KeyGesture(Key.Add, ModifierKeys.Control | ModifierKeys.Shift));
			this.RotateLeftCommand = new DelegateCommand("Rotate left", f => pdfPanel.RotateLeft(), isPdfLoaded, new KeyGesture(Key.Subtract, ModifierKeys.Control | ModifierKeys.Shift));

			this.ZoomInCommand = new DelegateCommand("Zoom in", f => pdfPanel.ZoomIn(), isPdfLoaded, new KeyGesture(Key.Add));
			this.ZoomOutCommand = new DelegateCommand("Zoom out", f => pdfPanel.ZoomOut(), isPdfLoaded, new KeyGesture(Key.Subtract));

			this.FitWidthCommand = new DelegateCommand("Fit width", f => pdfPanel.ZoomToWidth(), isPdfLoaded, new KeyGesture(Key.D4, ModifierKeys.Control));
			this.FitHeightCommand = new DelegateCommand("Fit height", f => pdfPanel.ZoomToHeight(), isPdfLoaded, new KeyGesture(Key.D5, ModifierKeys.Control));
			this.CustomZoomCommand = new DelegateCommand("Custom zoom", f => pdfPanel.SetFixedZoom(), isPdfLoaded, new KeyGesture(Key.D6, ModifierKeys.Control));

			this.TogglePageDisplayCommand = new DelegateCommand("Show pages continuously", f => pdfPanel.TogglePageDisplay(), isPdfLoaded, new KeyGesture(Key.D7, ModifierKeys.Control));

			this.FullscreenCommand = new FullscreenCommand("Full screen", wnd, new KeyGesture(Key.L, ModifierKeys.Control));

			this.SinglePageCommand = new DelegateCommand("Single page", f => { pdfPanel.ViewType = MoonPdfLib.ViewType.SinglePage; }, isPdfLoaded, new KeyGesture(Key.D1, ModifierKeys.Control));
			this.FacingCommand = new DelegateCommand("Facing", f => { pdfPanel.ViewType = MoonPdfLib.ViewType.Facing; }, isPdfLoaded, new KeyGesture(Key.D2, ModifierKeys.Control));
			this.BookViewCommand = new DelegateCommand("Book view", f => { pdfPanel.ViewType = MoonPdfLib.ViewType.BookView; }, isPdfLoaded, new KeyGesture(Key.D3, ModifierKeys.Control));

			this.ShowAboutCommand = new DelegateCommand("About", f => new AboutWindow().ShowDialog(), f => true, null);

			this.RegisterInputBindings(wnd);
		}

		private void RegisterInputBindings(MainWindow wnd)
		{
			// register inputbindings for all commands (properties)
			foreach (var pi in typeof(Commands).GetProperties())
			{
				var cmd = (BaseCommand)pi.GetValue(this, null);

				if (cmd.InputBinding != null)
					wnd.InputBindings.Add(cmd.InputBinding);
			}
		}
	}
}

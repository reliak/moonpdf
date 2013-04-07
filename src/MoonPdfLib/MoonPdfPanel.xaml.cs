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
using System.IO;
using System.Linq;
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
using MoonPdfLib.MuPdf;
using MoonPdfLib.Helper;
using System.Windows.Threading;
using System.ComponentModel;

namespace MoonPdfLib
{
	public partial class MoonPdfPanel : UserControl
	{
		public event EventHandler FileLoaded;
		public event EventHandler ZoomTypeChanged;
		public event EventHandler ViewTypeChanged;
		public event EventHandler PageDisplayChanged;

		private ZoomType zoomType = ZoomType.Fixed;
		private IMoonPdfPanel innerPanel;
		private MoonPdfPanelInputHandler inputHandler;
		private PdfPageBound[] itemBounds;
		private DispatcherTimer resizeTimer;

		#region Dependency properties
		public static readonly DependencyProperty PageBorderThicknessProperty = DependencyProperty.Register("PageBorderThickness", typeof(Thickness),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(new Thickness(0, 2, 4, 2)));

		public static readonly DependencyProperty ZoomStepProperty = DependencyProperty.Register("ZoomStep", typeof(double),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(0.25));

		public static readonly DependencyProperty MinZoomFactorProperty = DependencyProperty.Register("MinZoomFactor", typeof(double),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(0.15));

		public static readonly DependencyProperty MaxZoomFactorProperty = DependencyProperty.Register("MaxZoomFactor", typeof(double),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(6.0));

		public static readonly DependencyProperty ViewTypeProperty = DependencyProperty.Register("ViewType", typeof(ViewType),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(MoonPdfLib.ViewType.SinglePage));

		public static readonly DependencyProperty RotationProperty = DependencyProperty.Register("Rotation", typeof(ImageRotation),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(ImageRotation.None));

		public static readonly DependencyProperty PageDisplayProperty = DependencyProperty.Register("PageDisplay", typeof(PageDisplayType),
																			typeof(MoonPdfPanel), new FrameworkPropertyMetadata(PageDisplayType.SinglePage));
		
		public Thickness PageBorderThickness
		{
			get { return (Thickness)GetValue(PageBorderThicknessProperty); }
			set { SetValue(PageBorderThicknessProperty, value); }
		}

		public double ZoomStep
		{
			get { return (double)GetValue(ZoomStepProperty); }
			set { SetValue(ZoomStepProperty, value); }
		}

		public double MinZoomFactor
		{
			get { return (double)GetValue(MinZoomFactorProperty); }
			set { SetValue(MinZoomFactorProperty, value); }
		}

		public double MaxZoomFactor
		{
			get { return (double)GetValue(MaxZoomFactorProperty); }
			set { SetValue(MaxZoomFactorProperty, value); }
		}

		public MoonPdfLib.ViewType ViewType
		{
			get { return (MoonPdfLib.ViewType)GetValue(ViewTypeProperty); }
			set { SetValue(ViewTypeProperty, value); }
		}
				
		public ImageRotation Rotation
		{
			get { return (ImageRotation)GetValue(RotationProperty); }
			set { SetValue(RotationProperty, value); }
		}

		public PageDisplayType PageDisplay
		{
			get { return (PageDisplayType)GetValue(PageDisplayProperty); }
			set { SetValue(PageDisplayProperty, value); }
		}
		#endregion

		public double HorizontalMargin { get { return this.PageBorderThickness.Right; } }
		public string CurrentFilename { get; private set; }
		public int TotalPages { get; private set; }
		internal PdfPageBound[] ItemBounds { get { return this.itemBounds; } }

		public ZoomType ZoomType
		{
			get { return this.zoomType; }
			private set
			{
				if (this.zoomType != value)
				{
					this.zoomType = value;

					if (ZoomTypeChanged != null)
						ZoomTypeChanged(this, EventArgs.Empty);
				}
			}
		}

		internal ScrollViewer ScrollViewer
		{
			get { return this.innerPanel.ScrollViewer; }
		}

		public float CurrentZoom
		{
			get { return this.innerPanel.CurrentZoom; }
		}

		public MoonPdfPanel()
		{
			InitializeComponent();

			this.ChangeDisplayType(this.PageDisplay);
			this.inputHandler = new MoonPdfPanelInputHandler(this);

			this.SizeChanged += PdfViewerPanel_SizeChanged;

			resizeTimer = new DispatcherTimer();
			resizeTimer.Interval = TimeSpan.FromMilliseconds(150);
			resizeTimer.Tick += resizeTimer_Tick;
		}

		void PdfViewerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (this.CurrentFilename == null)
				return;

			resizeTimer.Stop();
			resizeTimer.Start();
		}

		void resizeTimer_Tick(object sender, EventArgs e)
		{
			resizeTimer.Stop();

			if (this.CurrentFilename == null)
				return;

			if (this.ZoomType == ZoomType.FitToWidth)
				ZoomToWidth();
			else if (this.ZoomType == ZoomType.FitToHeight)
				ZoomToHeight();
		}

		public void OpenFile(string pdfFilename)
		{
			if (!File.Exists(pdfFilename))
				throw new FileNotFoundException(string.Empty, pdfFilename);

			this.LoadPdfFile(pdfFilename);
			this.CurrentFilename = pdfFilename;

			if (this.FileLoaded != null)
				this.FileLoaded(this, EventArgs.Empty);
		}

		private void LoadPdfFile(string pdfFilename)
		{
			var tmpBounds = MuPdfWrapper.GetPageBounds(pdfFilename, this.Rotation);
			var finalBounds = CalculatePageBounds(tmpBounds, this.ViewType);
			this.itemBounds = finalBounds.ToArray();
			this.TotalPages = tmpBounds.Length;
			this.innerPanel.Load(pdfFilename);
		}

		private PdfPageBound[] CalculatePageBounds(Size[] singlePageBounds, ViewType viewType)
		{
			var pagesPerRow = Math.Min(GetPagesPerRow(), singlePageBounds.Length); // if multiple page-view, but pdf contains less pages than the pages per row
			var finalBounds = new List<PdfPageBound>();
			var verticalBorderOffset = (this.PageBorderThickness.Top + this.PageBorderThickness.Bottom);

			if (viewType == MoonPdfLib.ViewType.SinglePage)
			{
				finalBounds.AddRange(singlePageBounds.Select(p => new PdfPageBound( p, verticalBorderOffset, 0)));
			}
			else
			{
				var horizontalBorderOffset = this.HorizontalMargin;

				for (int i = 0; i < singlePageBounds.Length; i++)
				{
					if (i == 0 && viewType == MoonPdfLib.ViewType.BookView)
					{
						finalBounds.Add(new PdfPageBound(singlePageBounds[0], verticalBorderOffset, 0));
						continue;
					}

					var subset = singlePageBounds.Take(i, pagesPerRow).ToArray();

					// we get the max page-height from all pages in the subset and the sum of all page widths of the subset plus the offset between the pages
					finalBounds.Add(new PdfPageBound(new Size(subset.Sum(f => f.Width), subset.Max(f => f.Height)), verticalBorderOffset, horizontalBorderOffset * (subset.Length - 1)));
					i += (pagesPerRow - 1);
				}
			}

			return finalBounds.ToArray();
		}

		internal int GetPagesPerRow()
		{
			return this.ViewType == MoonPdfLib.ViewType.SinglePage ? 1 : 2;
		}

		public int GetCurrentPageNumber()
		{
			if (this.innerPanel == null)
				return -1;

			return this.innerPanel.GetCurrentPageIndex(this.ViewType) + 1;
		}

		public void ZoomToWidth()
		{
			this.innerPanel.ZoomToWidth();
			this.ZoomType = MoonPdfLib.ZoomType.FitToWidth;
		}

		public void ZoomToHeight()
		{
			this.innerPanel.ZoomToHeight();
			this.ZoomType = ZoomType.FitToHeight;
		}

		public void ZoomIn()
		{
            this.innerPanel.ZoomIn();
			this.ZoomType = ZoomType.Fixed;
		}

		public void ZoomOut()
		{
            this.innerPanel.ZoomOut();
			this.ZoomType = ZoomType.Fixed;
		}

		public void Zoom(double zoomFactor)
		{
            this.innerPanel.Zoom(zoomFactor);
			this.ZoomType = ZoomType.Fixed;
		}

		public void SetFixedZoom()
		{
			this.ZoomType = MoonPdfLib.ZoomType.Fixed;
		}

		public void GotoPreviousPage()
		{
			this.innerPanel.GotoPreviousPage();
		}

		public void GotoNextPage()
		{
			this.innerPanel.GotoNextPage();
		}

		public void GotoPage(int pageNumber)
		{
			this.innerPanel.GotoPage(pageNumber);
		}

		public void GotoFirstPage()
		{
			this.GotoPage(1);
		}

		public void GotoLastPage()
		{
			this.GotoPage(this.TotalPages);
		}

		public void RotateRight()
		{
			if ((int)this.Rotation < Enum.GetValues(typeof(ImageRotation)).Length)
				this.Rotation = (ImageRotation)this.Rotation + 1;
			else
				this.Rotation = ImageRotation.None;
		}

		public void RotateLeft()
		{
			if ((int)this.Rotation > 0)
				this.Rotation = (ImageRotation)this.Rotation -1;
			else
				this.Rotation = ImageRotation.Rotate270;
		}

		public void Rotate(ImageRotation rotation)
		{
			var currentPage = this.innerPanel.GetCurrentPageIndex(this.ViewType) + 1;
			this.LoadPdfFile(this.CurrentFilename);
			this.innerPanel.GotoPage(currentPage);
		}

		public void TogglePageDisplay()
		{
			this.PageDisplay = (this.PageDisplay == PageDisplayType.SinglePage) ? PageDisplayType.ContinuousPages : PageDisplayType.SinglePage;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);

			if (e.Property.Name.Equals("PageDisplay"))
				ChangeDisplayType((PageDisplayType)e.NewValue);
			else if (e.Property.Name.Equals("Rotation"))
				this.Rotate((ImageRotation)e.NewValue);
			else if (e.Property.Name.Equals("ViewType"))
				this.ApplyChangedViewType((ViewType)e.OldValue);
		}

		private void ApplyChangedViewType(ViewType oldViewType)
		{
			UpdateAndReload(() => {}, oldViewType);

			if (this.ViewTypeChanged != null)
				this.ViewTypeChanged(this, EventArgs.Empty);
		}

		private void ChangeDisplayType(PageDisplayType pageDisplayType)
		{
			UpdateAndReload(() =>
				{
					// we need to remove the current innerPanel
					this.pnlMain.Children.Clear();

					if (pageDisplayType == PageDisplayType.SinglePage)
						this.innerPanel = new SinglePageMoonPdfPanel(this);
					else
						this.innerPanel = new ContinuousMoonPdfPanel(this);

					this.pnlMain.Children.Add(this.innerPanel.Instance);
				}, this.ViewType);

			if (this.PageDisplayChanged != null)
				this.PageDisplayChanged(this, EventArgs.Empty);
		}

		private void UpdateAndReload(Action updateAction, ViewType viewType)
		{
			var currentPage = -1;
			var zoom = 1.0f;

			if (this.CurrentFilename != null)
			{
				currentPage = this.innerPanel.GetCurrentPageIndex(viewType) + 1;
				zoom = this.innerPanel.CurrentZoom;
			}

			updateAction();

			if (currentPage > -1)
			{
				Action reloadAction = () =>
					{
						this.LoadPdfFile(this.CurrentFilename);
						this.innerPanel.Zoom(zoom);
						this.innerPanel.GotoPage(currentPage);
					};

				if (this.innerPanel.Instance.IsLoaded)
					reloadAction();
				else
				{
					// we need to wait until the controls are loaded and then reload the pdf
					this.innerPanel.Instance.Loaded += (s, e) => { reloadAction();	};
				}
			}
		}

		/// <summary>
		/// Will only be triggered if the AllowDrop-Property is set to true
		/// </summary>
		/// <param name="e"></param>
		protected override void OnDrop(DragEventArgs e)
		{
			base.OnDrop(e);

			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var filenames = (string[])e.Data.GetData(DataFormats.FileDrop);
				var filename = filenames.FirstOrDefault();

				if (filename != null && File.Exists(filename))
					this.OpenFile(filename);
			}
		}
	}
}

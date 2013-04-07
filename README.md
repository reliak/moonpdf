MoonPdf
=======

MoonPdf is a WPF-based PDF Viewer that uses the MoonPdfLib library.

The MoonPdfLib contains a WPF control that can be included in your application.
An example for the inclusion is the MoonPdf app (see excerpt below)
```xml
<Window xmlns:mpp="clr-namespace:MoonPdfLib;assembly=MoonPdfLib" ...>
  <mpp:MoonPdfPanel Background="LightGray" ViewType="SinglePage" PageDisplay="ContinuousPages" PageBorderThickness="0,2,4,2" AllowDrop="True"/>
</Window>

#region Namespaces
using System;
using Autodesk.Revit.UI;
using System.Reflection;
using RevitFamilyBrowser.WPF_Classes;
using RevitFamilyBrowser.Revit_Classes;
using RevitFamilyBrowser.Properties;
using System.IO;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Collections.Generic;

#endregion

namespace RevitFamilyBrowser
{
    class App : IExternalApplication
    {
        public static App thisApp = null;
        public Result OnStartup(UIControlledApplication a)
        {
            a.CreateRibbonTab("Familien Browser"); //Familien Browser Families Browser
            RibbonPanel G17 = a.CreateRibbonPanel("Familien Browser", "Familien Browser");
            string path = Assembly.GetExecutingAssembly().Location;

            MyEvent handler = new MyEvent();
            ExternalEvent exEvent = ExternalEvent.Create(handler);

            DockPanel dockPanel = new DockPanel(exEvent, handler);
            DockablePaneId dpID = new DockablePaneId(new Guid("FA0C04E6-F9E7-413A-9D33-CFE32622E7B8"));
            a.RegisterDockablePane(dpID, "Familien Browser", (IDockablePaneProvider)dockPanel);

            PushButtonData btnShow = new PushButtonData("ShowPanel", "Panel\nanzeigen", path, "RevitFamilyBrowser.Revit_Classes.ShowPanel"); //Panel anzeigen ShowPanel
            btnShow.LargeImage = GetImage(Resources.IconShowPanel.GetHbitmap());
            RibbonItem ri1 = G17.AddItem(btnShow);

            PushButtonData btnFolder = new PushButtonData("OpenFolder", "Verzeichnis\n�ffnen", path, "RevitFamilyBrowser.Revit_Classes.FolderSelect");   //Verzeichnis  �ffnen        
            btnFolder.LargeImage = GetImage(Resources.OpenFolder.GetHbitmap());
            RibbonItem ri2 = G17.AddItem(btnFolder);

            //PushButtonData btnImage = new PushButtonData("Image", "Image", path, "RevitFamilyBrowser.Revit_Classes.Test");
            //RibbonItem ri3 = G17.AddItem(btnImage);           

            a.ControlledApplication.DocumentChanged += OnDocChanged;
            a.ControlledApplication.DocumentOpened += OnDocOpened;
            a.ControlledApplication.DocumentClosed += OnDocClosed;
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {           
            DirectoryInfo di = new DirectoryInfo(System.IO.Path.GetTempPath() + "FamilyBrowser\\");
            foreach (var imgfile in di.GetFiles())
            {
                try
                {
                    imgfile.Delete();
                }
                catch (Exception){}
            }
            a.ControlledApplication.DocumentChanged -= OnDocChanged;
            a.ControlledApplication.DocumentOpened -= OnDocOpened;
            a.ControlledApplication.DocumentClosed -= OnDocClosed;

            Properties.Settings.Default.CollectedData = string.Empty;
            Properties.Settings.Default.FamilyPath = string.Empty;
            Properties.Settings.Default.SymbolList = string.Empty;

            return Result.Succeeded;
        }
        private void OnDocOpened(object sender, DocumentOpenedEventArgs e)
        {
            Properties.Settings.Default.CollectedData = string.Empty;
            Autodesk.Revit.DB.Document doc = e.Document;
            CollectFamilyData(doc);
            CreateImages(e.Document);
        }

        private void OnDocChanged(object sender, DocumentChangedEventArgs e)
        {           
            Autodesk.Revit.DB.Document doc = e.GetDocument();
            CollectFamilyData(doc);
            CreateImages(doc);
        }
        private void OnDocClosed(object sender, DocumentClosedEventArgs e)
        {
            Properties.Settings.Default.CollectedData = string.Empty;
        }

        public void CollectFamilyData(Autodesk.Revit.DB.Document doc)
        {
            FilteredElementCollector families;
            Properties.Settings.Default.CollectedData = string.Empty;
            families = new FilteredElementCollector(doc).OfClass(typeof(Family));
            string temp = string.Empty;

            foreach (var item in families)
            {
                if (!(item.Name.Contains("Standart") ||
                    item.Name.Contains("Mullion")))
                {
                    Family family = item as Family;
                    FamilySymbol symbol;
                    temp += item.Name;
                    ISet<ElementId> familySymbolId = family.GetFamilySymbolIds();
                    foreach (ElementId id in familySymbolId)
                    {
                        symbol = family.Document.GetElement(id) as FamilySymbol;
                        {
                            temp += "#" + symbol.Name;
                        }
                    }
                    temp += "\n";
                }
            }
            Properties.Settings.Default.CollectedData = temp;
        }

        public void CreateImages(Autodesk.Revit.DB.Document doc)
        {
            FilteredElementCollector collector;
            collector = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance));

            foreach (FamilyInstance fi in collector)
            {              
                ElementId typeId = fi.GetTypeId();
                ElementType type = doc.GetElement(typeId) as ElementType;
                System.Drawing.Size imgSize = new System.Drawing.Size(200, 200);

                //------------Prewiew Image-----
                Bitmap image = type.GetPreviewImage(imgSize);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(ConvertBitmapToBitmapSource(image)));
                encoder.QualityLevel = 25;

                string TempImgFolder = System.IO.Path.GetTempPath() + "FamilyBrowser\\";
                if (!System.IO.Directory.Exists(TempImgFolder))
                {
                    System.IO.Directory.CreateDirectory(TempImgFolder);
                }
                string filename = TempImgFolder + type.Name + ".bmp";
                   foreach (var fileimage in Directory.GetFiles(TempImgFolder))
                {
                    if (filename != fileimage)
                    {
                        FileStream file = new FileStream(filename, FileMode.Create, FileAccess.Write);
                        encoder.Save(file);
                        file.Close();
                    }
                }
            }
        }

        private BitmapSource GetImage(IntPtr bm)
        {
            BitmapSource bmSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bm,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return bmSource;
        }

        static BitmapSource ConvertBitmapToBitmapSource(Bitmap bmp)
        {
            return System.Windows.Interop.Imaging
              .CreateBitmapSourceFromHBitmap(
                bmp.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
    }
}

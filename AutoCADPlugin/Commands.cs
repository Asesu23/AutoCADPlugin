using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Runtime;
using PdfiumViewer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SheetAreaSelector
{
    public class Commands
    {

        private void DirectPlot(string filePath, double minX, double minY, double maxX, double maxY, string orient, string upsideDown)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                Layout lo = (Layout)tr.GetObject(LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout), OpenMode.ForRead);

                PlotInfo pi = new PlotInfo();
                pi.Layout = lo.Id;

                PlotSettings ps = new PlotSettings(lo.ModelType);
                ps.CopyFrom(lo);
                PlotSettingsValidator psv = PlotSettingsValidator.Current;

                psv.SetPlotConfigurationName(ps, "AutoCAD PDF (General Documentation).pc3", "ISO_full_bleed_A4_(210.00_x_297.00_MM)");
                psv.SetPlotWindowArea(ps, new Extents2d(minX, minY, maxX, maxY));
                psv.SetPlotType(ps, Autodesk.AutoCAD.DatabaseServices.PlotType.Window);
                psv.SetPlotCentered(ps, true);
                psv.SetStdScaleType(ps, StdScaleType.ScaleToFit);

                // Landscape or portrait plus the upside-down flag decide the plot rotation
                PlotRotation rotation = (orient == "L") ? PlotRotation.Degrees090 : PlotRotation.Degrees000;
                if (upsideDown == "Y")
                    rotation = (orient == "L") ? PlotRotation.Degrees270 : PlotRotation.Degrees180;

                psv.SetPlotRotation(ps, rotation);

                pi.OverrideSettings = ps;
                PlotInfoValidator piv = new PlotInfoValidator();
                piv.MediaMatchingPolicy = MatchingPolicy.MatchEnabled;
                piv.Validate(pi);

                if (PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting)
                {
                    using (PlotEngine pe = PlotFactory.CreatePublishEngine())
                    {
                        pe.BeginPlot(null, null);
                        pe.BeginDocument(pi, doc.Name, null, 1, true, filePath);
                        pe.BeginPage(new PlotPageInfo(), pi, true, null);
                        pe.BeginGenerateGraphics(null);
                        pe.EndGenerateGraphics(null);
                        pe.EndPage(null);
                        pe.EndDocument(null);
                        pe.EndPlot(null);
                    }
                }
                tr.Commit();
            }
        }

        private static string _customFolderPath = @"C:\AutocadJpgResult\";
        public static string RowX { get; set; } = "1";
        public static string ColY { get; set; } = "1";
        public static string StartFrom { get; set; } = "1";

        public static string CustomFolderPath
        {
            get { return _customFolderPath; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _customFolderPath = value;
                }
            }
        }

        [CommandMethod("SelectSheetAreaRunScriptNow")]
        public static void RunSelectionAndSave()
        {
            var cmd = new Commands();
            cmd.SelectSheetAreaRunScriptNow(_customFolderPath, Commands.RowX, Commands.ColY, Commands.StartFrom);
        }

        public static void RunSelectionAndSaveWithCustomPath(string folderPath)
        {
            var cmd = new Commands();
            cmd.SelectSheetAreaRunScriptNow(folderPath);
        }

        public void SelectSheetAreaRunScriptNow(string folderPath = null, string pioX = null, string pioFloors = null, string pioStart = null)
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            object oldBackPlot = Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("BACKGROUNDPLOT");
            Autodesk.AutoCAD.ApplicationServices.Core.Application.SetSystemVariable("BACKGROUNDPLOT", 0);

            try
            {
                if (!int.TryParse(pioX, out int xRepeats)) xRepeats = 1;
                if (!int.TryParse(pioFloors, out int floorsCount)) floorsCount = 1;
                if (!int.TryParse(pioStart, out int startNumber)) startNumber = 1;

                PromptPointResult p1 = ed.GetPoint("\nУкажите первую точку области 1го в списке файла: ");
                if (p1.Status != PromptStatus.OK) return;

                PromptCornerOptions pco =
                    new PromptCornerOptions("\nУкажите противоположную точку области 1го в списке файла: ", p1.Value);
                PromptPointResult p2 = ed.GetCorner(pco);
                if (p2.Status != PromptStatus.OK) return;

                double minX = Math.Min(p1.Value.X, p2.Value.X);
                double minY = Math.Min(p1.Value.Y, p2.Value.Y);
                double maxX = Math.Max(p1.Value.X, p2.Value.X);
                double maxY = Math.Max(p1.Value.Y, p2.Value.Y);

                double areaWidth = maxX - minX;
                double areaHeight = maxY - minY;

                ed.WriteMessage($"\nВыбрана область: от ({minX:F2},{minY:F2}) до ({maxX:F2},{maxY:F2})");

                string orientation;
                string upsideDownOrientation;
                if (areaWidth > areaHeight) { orientation = "L"; upsideDownOrientation = "Y"; } else { orientation = "P"; upsideDownOrientation = "N"; }

                string baseDir = string.IsNullOrEmpty(folderPath) ? _customFolderPath : folderPath;

                Directory.CreateDirectory(baseDir);

                int currentNumber = startNumber;

                // Floors shift the plot window down by one area height, repeats shift it right by one area width
                for (int floor = 0; floor < floorsCount; floor++)
                {
                    double currentMinY = minY - (floor * areaHeight);
                    double currentMaxY = maxY - (floor * areaHeight);

                    for (int xRepeat = 0; xRepeat < xRepeats; xRepeat++)
                    {
                        double currentMinX = minX + (xRepeat * areaWidth);
                        double currentMaxX = maxX + (xRepeat * areaWidth);

                        string currentOutImagePath = Path.Combine(baseDir, $"{currentNumber}.pdf");
                        string escapedPath = currentOutImagePath.Contains(" ") ?
                            $"\"{currentOutImagePath}\"" : currentOutImagePath;

                        DirectPlot(escapedPath, currentMinX, currentMinY, currentMaxX, currentMaxY, orientation, upsideDownOrientation);

                        currentNumber++;
                    }
                }
                int totalCount = xRepeats * floorsCount;
                StartConversionWithTimer(baseDir, startNumber, totalCount);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[ERR] " + ex.Message);
            }
        }

        private void StartConversionWithTimer(string folderPath, int startNumber, int totalCount)
        {
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int processedCount = 0;
            const int dpi = 300;

            // PDFs can still be locked right after plotting, so poll until each one opens, then convert it to JPG
            var timer = new Timer { Interval = 500 };
            timer.Tick += (s, e) =>
            {
                Document doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc == null) return;

                for (int i = 0; i < totalCount; i++)
                {
                    int number = startNumber + i;
                    string pdfPath = Path.Combine(folderPath, $"{number}.pdf");
                    if (File.Exists(pdfPath) && !processedFiles.Contains(pdfPath))
                    {
                        if (IsFileReady(pdfPath))
                        {
                            try
                            {
                                string jpgPath = Path.Combine(folderPath, $"{number}.jpg");
                                ConvertPdfToJpg(pdfPath, jpgPath, dpi);

                                File.Delete(pdfPath);

                                processedFiles.Add(pdfPath);
                                processedCount++;
                            }
                            catch (System.Exception ex)
                            {
                                doc.Editor.WriteMessage($"\n[ERR] {pdfPath}: {ex.Message}");
                            }
                        }
                    }
                }

                if (processedCount >= totalCount)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };

            timer.Start();
            Document mainDoc = AcadApp.DocumentManager.MdiActiveDocument;
            mainDoc.Editor.WriteMessage($"\n[CONV] Запущен таймер конвертации для {totalCount} файлов (JPG, {dpi} DPI)...");
        }

        private bool IsFileReady(string filePath)
        {
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ConvertPdfToJpg(string pdfPath, string jpgPath, int dpi)
        {
            using (var pdfDoc = PdfDocument.Load(pdfPath))
            {
                if (pdfDoc.PageCount > 0)
                {
                    var flags = PdfRenderFlags.LcdText | PdfRenderFlags.ForPrinting |
                                PdfRenderFlags.Annotations | PdfRenderFlags.CorrectFromDpi;
                    using (var image = pdfDoc.Render(0, dpi, dpi, flags))
                    {
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);
                        var jpegCodec = GetEncoder(ImageFormat.Jpeg);
                        image.Save(jpgPath, jpegCodec, encoderParams);
                    }
                }
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }
    }
}
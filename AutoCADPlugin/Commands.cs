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

        public static void Run()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

            ExportSettings settings = ExportSettings.Current;
            if (settings.HasArea && settings.AreaDocument != doc.Name)
                settings.ClearArea();

            try
            {
                while (true)
                {
                    DialogResult result;
                    using (var form = new JpeggerForm(settings))
                    {
                        form.PreviewChanged += (s, e) => GridPreview.Show(settings);
                        GridPreview.Show(settings);
                        result = AcadApp.ShowModalDialog(form);
                    }

                    GridPreview.Clear();
                    settings.Save();

                    if (result == DialogResult.Retry)
                    {
                        PickArea(doc, settings);
                        continue;
                    }

                    if (result == DialogResult.OK)
                        new Commands().Export(doc, settings);

                    break;
                }
            }
            finally
            {
                GridPreview.Clear();
            }
        }

        private static void PickArea(Document doc, ExportSettings settings)
        {
            Editor ed = doc.Editor;

            PromptPointResult first = ed.GetPoint("\nУкажите первый угол области: ");
            if (first.Status != PromptStatus.OK) return;

            var options = new PromptCornerOptions("\nУкажите противоположный угол области: ", first.Value);
            PromptPointResult second = ed.GetCorner(options);
            if (second.Status != PromptStatus.OK) return;

            double minX = Math.Min(first.Value.X, second.Value.X);
            double minY = Math.Min(first.Value.Y, second.Value.Y);
            double maxX = Math.Max(first.Value.X, second.Value.X);
            double maxY = Math.Max(first.Value.Y, second.Value.Y);

            if (maxX - minX < 1e-9 || maxY - minY < 1e-9)
            {
                ed.WriteMessage("\nОбласть не должна быть нулевого размера.");
                return;
            }

            settings.SetArea(minX, minY, maxX, maxY, doc.Name);
        }

        private void Export(Document doc, ExportSettings settings)
        {
            Editor ed = doc.Editor;
            object oldBackgroundPlot = null;
            ProgressMeter progress = null;

            try
            {
                oldBackgroundPlot = Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("BACKGROUNDPLOT");
                Autodesk.AutoCAD.ApplicationServices.Core.Application.SetSystemVariable("BACKGROUNDPLOT", 0);

                string baseDir = settings.OutputFolder;
                Directory.CreateDirectory(baseDir);

                double areaWidth = settings.AreaWidth;
                double areaHeight = settings.AreaHeight;
                string orientation = settings.IsLandscape ? "L" : "P";
                string upsideDownOrientation = settings.IsLandscape ? "Y" : "N";

                int totalCount = settings.TotalCount;
                int currentNumber = settings.StartNumber;

                progress = new ProgressMeter();
                progress.Start("Jpegger: печать областей");
                progress.SetLimit(totalCount);

                // Floors shift the plot window down by one area height, repeats shift it right by one area width
                for (int floor = 0; floor < settings.Rows; floor++)
                {
                    double currentMinY = settings.MinY - (floor * areaHeight);
                    double currentMaxY = settings.MaxY - (floor * areaHeight);

                    for (int xRepeat = 0; xRepeat < settings.Columns; xRepeat++)
                    {
                        double currentMinX = settings.MinX + (xRepeat * areaWidth);
                        double currentMaxX = settings.MaxX + (xRepeat * areaWidth);

                        string currentOutImagePath = Path.Combine(baseDir, $"{currentNumber}.pdf");
                        string escapedPath = currentOutImagePath.Contains(" ") ?
                            $"\"{currentOutImagePath}\"" : currentOutImagePath;

                        DirectPlot(escapedPath, currentMinX, currentMinY, currentMaxX, currentMaxY, orientation, upsideDownOrientation);

                        currentNumber++;
                        progress.MeterProgress();
                    }
                }

                StartConversionWithTimer(baseDir, settings.StartNumber, totalCount, settings.OpenFolderAfterExport);
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("\n[ERR] " + ex.Message);
            }
            finally
            {
                if (progress != null) progress.Stop();
                if (oldBackgroundPlot != null)
                {
                    try { Autodesk.AutoCAD.ApplicationServices.Core.Application.SetSystemVariable("BACKGROUNDPLOT", oldBackgroundPlot); }
                    catch { }
                }
            }
        }

        private void StartConversionWithTimer(string folderPath, int startNumber, int totalCount, bool openFolder)
        {
            var processedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int convertedCount = 0;
            int failedCount = 0;
            const int dpi = 300;
            DateTime deadline = DateTime.UtcNow.AddMinutes(5);

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
                    if (File.Exists(pdfPath) && !processedFiles.Contains(pdfPath) && IsFileReady(pdfPath))
                    {
                        processedFiles.Add(pdfPath);
                        try
                        {
                            string jpgPath = Path.Combine(folderPath, $"{number}.jpg");
                            ConvertPdfToJpg(pdfPath, jpgPath, dpi);

                            File.Delete(pdfPath);
                            convertedCount++;
                        }
                        catch (System.Exception ex)
                        {
                            failedCount++;
                            doc.Editor.WriteMessage($"\n[ERR] {pdfPath}: {ex.Message}");
                        }
                    }
                }

                bool finished = convertedCount + failedCount >= totalCount;
                if (finished || DateTime.UtcNow > deadline)
                {
                    timer.Stop();
                    timer.Dispose();
                    doc.Editor.WriteMessage($"\n[Jpegger] Готово: {convertedCount} из {totalCount} файлов сохранено в {folderPath}");
                    if (openFolder && convertedCount > 0) OpenFolder(folderPath);
                }
            };

            timer.Start();
            Document mainDoc = AcadApp.DocumentManager.MdiActiveDocument;
            mainDoc.Editor.WriteMessage($"\n[Jpegger] Конвертация {totalCount} файлов в JPG ({dpi} DPI)...");
        }

        private static void OpenFolder(string folderPath)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + folderPath + "\"");
            }
            catch
            {
            }
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

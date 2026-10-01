using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Mode = Autodesk.AutoCAD.GraphicsInterface.TransientDrawingMode;

namespace SheetAreaSelector
{
    internal static class GridPreview
    {
        private const int SubDrawingMode = 4711;
        private const int MaxDetailedTiles = 300;
        private const int FirstTileColor = 2;
        private const int TileColor = 4;

        // 70% of the tile color blended over a dark background, so the plate needs no transparency
        private static readonly Autodesk.AutoCAD.Colors.Color FirstPlateColor = Autodesk.AutoCAD.Colors.Color.FromRgb(188, 191, 14);
        private static readonly Autodesk.AutoCAD.Colors.Color TilePlateColor = Autodesk.AutoCAD.Colors.Color.FromRgb(10, 191, 193);

        private static readonly List<Entity> Items = new List<Entity>();

        public static void Show(ExportSettings settings)
        {
            Clear();
            if (!settings.HasArea) return;

            try
            {
                if (settings.TotalCount <= MaxDetailedTiles)
                    DrawTiles(settings);
                else
                    DrawOutline(settings);
                Refresh();
            }
            catch
            {
                Clear();
            }
        }

        public static void Clear()
        {
            if (Items.Count == 0) return;

            try
            {
                var manager = Autodesk.AutoCAD.GraphicsInterface.TransientManager.CurrentTransientManager;
                foreach (Entity item in Items)
                {
                    try { manager.EraseTransient(item, new IntegerCollection()); }
                    catch { }
                }
            }
            catch
            {
            }

            foreach (Entity item in Items)
            {
                try { item.Dispose(); }
                catch { }
            }
            Items.Clear();
            Refresh();
        }

        private static void DrawTiles(ExportSettings settings)
        {
            double width = settings.AreaWidth;
            double height = settings.AreaHeight;
            double textHeight = Math.Min(width, height) * 0.1;
            double spacing = TintSpacing(height);
            int number = settings.StartNumber;

            for (int row = 0; row < settings.Rows; row++)
            {
                for (int column = 0; column < settings.Columns; column++)
                {
                    double x = settings.MinX + column * width;
                    double y = settings.MinY - row * height;
                    bool first = row == 0 && column == 0;
                    string text = number.ToString(CultureInfo.InvariantCulture);
                    double centerX = x + width / 2;
                    double centerY = y + height / 2;

                    Add(CreateTint(x, y, x + width, y + height, first ? FirstTileColor : TileColor, spacing), Mode.DirectShortTerm);
                    Add(CreateFrame(x, y, x + width, y + height, first ? FirstTileColor : TileColor), Mode.DirectShortTerm);
                    Add(CreatePlate(text, centerX, centerY, textHeight, first), Mode.DirectShortTerm);
                    Add(CreateLabel(text, centerX, centerY, textHeight), Mode.DirectTopmost);
                    number++;
                }
            }
        }

        private static void DrawOutline(ExportSettings settings)
        {
            double width = settings.AreaWidth;
            double height = settings.AreaHeight;

            Add(CreateFrame(settings.MinX, settings.MinY - (settings.Rows - 1) * height,
                settings.MinX + settings.Columns * width, settings.MaxY, TileColor), Mode.DirectShortTerm);
            Add(CreateFrame(settings.MinX, settings.MinY, settings.MaxX, settings.MaxY, FirstTileColor), Mode.DirectShortTerm);
        }

        // Parallel strokes joined into one polyline tint the whole sheet while keeping the drawing visible through the gaps
        private static Entity CreateTint(double x1, double y1, double x2, double y2, int color, double spacing)
        {
            var tint = new Polyline();
            int index = 0;
            bool toRight = true;
            for (double y = y1; y <= y2; y += spacing)
            {
                tint.AddVertexAt(index++, new Point2d(toRight ? x1 : x2, y), 0, 0, 0);
                tint.AddVertexAt(index++, new Point2d(toRight ? x2 : x1, y), 0, 0, 0);
                toRight = !toRight;
            }
            tint.ColorIndex = color;
            return tint;
        }

        private static double TintSpacing(double tileHeight)
        {
            double pixel = 0;
            try
            {
                double viewSize = Convert.ToDouble(Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("VIEWSIZE"), CultureInfo.InvariantCulture);
                var screen = (Point2d)Autodesk.AutoCAD.ApplicationServices.Core.Application.GetSystemVariable("SCREENSIZE");
                if (viewSize > 0 && screen.Y > 0) pixel = viewSize / screen.Y;
            }
            catch
            {
            }

            double spacing = pixel > 0 ? pixel * 2 : tileHeight / 48;
            return Math.Min(Math.Max(spacing, tileHeight / 160), tileHeight / 8);
        }

        private static Entity CreateFrame(double x1, double y1, double x2, double y2, int color)
        {
            var frame = new Polyline();
            frame.AddVertexAt(0, new Point2d(x1, y1), 0, 0, 0);
            frame.AddVertexAt(1, new Point2d(x2, y1), 0, 0, 0);
            frame.AddVertexAt(2, new Point2d(x2, y2), 0, 0, 0);
            frame.AddVertexAt(3, new Point2d(x1, y2), 0, 0, 0);
            frame.Closed = true;
            frame.ColorIndex = color;
            return frame;
        }

        // Full block glyphs form the plate, because transients do not draw solid fills in 2D Wireframe
        private static Entity CreatePlate(string text, double centerX, double centerY, double height, bool first)
        {
            var plate = new MText();
            plate.Location = new Point3d(centerX, centerY, 0);
            plate.Attachment = AttachmentPoint.MiddleCenter;
            plate.TextHeight = height;
            plate.Contents = "{\\fArial|b0|i0|c0|p34;" + new string('\u2588', text.Length + 1) + "}";
            plate.Color = first ? FirstPlateColor : TilePlateColor;
            return plate;
        }

        private static Entity CreateLabel(string text, double centerX, double centerY, double height)
        {
            var label = new MText();
            label.Location = new Point3d(centerX, centerY, 0);
            label.Attachment = AttachmentPoint.MiddleCenter;
            label.TextHeight = height;
            label.Contents = "{\\fArial|b1|i0|c0|p34;" + text + "}";
            label.Color = Autodesk.AutoCAD.Colors.Color.FromRgb(0, 0, 0);
            return label;
        }

        private static void Add(Entity entity, Mode mode)
        {
            var manager = Autodesk.AutoCAD.GraphicsInterface.TransientManager.CurrentTransientManager;
            manager.AddTransient(entity, mode, SubDrawingMode, new IntegerCollection());
            Items.Add(entity);
        }

        private static void Refresh()
        {
            try
            {
                var doc = AcadApp.DocumentManager.MdiActiveDocument;
                if (doc != null) doc.Editor.UpdateScreen();
            }
            catch
            {
            }
        }
    }
}

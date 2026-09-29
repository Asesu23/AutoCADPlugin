using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SheetAreaSelector
{
    internal static class GridPreview
    {
        private const int SubDrawingMode = 4711;
        private const int MaxDetailedTiles = 300;
        private const int FirstTileColor = 2;
        private const int TileColor = 4;

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
            double textHeight = Math.Min(width, height) * 0.12;
            int number = settings.StartNumber;

            for (int row = 0; row < settings.Rows; row++)
            {
                for (int column = 0; column < settings.Columns; column++)
                {
                    double x = settings.MinX + column * width;
                    double y = settings.MinY - row * height;
                    int color = (row == 0 && column == 0) ? FirstTileColor : TileColor;

                    Add(CreateFrame(x, y, x + width, y + height, color));
                    Add(CreateLabel(number.ToString(CultureInfo.InvariantCulture), x + width / 2, y + height / 2, textHeight, color));
                    number++;
                }
            }
        }

        private static void DrawOutline(ExportSettings settings)
        {
            double width = settings.AreaWidth;
            double height = settings.AreaHeight;

            Add(CreateFrame(settings.MinX, settings.MinY - (settings.Rows - 1) * height,
                settings.MinX + settings.Columns * width, settings.MaxY, TileColor));
            Add(CreateFrame(settings.MinX, settings.MinY, settings.MaxX, settings.MaxY, FirstTileColor));
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

        private static Entity CreateLabel(string text, double centerX, double centerY, double height, int color)
        {
            double approximateWidth = height * 0.65 * text.Length;
            var label = new DBText();
            label.TextString = text;
            label.Height = height;
            label.Position = new Point3d(centerX - approximateWidth / 2, centerY - height / 2, 0);
            label.ColorIndex = color;
            return label;
        }

        private static void Add(Entity entity)
        {
            var manager = Autodesk.AutoCAD.GraphicsInterface.TransientManager.CurrentTransientManager;
            manager.AddTransient(entity, Autodesk.AutoCAD.GraphicsInterface.TransientDrawingMode.DirectShortTerm,
                SubDrawingMode, new IntegerCollection());
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

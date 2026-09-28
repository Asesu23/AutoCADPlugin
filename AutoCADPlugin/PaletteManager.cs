using System;
using Autodesk.AutoCAD.Windows;

namespace SheetAreaSelector
{
    public static class PaletteManager
    {
        public static PaletteSet Ps;

        public static void CreatePalette()
        {
            if (Ps != null) return;
            Ps = new PaletteSet("Sheet Area Selector");
            Ps.Size = new System.Drawing.Size(260, 150);
            Ps.DockEnabled = DockSides.Left | DockSides.Right | DockSides.Top | DockSides.Bottom;
            var ctrl = new SheetAreaControl();
            Ps.Add("SheetArea", ctrl);
            Ps.Visible = false;
        }

        public static void Show()
        {
            if (Ps == null) CreatePalette();
            Ps.Visible = true;
            try { Ps.Activate(0); } catch { }
        }

        public static void Hide()
        {
            if (Ps == null) return;
            Ps.Visible = false;
        }

        public static void Toggle()
        {
            if (Ps == null) CreatePalette();
            Ps.Visible = !Ps.Visible;
            if (Ps.Visible) try { Ps.Activate(0); } catch { }
        }

        public static void Dispose()
        {
            if (Ps == null) return;
            try
            {
                Ps.Visible = false;
                Ps.Dispose();
            }
            catch { }
            Ps = null;
        }
    }
}
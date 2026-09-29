using Microsoft.Win32;
using System;
using System.Globalization;

namespace SheetAreaSelector
{
    internal sealed class ExportSettings
    {
        public const int MaxCount = 200;
        public const int MaxNumber = 100000;

        private const string RegistryPath = @"Software\Jpegger";

        private static ExportSettings _current;

        public static ExportSettings Current
        {
            get { return _current ?? (_current = Load()); }
        }

        public int Columns { get; set; } = 1;
        public int Rows { get; set; } = 1;
        public int StartNumber { get; set; } = 1;
        public string OutputFolder { get; set; } = @"C:\AutocadJpgResult\";
        public bool OpenFolderAfterExport { get; set; } = true;

        public bool HasArea { get; private set; }
        public double MinX { get; private set; }
        public double MinY { get; private set; }
        public double MaxX { get; private set; }
        public double MaxY { get; private set; }
        public string AreaDocument { get; private set; }

        public double AreaWidth => MaxX - MinX;
        public double AreaHeight => MaxY - MinY;
        public bool IsLandscape => AreaWidth > AreaHeight;
        public int TotalCount => Columns * Rows;

        public void SetArea(double minX, double minY, double maxX, double maxY, string documentName)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            AreaDocument = documentName;
            HasArea = true;
        }

        public void ClearArea()
        {
            HasArea = false;
            AreaDocument = null;
        }

        public void Save()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath))
                {
                    if (key == null) return;
                    key.SetValue("Columns", Columns, RegistryValueKind.DWord);
                    key.SetValue("Rows", Rows, RegistryValueKind.DWord);
                    key.SetValue("StartNumber", StartNumber, RegistryValueKind.DWord);
                    key.SetValue("OutputFolder", OutputFolder ?? string.Empty, RegistryValueKind.String);
                    key.SetValue("OpenFolder", OpenFolderAfterExport ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch
            {
            }
        }

        private static ExportSettings Load()
        {
            var settings = new ExportSettings();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath))
                {
                    if (key == null) return settings;

                    settings.Columns = ReadInt(key, "Columns", settings.Columns, 1, MaxCount);
                    settings.Rows = ReadInt(key, "Rows", settings.Rows, 1, MaxCount);
                    settings.StartNumber = ReadInt(key, "StartNumber", settings.StartNumber, 1, MaxNumber);
                    settings.OpenFolderAfterExport = ReadInt(key, "OpenFolder", 1, 0, 1) == 1;

                    string folder = key.GetValue("OutputFolder") as string;
                    if (!string.IsNullOrWhiteSpace(folder))
                        settings.OutputFolder = folder;
                }
            }
            catch
            {
            }
            return settings;
        }

        private static int ReadInt(RegistryKey key, string name, int fallback, int min, int max)
        {
            object raw = key.GetValue(name);
            int value;
            if (raw != null && int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                && value >= min && value <= max)
                return value;
            return fallback;
        }
    }
}

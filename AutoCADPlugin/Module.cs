using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Input;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;

namespace SheetAreaSelector
{
    public class Module : IExtensionApplication
    {
        public void Initialize()
        {
            System.Threading.Tasks.Task.Run(() => CheckForUpdates());
            try
            {
                TryCreateRibbonTab();
            }
            catch (System.Exception ex)
            {
                TryWriteEditor("\nModule.Initialize error: " + ex.Message);
            }

            AcadApp.Idle += OnIdleOnce;
        }

        private void CheckForUpdates()
        {
            try
            {
                string versionUrl = "https://github.com/Asesu23/Jpegger/releases/latest/download/version.txt";
                string setupUrl = "https://github.com/Asesu23/Jpegger/releases/latest/download/Jpegger_Setup.exe";

                using (WebClient client = new WebClient())
                {
                    string latestVersionStr = client.DownloadString(versionUrl).Trim();
                    Version latestVersion = new Version(latestVersionStr);

                    Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    if (latestVersion > currentVersion)
                    {
                        var res = System.Windows.Forms.MessageBox.Show(
                            "Доступна новая версия Jpegger. Установить?",
                            "Jpegger",
                            System.Windows.Forms.MessageBoxButtons.YesNo,
                            System.Windows.Forms.MessageBoxIcon.Question,
                            System.Windows.Forms.MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);

                        if (res == System.Windows.Forms.DialogResult.Yes)
                        {
                            string tempExe = Path.Combine(Path.GetTempPath(), "JpeggerUpdate.exe");
                            client.DownloadFile(setupUrl, tempExe);

                            Process.Start(tempExe);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Update check failed: " + ex.Message);
            }
        }

        private void OnIdleOnce(object sender, EventArgs e)
        {
            AcadApp.Idle -= OnIdleOnce;
            try
            {
                TryCreateRibbonTab();
            }
            catch (System.Exception ex)
            {
                TryWriteEditor("\nOnIdleOnce error: " + ex.Message);
            }
        }

        public void Terminate()
        {
            AcadApp.Idle -= OnIdleOnce;
        }

        private static void TryWriteEditor(string msg)
        {
            try
            {
                AcadApp.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(msg);
            }
            catch { }
        }

        private static void TryCreateRibbonTab()
        {
            try
            {
                var ribbon = ComponentManager.Ribbon;
                if (ribbon == null) return;

                // The built-in Home tab has an id containing "ID_TabHome"
                var homeTab = ribbon.Tabs.FirstOrDefault(t => t.Id.Contains("ID_TabHome"));

                if (homeTab == null) return;

                const string panelTitle = "Jpegger";
                if (homeTab.Panels.Any(p => p.Source.Title == panelTitle)) return;

                var panelSource = new RibbonPanelSource { Title = panelTitle };

                var panel = new RibbonPanel { Source = panelSource };

                var btn = new Autodesk.Windows.RibbonButton
                {
                    Text = "Export",
                    Id = "JpeggerButton",
                    ShowText = true,
                    ShowImage = true,
                    Size = RibbonItemSize.Large,
                    Orientation = System.Windows.Controls.Orientation.Vertical,
                    LargeImage = GetBitmapSource(AutoCADPlugin.Properties.Resources.jpegger),
                    Image = GetBitmapSource(AutoCADPlugin.Properties.Resources.jpegger),
                    ToolTip = new Autodesk.Windows.RibbonToolTip
                    {
                        Title = "Jpegger",
                        Content = "Export drawing sheet areas to JPG images",
                        Command = "JPEGGER"
                    },

                    CommandHandler = new RibbonShowFormCommand()
                };

                panelSource.Items.Add(btn);
                homeTab.Panels.Add(panel);
            }
            catch (System.Exception ex)
            {
                TryWriteEditor("\nError adding to Home tab: " + ex.Message);
            }
        }


        private class RibbonShowFormCommand : ICommand
        {
            private EventHandler _dummy;
            public event EventHandler CanExecuteChanged
            {
                add { _dummy += value; }
                remove { _dummy -= value; }
            }
            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter)
            {
                try
                {
                    var doc = AcadApp.DocumentManager.MdiActiveDocument;
                    if (doc != null)
                        doc.SendStringToExecute("JPEGGER ", true, false, false);
                }
                catch (System.Exception ex)
                {
                    TryWriteEditor("\nRibbon button Execute error: " + ex.Message);
                }
            }
        }

        [CommandMethod("Jpegger")]
        [CommandMethod("SheetArea")]
        public static void Cmd_Jpegger()
        {
            try
            {
                Commands.Run();
            }
            catch (System.Exception ex)
            {
                TryWriteEditor("\nJpegger error: " + ex.Message);
            }
        }

        private static System.Windows.Media.Imaging.BitmapSource GetBitmapSource(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null) return null;
            try
            {
                var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                try
                {
                    var source = System.Windows.Media.Imaging.BitmapSource.Create(
                        data.Width, data.Height, 96, 96,
                        System.Windows.Media.PixelFormats.Pbgra32, null,
                        data.Scan0, data.Stride * data.Height, data.Stride);
                    source.Freeze();
                    return source;
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
            }
            catch
            {
                return null;
            }
        }

    }
}
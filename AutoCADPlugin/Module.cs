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
                // 1. Прямая ссылка на файл с версией (например на GitHub)
                string versionUrl = "https://github.com/Asesu23/AutoCADPlugin/releases/latest/download/version.txt";
                // 2. Прямая ссылка на ваш инсталлятор .exe
                string setupUrl = "https://github.com/Asesu23/AutoCADPlugin/releases/latest/download/AutoCADPlugin_Setup.exe";

                using (WebClient client = new WebClient())
                {
                    // Скачиваем номер версии с сервера
                    string latestVersionStr = client.DownloadString(versionUrl).Trim();
                    Version latestVersion = new Version(latestVersionStr);

                    // Получаем версию текущей запущенной DLL
                    Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                    if (latestVersion > currentVersion)
                    {
                        var res = System.Windows.Forms.MessageBox.Show(
                            "Доступна новая версия плагина. Установить?",
                            "Обновление",
                            System.Windows.Forms.MessageBoxButtons.YesNo,
                            System.Windows.Forms.MessageBoxIcon.Question,
                            System.Windows.Forms.MessageBoxDefaultButton.Button1,
                            System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly);

                        if (res == System.Windows.Forms.DialogResult.Yes)
                        {
                            string tempExe = Path.Combine(Path.GetTempPath(), "PluginUpdate.exe");
                            client.DownloadFile(setupUrl, tempExe);

                            // Запускаем инсталлятор и закрываем AutoCAD
                            Process.Start(tempExe);
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Это покажет окно с текстом ошибки, если обновление не срабатывает
                System.Windows.Forms.MessageBox.Show("Ошибка в модуле обновления: " + ex.Message);
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

                // Ищем вкладку "Home" (Главная). 
                // В AutoCAD её ID обычно содержит "ID_TabHome"
                var homeTab = ribbon.Tabs.FirstOrDefault(t => t.Id.Contains("ID_TabHome"));

                if (homeTab == null) return;

                // Проверяем, не добавлена ли уже наша панель (чтобы не дублировать)
                const string panelTitle = "Jpeger";
                if (homeTab.Panels.Any(p => p.Source.Title == panelTitle)) return;

                // Создаем источник панели
                var panelSource = new RibbonPanelSource { Title = panelTitle };

                // Создаем саму панель
                var panel = new RibbonPanel { Source = panelSource };

                // Создаем кнопку
                var btn = new Autodesk.Windows.RibbonButton
                {
                    Text = "Jpg create",
                    Id = "SheetAreaSelectorBtn",
                    ShowText = false,
                    ShowImage = true,
                    Size = RibbonItemSize.Large,
                    Orientation = System.Windows.Controls.Orientation.Vertical,
                    LargeImage = GetBitmapSource(AutoCADPlugin.Properties.Resources.jpeger),
                    Image = GetBitmapSource(AutoCADPlugin.Properties.Resources.jpeger),

                    CommandHandler = new RibbonShowFormCommand()
                };

                // Добавляем кнопку в панель, а панель во вкладку Home
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
                    SheetAreaForm.ShowModeless();
                }
                catch (System.Exception ex)
                {
                    TryWriteEditor("\nRibbon button Execute error: " + ex.Message);
                }
            }
        }

        [CommandMethod("SheetArea")]
        public static void Cmd_SheetArea()
        {
            try
            {
                SheetAreaForm.ShowModeless();
            }
            catch (System.Exception ex)
            {
                TryWriteEditor("\nCmd_SheetArea error: " + ex.Message);
            }
        }
        private static System.Windows.Media.Imaging.BitmapSource GetBitmapSource(System.Drawing.Bitmap bitmap)
        {
            if (bitmap == null) return null;
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap.GetHbitmap(),
                    IntPtr.Zero,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            }
            catch
            {
                return null;
            }
        }

    }
}
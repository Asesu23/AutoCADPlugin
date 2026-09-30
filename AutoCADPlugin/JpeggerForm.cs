using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace SheetAreaSelector
{
    internal sealed class JpeggerForm : Form
    {
        private readonly ExportSettings _settings;
        private readonly ToolTip _toolTip = new ToolTip();
        private readonly Font _titleFont;
        private readonly Font _primaryFont;

        private Button _pickButton;
        private Label _areaLabel;
        private NumericUpDown _columns;
        private NumericUpDown _rows;
        private NumericUpDown _start;
        private Label _summaryLabel;
        private TextBox _folderBox;
        private CheckBox _openFolderBox;
        private Button _exportButton;
        private bool _loading;

        public event EventHandler PreviewChanged;
        public event EventHandler PickAreaRequested;
        public event EventHandler ExportRequested;

        public JpeggerForm(ExportSettings settings)
        {
            _settings = settings;
            Font = SystemFonts.MessageBoxFont;
            _titleFont = new Font(Font.FontFamily, 13F, FontStyle.Bold);
            _primaryFont = new Font(Font, FontStyle.Bold);

            BuildLayout();
            LoadValues();
            UpdateState();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Rectangle area = Screen.FromPoint(MousePosition).WorkingArea;
            Location = new Point(Math.Max(area.Left, area.Right - Width - 40), area.Top + 120);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _toolTip.Dispose();
                _titleFont.Dispose();
                _primaryFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private void BuildLayout()
        {
            SuspendLayout();

            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(440, 64),
                BackColor = Color.FromArgb(59, 68, 83)
            };
            header.Controls.Add(new PictureBox
            {
                Location = new Point(16, 16),
                Size = new Size(32, 32),
                SizeMode = PictureBoxSizeMode.CenterImage,
                Image = AutoCADPlugin.Properties.Resources.jpegger
            });
            header.Controls.Add(new Label
            {
                Text = "Jpegger",
                Font = _titleFont,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(60, 9)
            });
            header.Controls.Add(new Label
            {
                Text = "Экспорт областей листа в JPG",
                ForeColor = Color.FromArgb(176, 184, 196),
                AutoSize = true,
                Location = new Point(62, 37)
            });

            var areaGroup = new GroupBox
            {
                Text = "Область",
                Location = new Point(12, 76),
                Size = new Size(416, 68)
            };
            _pickButton = new Button
            {
                Text = "Указать область <",
                Location = new Point(12, 24),
                Size = new Size(160, 30)
            };
            _pickButton.Click += (s, e) => Raise(PickAreaRequested);
            _areaLabel = new Label
            {
                Location = new Point(184, 24),
                Size = new Size(222, 30),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            areaGroup.Controls.Add(_pickButton);
            areaGroup.Controls.Add(_areaLabel);
            _toolTip.SetToolTip(_pickButton, "Окно скроется: укажите два противоположных угла первой области");

            var gridGroup = new GroupBox
            {
                Text = "Сетка экспорта",
                Location = new Point(12, 152),
                Size = new Size(416, 108)
            };
            _columns = CreateNumber(104, 24, 64, 1, ExportSettings.MaxCount);
            _rows = CreateNumber(280, 24, 64, 1, ExportSettings.MaxCount);
            _start = CreateNumber(104, 54, 80, 1, ExportSettings.MaxNumber);
            _summaryLabel = new Label
            {
                Location = new Point(12, 84),
                Size = new Size(392, 18),
                AutoEllipsis = true
            };
            gridGroup.Controls.Add(CreateCaption("Колонок (X):", 12, 24, 88));
            gridGroup.Controls.Add(_columns);
            gridGroup.Controls.Add(CreateCaption("Рядов (Y):", 200, 24, 76));
            gridGroup.Controls.Add(_rows);
            gridGroup.Controls.Add(CreateCaption("Начать с №:", 12, 54, 88));
            gridGroup.Controls.Add(_start);
            gridGroup.Controls.Add(_summaryLabel);
            _toolTip.SetToolTip(_columns, "Сколько областей выгрузить вправо (по оси X)");
            _toolTip.SetToolTip(_rows, "Сколько рядов выгрузить вниз (по оси Y)");
            _toolTip.SetToolTip(_start, "Номер первого файла: 1.jpg, 2.jpg и так далее");

            var saveGroup = new GroupBox
            {
                Text = "Сохранение",
                Location = new Point(12, 268),
                Size = new Size(416, 88)
            };
            _folderBox = new TextBox
            {
                Location = new Point(12, 24),
                Size = new Size(356, 23)
            };
            _folderBox.TextChanged += (s, e) =>
            {
                if (!_loading) _settings.OutputFolder = _folderBox.Text;
            };
            var browseButton = new Button
            {
                Text = "...",
                Location = new Point(376, 23),
                Size = new Size(28, 25)
            };
            browseButton.Click += OnBrowseClick;
            _openFolderBox = new CheckBox
            {
                Text = "Открыть папку после экспорта",
                Location = new Point(12, 56),
                Size = new Size(392, 22)
            };
            _openFolderBox.CheckedChanged += (s, e) =>
            {
                if (!_loading) _settings.OpenFolderAfterExport = _openFolderBox.Checked;
            };
            saveGroup.Controls.Add(_folderBox);
            saveGroup.Controls.Add(browseButton);
            saveGroup.Controls.Add(_openFolderBox);
            _toolTip.SetToolTip(browseButton, "Выбрать папку");

            var separator = new Label
            {
                Location = new Point(0, 366),
                Size = new Size(440, 2),
                BorderStyle = BorderStyle.Fixed3D
            };

            _exportButton = new Button
            {
                Text = "Экспорт",
                Location = new Point(200, 378),
                Size = new Size(130, 34),
                FlatStyle = FlatStyle.Flat,
                Font = _primaryFont
            };
            _exportButton.FlatAppearance.BorderSize = 0;
            _exportButton.EnabledChanged += (s, e) => StyleExportButton();
            _exportButton.Click += OnExportClick;

            var closeButton = new Button
            {
                Text = "Закрыть",
                Location = new Point(338, 378),
                Size = new Size(90, 34)
            };
            closeButton.Click += (s, e) => Close();

            Controls.Add(header);
            Controls.Add(areaGroup);
            Controls.Add(gridGroup);
            Controls.Add(saveGroup);
            Controls.Add(separator);
            Controls.Add(_exportButton);
            Controls.Add(closeButton);

            Text = "Jpegger";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.Manual;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            CancelButton = closeButton;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(440, 428);

            Shown += (s, e) => ActiveControl = _settings.HasArea ? (Control)_exportButton : _pickButton;

            ResumeLayout(false);
        }

        private NumericUpDown CreateNumber(int x, int y, int width, int min, int max)
        {
            var box = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, 23),
                Minimum = min,
                Maximum = max,
                TextAlign = HorizontalAlignment.Right
            };
            box.ValueChanged += OnValuesChanged;
            box.TextChanged += OnValuesChanged;
            return box;
        }

        private static Label CreateCaption(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 23),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private void LoadValues()
        {
            _loading = true;
            _columns.Value = Clamp(_settings.Columns, (int)_columns.Minimum, (int)_columns.Maximum);
            _rows.Value = Clamp(_settings.Rows, (int)_rows.Minimum, (int)_rows.Maximum);
            _start.Value = Clamp(_settings.StartNumber, (int)_start.Minimum, (int)_start.Maximum);
            _folderBox.Text = _settings.OutputFolder;
            _openFolderBox.Checked = _settings.OpenFolderAfterExport;
            _loading = false;
        }

        private void OnValuesChanged(object sender, EventArgs e)
        {
            if (_loading) return;

            int columns = ReadNumber(_columns);
            int rows = ReadNumber(_rows);
            int start = ReadNumber(_start);
            if (columns == _settings.Columns && rows == _settings.Rows && start == _settings.StartNumber) return;

            _settings.Columns = columns;
            _settings.Rows = rows;
            _settings.StartNumber = start;
            UpdateState();

            Raise(PreviewChanged);
        }

        public void RefreshState()
        {
            UpdateState();
        }

        private void Raise(EventHandler handler)
        {
            if (handler != null) handler(this, EventArgs.Empty);
        }

        private void UpdateState()
        {
            bool hasArea = _settings.HasArea;
            if (hasArea)
            {
                _areaLabel.Text = string.Format(CultureInfo.CurrentCulture, "Область: {0:0.##} x {1:0.##} ({2})",
                    _settings.AreaWidth, _settings.AreaHeight, _settings.IsLandscape ? "альбомная" : "книжная");
                _areaLabel.ForeColor = Color.FromArgb(30, 120, 60);
                _pickButton.Text = "Указать заново <";
            }
            else
            {
                _areaLabel.Text = "Область не выбрана";
                _areaLabel.ForeColor = Color.FromArgb(180, 60, 50);
                _pickButton.Text = "Указать область <";
            }

            int total = _settings.TotalCount;
            int first = _settings.StartNumber;
            _summaryLabel.Text = total == 1
                ? string.Format(CultureInfo.CurrentCulture, "Будет создан 1 файл ({0}.jpg)", first)
                : string.Format(CultureInfo.CurrentCulture, "Будет создано файлов: {0} ({1}.jpg ... {2}.jpg)", total, first, first + total - 1);

            _exportButton.Enabled = hasArea;
            StyleExportButton();
        }

        private void StyleExportButton()
        {
            if (_exportButton.Enabled)
            {
                _exportButton.BackColor = Color.FromArgb(0, 120, 215);
                _exportButton.ForeColor = Color.White;
            }
            else
            {
                _exportButton.BackColor = Color.FromArgb(214, 214, 214);
                _exportButton.ForeColor = Color.FromArgb(128, 128, 128);
            }
        }

        private void OnBrowseClick(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Папка для сохранения JPG";
                string current = _folderBox.Text.Trim();
                if (Directory.Exists(current)) dialog.SelectedPath = current;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                    _folderBox.Text = dialog.SelectedPath;
            }
        }

        private void OnExportClick(object sender, EventArgs e)
        {
            string folder = _folderBox.Text.Trim();
            string error;
            if (!TryPrepareFolder(folder, out error))
            {
                MessageBox.Show(this, error, "Jpegger", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _folderBox.Focus();
                return;
            }

            _settings.OutputFolder = folder;
            Raise(ExportRequested);
        }

        private static bool TryPrepareFolder(string folder, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(folder))
            {
                error = "Укажите папку для сохранения.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(folder);
                return true;
            }
            catch (Exception ex)
            {
                error = "Не удалось создать папку:\n" + ex.Message;
                return false;
            }
        }

        private static int ReadNumber(NumericUpDown box)
        {
            int parsed;
            if (int.TryParse(box.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
                return Clamp(parsed, (int)box.Minimum, (int)box.Maximum);
            return (int)box.Value;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}

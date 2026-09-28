using System;
using System.Drawing;
using System.Windows.Forms;

namespace SheetAreaSelector
{
    public class SheetAreaForm : Form
    {
        private static SheetAreaForm _instance;
        private Button buttonSelect;
        private Button buttonClose;
        private Button buttonSelectFolder;
        private Label labelFolderPath;

        // Поля ввода
        private Label lblRowX;
        private TextBox txtRowX;
        private Label lblColY;
        private TextBox txtColY;
        private Label lblStartFrom;
        private TextBox txtStartFrom;

        public SheetAreaForm()
        {
            InitializeComponent();
            UpdateFolderLabel();
        }

        private void InitializeComponent()
        {
            this.buttonSelect = new Button();
            this.buttonClose = new Button();
            this.buttonSelectFolder = new Button();
            this.labelFolderPath = new Label();

            this.lblRowX = new Label();
            this.txtRowX = new TextBox();
            this.lblColY = new Label();
            this.txtColY = new TextBox();
            this.lblStartFrom = new Label();
            this.txtStartFrom = new TextBox();

            this.SuspendLayout();

            // Кнопка выбора области
            this.buttonSelect.Location = new Point(12, 12);
            this.buttonSelect.Size = new Size(260, 40);
            this.buttonSelect.Text = "Выбрать область";
            this.buttonSelect.Click += new EventHandler(this.buttonSelect_Click);

            // Поле Строка(X)
            this.lblRowX.Location = new Point(12, 60);
            this.lblRowX.Size = new Size(80, 15);
            this.lblRowX.Text = "Строка(X):";
            this.txtRowX.Location = new Point(12, 78);
            this.txtRowX.Size = new Size(80, 20);

            // Поле Столбец(Y)
            this.lblColY.Location = new Point(102, 60);
            this.lblColY.Size = new Size(80, 15);
            this.lblColY.Text = "Столбец(Y):";
            this.txtColY.Location = new Point(102, 78);
            this.txtColY.Size = new Size(80, 20);

            // Поле С какой начинать
            this.lblStartFrom.Location = new Point(192, 60);
            this.lblStartFrom.Size = new Size(80, 15);
            this.lblStartFrom.Text = "Начать с:";
            this.txtStartFrom.Location = new Point(192, 78);
            this.txtStartFrom.Size = new Size(80, 20);

            // Кнопка выбора папки
            this.buttonSelectFolder.Location = new Point(12, 110);
            this.buttonSelectFolder.Size = new Size(260, 30);
            this.buttonSelectFolder.Text = "Выбрать папку сохранения";
            this.buttonSelectFolder.Click += new EventHandler(this.buttonSelectFolder_Click);

            // Путь
            this.labelFolderPath.Location = new Point(12, 145);
            this.labelFolderPath.Size = new Size(260, 40);
            this.labelFolderPath.BorderStyle = BorderStyle.FixedSingle;
            this.labelFolderPath.BackColor = SystemColors.Window;

            // Кнопка закрытия
            this.buttonClose.Location = new Point(12, 195);
            this.buttonClose.Size = new Size(260, 30);
            this.buttonClose.Text = "Закрыть";
            this.buttonClose.Click += new EventHandler(this.buttonClose_Click);

            // Форма
            this.ClientSize = new Size(284, 235);
            this.Controls.AddRange(new Control[] {
                buttonSelect, lblRowX, txtRowX, lblColY, txtColY,
                lblStartFrom, txtStartFrom, buttonSelectFolder, labelFolderPath, buttonClose
            });

            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Sheet Area";
            this.TopMost = true;

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Записываем значения из полей формы в класс Commands
                Commands.RowX = txtRowX.Text;
                Commands.ColY = txtColY.Text;
                Commands.StartFrom = txtStartFrom.Text;

                // 2. Скрываем форму и запускаем логику
                this.Hide();
                Commands.RunSelectionAndSave();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                try { this.Show(); this.Activate(); } catch { }
            }
        }


        private void buttonSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = Commands.CustomFolderPath;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    Commands.CustomFolderPath = folderDialog.SelectedPath;
                    UpdateFolderLabel();
                }
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
            _instance = null;
        }

        private void UpdateFolderLabel()
        {
            if (labelFolderPath != null)
            {
                string path = Commands.CustomFolderPath;
                labelFolderPath.Text = path.Length > 45 ? $"...{path.Substring(path.Length - 45)}" : path;
            }
        }

        public static void ShowModeless()
        {
            if (_instance == null || _instance.IsDisposed) _instance = new SheetAreaForm();
            if (!_instance.Visible) _instance.Show();
            else _instance.Activate();
            _instance.UpdateFolderLabel();
        }
    }
}

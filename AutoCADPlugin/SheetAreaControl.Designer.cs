namespace SheetAreaSelector
{
    partial class SheetAreaControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button buttonSelect;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.buttonSelect = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.buttonSelect.Location = new System.Drawing.Point(12, 12);
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.Size = new System.Drawing.Size(236, 56);
            this.buttonSelect.TabIndex = 0;
            this.buttonSelect.Text = "Выбрать область";
            this.buttonSelect.UseVisualStyleBackColor = true;
            this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.buttonSelect);
            this.Name = "SheetAreaControl";
            this.Size = new System.Drawing.Size(260, 80);
            this.ResumeLayout(false);
        }
    }
}

using System;
using System.Windows.Forms;

namespace SheetAreaSelector
{
    public partial class SheetAreaControl : UserControl
    {
        public SheetAreaControl()
        {
            InitializeComponent();
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            PaletteManager.Hide();
            Commands.RunSelectionAndSave();
            PaletteManager.Show();
        }
    }
}

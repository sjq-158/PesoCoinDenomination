using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace PesoCoinDenomination
{
    public partial class Form1 : Form
    {
        Bitmap loaded, processed;

        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            loaded = new Bitmap(openFileDialog1.FileName);
            pictureBox1.Image = loaded;
            processed = null;
            txtInfo.Text = "Image loaded." + Environment.NewLine + "Go to DIP > Count Coins.";
        }

        private void countCoinsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (loaded == null)
            {
                MessageBox.Show("Open an image first (File > Open).");
                return;
            }

            CoinResult result = CoinCounter.process(loaded);

            processed = result.annotated;
            pictureBox1.Image = processed;
            txtInfo.Text = result.details;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (processed == null)
            {
                MessageBox.Show("Nothing to save yet. Run Count Coins first.");
                return;
            }
            saveFileDialog1.ShowDialog();
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            processed.Save(saveFileDialog1.FileName);
        }
    }
}
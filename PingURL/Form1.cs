using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingURL
{
    public partial class Form1 : Form
    {
        private string[] listURL;
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            openFileDialog1 = new OpenFileDialog()
            {
                FileName = "Select a text file",
                Filter = "Text files (*.txt)|*.txt",
                Title = "Open text file"
            };
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var filePath = openFileDialog1.FileName;
                    lbFile.Text = filePath;
                    using (Stream str = openFileDialog1.OpenFile())
                    {
                        listURL = File.ReadAllLines(filePath);
                    }
                    MessageBox.Show("Read successful");
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            foreach (var item in listURL)
            {
                string result = await PingAsync(item) ? "OK" : "Not Found";
                dtgvResult.Rows.Add(item, result, DateTime.Now.ToString());
            }
        }
        private static async Task<bool> PingAsync(string url)
        {
            Ping ping = new Ping();
            PingReply result = await ping.SendPingAsync(url);
            return result.Status == IPStatus.Success;
        }
    }
}

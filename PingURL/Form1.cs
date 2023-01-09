using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PingURL;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace PingURL
{
    public struct URLInfo
    {
        public string url { get; set; }
        public string statusCode { get; set; }
    }

    public partial class Form1 : Form
    {
        private string filePath = string.Empty;
        private List<string> listURL;
        private int numOfThreads = 4;
        private HttpClient client = new HttpClient();
        private bool allDone = false;

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
                    filePath = openFileDialog1.FileName;
                    lbFile.Text = filePath;
                    using (Stream str = openFileDialog1.OpenFile())
                    {
                        string[] results = File.ReadAllLines(filePath);
                        listURL = new List<string>(results);
                    }
                    MessageBox.Show("Read successful.");
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (filePath == string.Empty)
            {
                MessageBox.Show("File is empty!!!");
                return;
            }
            var stopwatch = Stopwatch.StartNew();
            btnStart.Enabled = false;
            dtgvResult.Rows.Clear();
            dtgvResult.Refresh();
            for (int i = 0; i < numOfThreads; i++)
            {
                Thread t = new Thread(async () =>
                {
                    while (listURL.Count() > 0)
                    {
                        URLInfo urlInfo = await ScanURL();
                        Action action = () => {
                            dtgvResult.Rows.Add(urlInfo.url, urlInfo.statusCode, DateTime.Now.ToString());
                            dtgvResult.FirstDisplayedScrollingRowIndex = dtgvResult.RowCount - 1;
                        };
                        this.BeginInvoke(action);

                    }
                    if (!allDone)
                    {
                        allDone = true;
                        stopwatch.Stop();
                        filePath = string.Empty;
                        Action action = () => {
                            btnStart.Enabled = true;
                            lbFile.Text= string.Empty;
                        };
                        this.BeginInvoke(action);
                        MessageBox.Show($"Completed. Spent time: {stopwatch.Elapsed.TotalSeconds}s.");
                    }
                });
                t.Start();
            }
                
        }

        private async Task<URLInfo> ScanURL()
        {
            string url = listURL.FirstOrDefault();
            listURL.RemoveAt(0);
            string statusCode = await GetStatusCode(url);
            URLInfo urlInfo = new URLInfo();
            urlInfo.url = url;
            urlInfo.statusCode = statusCode;
            return urlInfo;
        }

        private async Task<string> GetStatusCode(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);
            return response.StatusCode.ToString();
        }
    }
}

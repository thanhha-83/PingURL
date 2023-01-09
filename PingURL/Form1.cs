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

    public partial class Form1 : Form
    {
        private string filePath = string.Empty;
        private static IEnumerable<string> listURL;
        private CancellationTokenSource s_cts = new CancellationTokenSource();

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
                        listURL = results;
                    }
                    dtgvResult.Rows.Clear();
                    dtgvResult.Refresh();
                    foreach (string url in listURL)
                    {
                        dtgvResult.Rows.Add(url, string.Empty);
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
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            Task mainTask = Task.Run(() =>
            {
                int i = 0;
                foreach (string url in listURL)
                {
                    Task task = PingAddressAsync(url, 5000, s_cts.Token, dtgvResult.Rows[i].Cells[1]);
                    i++;
                }
            });
        }

        public async Task PingAddressAsync(string address, int timeout, CancellationToken token, DataGridViewCell resultCell = null)
        {
            Ping pingClass = new Ping();
            token.Register(() => pingClass.SendAsyncCancel());
            while (true)
            {
                if (token.IsCancellationRequested) { break; }
                try
                {
                    PingReply pingReply = await pingClass.SendPingAsync(address, timeout);
                    if (resultCell != null)
                    {
                        Action action = () =>
                        {
                            resultCell.Value = pingReply.RoundtripTime.ToString();
                        };
                        this.BeginInvoke(action);
                        Console.WriteLine(address + " " + pingReply.RoundtripTime.ToString());
                    }
                }
                catch (Exception e)
                {
                    Action action = () =>
                    {
                        resultCell.Value = e.Message;
                    };
                    this.BeginInvoke(action);
                    Console.WriteLine(e.Message);
                }
                await Task.Delay(1000);
            }
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            await handleCancelPing();
            btnStop.Enabled = false;
            btnStart.Enabled = true;
        }

        private async void btnClear_Click(object sender, EventArgs e)
        {
            await handleCancelPing();
            dtgvResult.Rows.Clear();
            dtgvResult.Refresh();
            filePath = string.Empty;
            lbFile.Text = string.Empty;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        public async Task handleCancelPing()
        {
            if (s_cts != null)
            {
                s_cts.Cancel();
            }
            if (listURL != null && listURL.Count() > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (string url in listURL)
                {
                    var task = Task.Run(async () =>
                    {
                        await PingAddressAsync(url, 5000, s_cts.Token);
                    });
                    tasks.Add(task);

                }
                await Task.WhenAll(tasks.ToArray());
                s_cts.Dispose();
                s_cts = new CancellationTokenSource();
            }
            Console.WriteLine("Has cancelled all!");
        }
    }
}

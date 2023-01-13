using PingURL;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace PingURL
{

    public partial class MainForm : Form
    {
        private CancellationTokenSource cts = new CancellationTokenSource();

        Task pingTask = null;

        public MainForm()
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
                        string[] results = File.ReadAllLines(filePath);

                        dtgvResult.Rows.Clear();
                        dtgvResult.Refresh();

                        foreach (string url in results)
                        {
                            dtgvResult.Rows.Add(url, string.Empty);
                        }
                        MessageBox.Show("Read successful.");
                    }
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
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            btnClear.Enabled = false;

            cts = new CancellationTokenSource();
            pingTask = PingTask(cts.Token);
        }

        private async void btnStop_Click(object sender, EventArgs e)
        {
            await StopPinging();

            Console.WriteLine("Has cancelled all!");


            btnStop.Enabled = false;
            btnStart.Enabled = true;
            btnClear.Enabled = true;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {

            dtgvResult.Rows.Clear();
            dtgvResult.Refresh();
            lbFile.Text = string.Empty;
            btnStart.Enabled = true;
            btnClear.Enabled = false;
            btnStop.Enabled = false;
        }

        public async Task PingTask(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Task> pingTasks = new List<Task>();

            foreach (DataGridViewRow row in dtgvResult.Rows)
            {
                // Header row and new row
                if (row.Index < 0 || row.Index >= dtgvResult.RowCount - 1)
                    continue;

                var urlCell = row.Cells[dgvCellHostUrl.Index];
                var pingCell = row.Cells[dgvCellPing.Index];

                var progress = new Progress<UrlPinger.PingProgressParams>();
                progress.ProgressChanged += (sender, e) =>
                {
                    pingCell.Value = e.Ping;
                };

                pingTasks.Add(UrlPinger.PingAddressAsync(urlCell.Value.ToString(), 300, progress, cancellationToken));
            }

            // Wait until no tasks left
            while (pingTasks.Count > 0)
            {
                // Wait for any task to complete
                try
                {
                    await Task.WhenAny(pingTasks);
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + ex.StackTrace);
                }

                // Remove completed task
                for (int i = 0; i < pingTasks.Count; ++i)
                {
                    if (pingTasks[i].IsCompleted)
                    {
                        pingTasks.RemoveAt(i);
                        --i;
                    }
                }
            }
        }

        private async void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            await StopPinging();
        }

        // No exceptions throw guaranteed
        async Task StopPinging()
        {
            if (cts != null)
            {
                cts.Cancel();
            }

            if (pingTask != null)
            {
                try
                {
                    await pingTask;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message + ex.StackTrace);
                }
            }

            cts = null;
        }
    }
}

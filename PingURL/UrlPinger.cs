using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PingURL
{
    public static class UrlPinger
    {
        public class PingProgressParams
        {
            public string Url { get; set; }

            // In millisecond
            public long Ping { get; set; }
        }

        // minInterval is the minimum millisecond required to ping once
        public static async Task PingAddressAsync(string address, long minInterval, IProgress<PingProgressParams> progress, CancellationToken cancellationToken)
        {
            var progressParams = new PingProgressParams();
            progressParams.Url = address;

            Ping pinger = new Ping();

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    pinger.SendAsyncCancel();
                    pinger.Dispose();
                    break;
                }

                PingReply pingReply = await pinger.SendPingAsync(address);
                progressParams.Ping = pingReply.RoundtripTime;
                progress.Report(progressParams);

                try
                {
                    // Only ping once a second
                    await Task.Delay(TimeSpan.FromMilliseconds(minInterval - progressParams.Ping), cancellationToken);
                }
                catch (OperationCanceledException)
                {

                }
            }
        }
    }
}

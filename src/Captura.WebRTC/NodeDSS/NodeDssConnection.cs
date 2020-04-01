using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{
    public class NodeDssConnection : IDisposable
    {
        private string serverAddress;
        private int pollIntervalMs;
        private string path = "";

        public Action<string> MessageReceived;

        private CancellationTokenSource shutdown = new CancellationTokenSource();

        public NodeDssConnection(string serverAddress, string path, int pollIntervalMs = 500)
        {
            if (string.IsNullOrEmpty(serverAddress))
            {
                throw new InvalidOperationException("");
            }
            if (pollIntervalMs <= 0)
            {
                throw new InvalidOperationException("");
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new InvalidOperationException("");
            }

            this.serverAddress = serverAddress;
            if (!this.serverAddress.EndsWith("/"))
            {
                this.serverAddress += "/";
            }

            this.pollIntervalMs = pollIntervalMs;
            this.path = path;
        }

        public void Start()
        {
            var _ = RunAsync();
        }
        
        private async Task RunAsync()
        {
            while (!shutdown.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(pollIntervalMs, shutdown.Token);

                    var message = await NetworkUtil.GetAsync($"{serverAddress}{path}");

                    MessageReceived?.Invoke(message);
                }
                catch(TaskCanceledException)
                {
                    // valid
                }
                catch (WebException e)
                {
                    if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        // ignore, Node-DSS is super spammy
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception e)
                { 
                    Util.WriteLine($"Network error trying to send data to {serverAddress}: {e}");
                }
            }
        }

        public void Dispose()
        {
            MessageReceived = null;

            shutdown.Cancel();
        }
    }
}

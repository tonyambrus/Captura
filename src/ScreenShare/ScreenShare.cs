using Newtonsoft.Json;
using QRCoder;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenShare
{
    public class ScreenShare : IDisposable
    {
        private string channel = "screenShare";// Guid.NewGuid().ToString();
        private string channelKey = "screenShare";
        private string serverAddress = "http://192.168.1.158:3000/"; //"http://node-swc.azurewebsites.net/";
        private string mediaServerPath = @"D:\Code\Experiments\mediaserver";
        private string streamName = "screenShare";
        private Process mediaServerProcess = null;
        private Job job;
        private ScreenShareView view;
        private CancellationTokenSource shutdown;

        public string ChannelAddress => $"{serverAddress}channel/{channel}/";
        public string StreamName => streamName;
        public bool Connected { get; private set; }

        public ScreenShare()
        {
            view = new ScreenShareView();
            //view.Closed += (s, e) => Dispose();
            view.InitializeComponent();

            Connected = false;
        }

        public async Task Start(CancellationToken token)
        {
            shutdown = CancellationTokenSource.CreateLinkedTokenSource(token);

            await RemoveChannelAsync();
            RemoveChannelOnShutdownAsync();
            StartMediaServer();

            if (await WaitForChannelSetupAsync())
            {
                // starts captura with local param
                ShowQRCode();
                Connected = true;
            }
        }

        private void StartMediaServer()
        {         
            // starts mediaServer with server param url
            var psi = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = "/c \"C:\\Program Files\\nodejs\\npm.cmd\" start",
                WorkingDirectory = mediaServerPath,
                UseShellExecute = false
            };

            psi.Environment["SIGNALLINGPROXYSERVER_CHANNEL"] = channel;
            psi.Environment["SIGNALLINGPROXYSERVER_CHANNELKEY"] = channelKey;
            psi.Environment["SIGNALLINGPROXYSERVER_ADDRESS"] = serverAddress;
            
            mediaServerProcess = Process.Start(psi);
            
            // kill child process on close
            job = new Job();
            job.AddProcess(mediaServerProcess.Id);
        }

        private async void RemoveChannelOnShutdownAsync()
        {
            try
            {
                await Task.Delay(-1, shutdown.Token);
            }
            catch (TaskCanceledException)
            {
                // no-op
            }

            await RemoveChannelAsync();
        }

        private async Task RemoveChannelAsync()
        {
            try
            {
                await NetworkUtil.GetAsync($"{serverAddress}remove/{channel}?key={channelKey}", shutdown.Token);
            }
            catch
            {
                // no-op
            }
        }


        private async Task<bool> WaitForChannelSetupAsync()
        {
           // await Task.Delay(5000); // HACK: wait for media server to start

            while (!shutdown.IsCancellationRequested)
            {
                try
                {
                    await NetworkUtil.GetAsync($"{serverAddress}list/{channel}/private?key={channelKey}", shutdown.Token);
                    return true;
                }
                catch (Exception)
                {
                    // 404 keep waiting
                    await Task.Delay(500);
                }
            }

            return false;
        }

        private void ShowQRCode()
        {
            var url = $"{serverAddress}channel/{channel}/#{streamName}";

            // shows QR code [server, broadcast url]
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            view.SetImage(qrCodeImage.ToBitmapImage());
            view.Show();
        }

        public void Dispose()
        {
            shutdown.Cancel();

            if (view != null)
            {
                view.Dispatcher.Invoke(() => view.Hide());
            }

            job?.Dispose();

            Connected = false;
        }
    }
}

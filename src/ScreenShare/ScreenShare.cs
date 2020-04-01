using Newtonsoft.Json;
using QRCoder;
using System;
using System.Diagnostics;

namespace ScreenShare
{
    public class ScreenShare : IDisposable
    {
        private string channel = "screenShare";// Guid.NewGuid().ToString();
        private string channelKey = "screenShare";
        private string serverAddress = "http://192.168.1.158:3000/"; //"http://node-swc.azurewebsites.net/";
        private string mediaServerPath = @"D:\Code\Experiments\mediaserver";
        private string streamName = "ScreenShare";
        private Process mediaServerProcess;
        private ScreenShareView view;

        public ScreenShare()
        {
            view = new ScreenShareView();
            view.Closed += (s, e) => Dispose();
            view.InitializeComponent();

            Run();
        }

        public void Run()
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
            //mediaServerProcess = Process.Start(psi);

            // starts captura with local param
            ShowQRCode();
        }

        private void ShowQRCode()
        {
            var json = JsonConvert.SerializeObject(new
            {
                nodeDssServerUrl = $"{serverAddress}/channel/{channel}/",
                channel = channel,
                streamName
            });

            // shows QR code [server, broadcast url]
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(json, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);

            view.SetImage(qrCodeImage.ToBitmapImage());
            view.Show();
        }

        public async void Dispose()
        {
            view?.Hide();
            view = null;

            mediaServerProcess?.Kill();
            mediaServerProcess?.Dispose();
        }
    }
}

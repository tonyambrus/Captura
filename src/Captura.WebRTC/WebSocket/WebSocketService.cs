using System;
using System.Net;
using WebSocketSharp.Server;

namespace Captura.Models.WebRTC
{
    public class WebSocketService : IDisposable
    {
        private WebSocketServer webSocketServer;

        public WebSocketService(WebRTCHost webrtc, string path, int port, string certPath = null, bool secure = false)
        {
            Util.WriteLine("Starting web socket server...");
            webSocketServer = new WebSocketServer(IPAddress.Any, port, secure);
            if (secure)
            {
                webSocketServer.SslConfiguration.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath);
                webSocketServer.SslConfiguration.CheckCertificateRevocation = false;
            }

            webSocketServer.AddWebSocketService(path, () => new WebSocketSignaler(new WebRTCSession(webrtc)));
            webSocketServer.Start();
        }

        public void Dispose()
        {
            webSocketServer?.Stop();
            webSocketServer = null;
        }
    }
}

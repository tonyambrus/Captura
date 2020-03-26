using System;
using System.Diagnostics;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Captura.Models.WebRTC
{
    public class WebSocketService : IDisposable
    {
        private static WebSocketServer webSocketServer;

        public event Action Opened;
        public event Action<CloseEventArgs> Closed;
        public event Action<WebSocket, ErrorEventArgs> Error;

        public WebSocketService(Func<WebSocketService, WebSocketSession> factory, int port, string certPath = null, bool secure = false)
        {
            // Start web socket server.
            Debug.WriteLine("Starting web socket server...");
            webSocketServer = new WebSocketServer(IPAddress.Any, port, secure);
            if (secure)
            {
                webSocketServer.SslConfiguration.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath);
                webSocketServer.SslConfiguration.CheckCertificateRevocation = false;
            }

            webSocketServer.AddWebSocketService("/", () => factory(this));
            webSocketServer.Start();
        }

        public void Dispose()
        {
            webSocketServer?.Stop();
            webSocketServer = null;
        }
    }
}

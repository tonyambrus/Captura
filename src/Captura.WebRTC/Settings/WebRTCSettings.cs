using System.Linq;
using System.Net;

namespace Captura
{
    public enum WebRTCEndpoint
    {
        WebSocket,
        MediaServer
    }

    public class WebRTCSettings : PropertyStore
    {
        public string IP
        {
            get => Dns
                .GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Where(a => !a.ToString().StartsWith("169.")) // exclude local
                .FirstOrDefault()
                ?.ToString()
                ?? "127.0.0.1";
        }

        public WebRTCEndpoint Mode
        {
            get => Get(WebRTCEndpoint.MediaServer);
            set => Set(value);
        }

        public string MediaServerUrl
        {
            get => Get($"http://{IP}:3000/");
            set => Set(value);
        }

        public string MediaServerStreamName
        {
            get => Get("Captura");
            set => Set(value);
        }

        public int WebSocketPort
        {
            get => Get(8090);
            set => Set(value);
        }

        public string WebSocketPath
        {
            get => Get("/");
            set
            {
                var path = value;
                path = path.Replace('\\', '/');
                path = path.StartsWith("/") ? path : ("/" + path);
                Set(path);
            }
        }
    }
}
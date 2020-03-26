using System.Linq;
using System.Net;

namespace Captura
{
    public class WebRTCSettings : PropertyStore
    {
        public bool Secure
        {
            get => Get(false);
            set => Set(value);
        }

        public string IP
        {
            get => Dns
                .GetHostEntry(Dns.GetHostName())
                .AddressList
                .FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ?.ToString()
                ?? "127.0.0.1";
        }

        public int Port
        {
            get => Get(8090);
            set => Set(value);
        }

        public string Path
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
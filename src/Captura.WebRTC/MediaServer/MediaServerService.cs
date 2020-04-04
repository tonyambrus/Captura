using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{
    public class MediaServerService : IDisposable
    {
        private readonly string streamName;
        private readonly string localId = Environment.MachineName;
        private List<MediaServerSignaler> signallers = new List<MediaServerSignaler>();
        private WebRTCHost webrtc;

        public string ServerAddress { get; }

        public MediaServerService(WebRTCHost webrtc, string serverAddress, string streamName)
        {
            this.webrtc = webrtc;
            this.streamName = streamName;
            this.ServerAddress = serverAddress;

            var _ = EstablishAsBroadcasterAsync();
        }
        
        private async Task EstablishAsBroadcasterAsync()
        {
            try
            {
                var body = $"{{ \"signalFromId\": \"{localId}\" }}";
                var response = await NetworkUtil.PostJsonAsync($"{ServerAddress}broadcast/{streamName}", body);
                Util.WriteLine($"SFU/Transport capabilities received:\n{response}");

                var _ = new MediaServerSignaler(this, localId, streamName, new WebRTCSession(webrtc));
            }
            catch (Exception e)
            {
                Util.WriteLine($"Failed to establish as a broadcaster ({e.Message}). Trying again in 2 seconds");
                Util.WriteLine(e.ToString());

                await Task.Delay(2000);
                await EstablishAsBroadcasterAsync();
            }
        }

        public void Register(MediaServerSignaler signaller)
        {
            lock (signallers)
            {
                signallers.Add(signaller);
            }
        }

        public void Unregister(MediaServerSignaler signaller)
        {
            lock (signallers)
            {
                signallers.Remove(signaller);
            }
        }

        public void Dispose()
        {
            lock (signallers)
            {
                foreach (var signaller in signallers.ToArray())
                {
                    signaller.Dispose();
                }

                signallers.Clear();
            }
        }
    }
}

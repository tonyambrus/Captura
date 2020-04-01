using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{
    public class NodeDssService : IDisposable
    {
        private readonly string localId = Environment.MachineName;
        private List<NodeDssSignaler> signallers = new List<NodeDssSignaler>();

        public string ServerAddress { get; }

        public NodeDssService(WebRTCHost webrtc, string serverAddress, string remoteId)
        {
            this.ServerAddress = serverAddress;
            var _ = new NodeDssSignaler(this, localId, remoteId, new WebRTCSession(webrtc));
        }

        public void Register(NodeDssSignaler signaller)
        {
            lock (signallers)
            {
                signallers.Add(signaller);
            }
        }

        public void Unregister(NodeDssSignaler signaller)
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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{
    public class NodeDssSignaler : IDisposable
    {
        public bool IsClient => false;
        public bool receivedOffer = false;
        
        private NodeDssService service;
        private string localId;
        private string remoteId;

        private WebRTCSession session;
        private NodeDssConnection connection;
        
        public NodeDssSignaler(NodeDssService svc, string localId, string remoteId, WebRTCSession session)
        {
            this.service = svc;
            this.service.Register(this);

            this.localId = localId;
            this.remoteId = remoteId;

            this.connection = new NodeDssConnection(service.ServerAddress, $"data/{localId}");
            this.connection.MessageReceived += OnMessageReceived;
            this.connection.Start();

            this.session = session;
            this.session.Initialized += OnPeerConnectionInitialized;
            this.session.PostShutdown += OnPostShutdown;
            this.session.Peer.Connected += OnPeerConnected;
            this.session.Peer.RenegotiationNeeded += OnRenegotiate;
            this.session.Start();
        }

        private async void OnMessageReceived(string msg)
        {
            var jsonMsg = JObject.Parse(msg);
            if (jsonMsg == null)
            {
                Util.LogError($"Failed to deserialize JSON message : {msg}");
                return;
            }

            // depending on what type of message we get, we'll handle it differently
            // this is the "glue" that allows two peers to establish a connection.
            var msgType = (Message.WireMessageType)(int)jsonMsg["MessageType"];
            var data = (string)jsonMsg["Data"];

            if (msgType == Message.WireMessageType.Offer)
            {
                Util.WriteLine($"NodeDssSignaler.SetRemoteDescription - offer='{Util.PrettyPrint(data, 20)}...'");
                await session.Peer.SetRemoteDescriptionAsync("offer", data);
                receivedOffer = true;

                Util.WriteLine($"NodeDssSignaler.CreateAnswer");
                if (!session.Peer.CreateAnswer())
                {
                    Console.WriteLine("NodeDssSignaler - Failed to create peer connection answer, closing peer connection.");
                    Dispose();
                }
            }
            else if (msgType == Message.WireMessageType.Answer)
            {
                Util.WriteLine($"NodeDssSignaler.SetRemoteDescription - answer='{Util.PrettyPrint(data, 20)}...'");
                await session.Peer.SetRemoteDescriptionAsync("answer", data);
            }
            else if (msgType == Message.WireMessageType.Ice)
            {
                var iceDataSeparator = (string)jsonMsg["IceDataSeparator"];
                
                // this "parts" protocol is defined above, in OnIceCandiateReadyToSend listener
                var parts = data.Split(new string[] { iceDataSeparator }, StringSplitOptions.RemoveEmptyEntries);
                
                // Note the inverted arguments; candidate is last here, but first in OnIceCandiateReadyToSend
                session.Peer.AddIceCandidate(parts[2], int.Parse(parts[1]), parts[0]);
            }
            else
            {
                Util.LogError($"Unknown message '{Util.PrettyPrint(msg, 20)}...'");
            }
        }

        private void OnPeerConnectionInitialized(WebRTCSession session)
        {
            session.Initialized -= OnPeerConnectionInitialized;

            session.Peer.LocalSdpReadytoSend += SendSdpMessage;
            session.Peer.IceCandidateReadytoSend += SendIceCandidateMessage;
            // TODO: session.Peer.SetBitrate()

            session.Peer.CreateOffer();
        }

        public async void SendSdpMessage(string type, string sdp)
        {
            await SendMessageAsync(new Message()
            {
                MessageType = (int)Message.WireMessageTypeFromString(type),
                Data = sdp,
                IceDataSeparator = ""
            });
        }

        /// <summary>
        /// Callback fired when an ICE candidate message has been generated and is ready to
        /// be sent to the remote peer by the signaling object.
        /// </summary>
        public async void SendIceCandidateMessage(string candidate, int sdpMlineIndex, string sdpMid)
        {
            await SendMessageAsync(new Message()
            {
                MessageType = (int)Message.WireMessageType.Ice,
                Data = $"{candidate}|{sdpMlineIndex}|{sdpMid}",
                IceDataSeparator = "|"
            });
        }

        private async Task SendMessageAsync(Message msg)
        {
            var json = JsonConvert.SerializeObject(msg, Formatting.None);
            Util.Log($"NodeDssSignaler.SendMessageAsync('{service.ServerAddress}data/{remoteId}', '{Util.PrettyPrint(json, 60)}...')");
            await NetworkUtil.PostJsonAsync($"{service.ServerAddress}data/{remoteId}", json);
        }

        private void OnPeerConnected()
        {
        }

        private void OnPostShutdown(WebRTCSession obj)
        {
            Dispose();
        }

        public async void Dispose()
        {
            connection.MessageReceived -= OnMessageReceived;
            connection.Dispose();

            session.Peer.LocalSdpReadytoSend -= SendSdpMessage;
            session.Peer.IceCandidateReadytoSend -= SendIceCandidateMessage;

            session.Peer.RenegotiationNeeded -= OnRenegotiate;
            session.Peer.Connected -= OnPeerConnected;
            session.PostShutdown -= OnPostShutdown;
            session.Initialized -= OnPeerConnectionInitialized;
            session.Dispose();

            service.Unregister(this);
        }

        private void OnRenegotiate()
        {
            Util.WriteLine("NodeDssSignaler.RenegotiationNeeded");
            receivedOffer = false;

            Util.WriteLine("NodeDssSignaler.peer.CreateOffer");
            session.Peer.CreateOffer();
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{
    public class MediaServerSignaler : IDisposable
    {
        public bool receivedOffer = false;
        
        private MediaServerService service;
        private string localId;
        private string streamName;

        private WebRTCSession session;
        private NodeDssConnection connection;
        private CancellationTokenSource shutdown;
        
        public MediaServerSignaler(MediaServerService svc, string localId, string streamName, WebRTCSession session)
        {
            this.service = svc;
            this.service.Register(this);

            this.localId = localId;
            this.streamName = streamName;

            this.shutdown = new CancellationTokenSource();

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
                Util.WriteLine($"MediaServerSignaler.SetRemoteDescription - offer='{Util.PrettyPrint(data, 20)}...'");
                await session.Peer.SetRemoteDescriptionAsync("offer", data);
                receivedOffer = true;

                Util.WriteLine($"MediaServerSignaler.CreateAnswer");
                if (!session.Peer.CreateAnswer())
                {
                    Console.WriteLine("MediaServerSignaler - Failed to create peer connection answer, closing peer connection.");
                    Dispose();
                }
            }
            else if (msgType == Message.WireMessageType.Answer)
            {
                Util.WriteLine($"MediaServerSignaler.SetRemoteDescription - answer='{Util.PrettyPrint(data, 20)}...'");
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
            if (shutdown.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var json = JsonConvert.SerializeObject(msg, Formatting.None);
                Util.Log($"MediaServerSignaler.SendMessageAsync('{service.ServerAddress}data/{localId}/{streamName}', '{Util.PrettyPrint(json, 60)}...')");
                await NetworkUtil.PostJsonAsync($"{service.ServerAddress}data/{localId}/{streamName}", json);
            }
            catch
            {
                Util.Log($"Failed to SendMessageAsync, waiting 2 seconds");
                await Task.Delay(2000);
                await SendMessageAsync(msg);
            }
        }

        private async void OnPeerConnected()
        {
            await ResumeConnectionAsync();
        }

        private async Task ResumeConnectionAsync()
        {
            if (shutdown.IsCancellationRequested)
            {
                return;
            }

            try
            {
                string body = $"{{ \"signalFromId\": \"{localId}\" }}";
                Util.Log($"MediaServerSignaler.ResumeConnectionAsync('{service.ServerAddress}broadcast/{streamName}/resume', '{Util.PrettyPrint(body, 60)}...')");
                await NetworkUtil.PostJsonAsync($"{service.ServerAddress}broadcast/{streamName}/resume", body);
            }
            catch
            {
                Util.Log($"Failed to ResumeConnectionAsync, waiting 2 seconds");
                await Task.Delay(2000);
                await ResumeConnectionAsync();
            }
        }

        private void OnPostShutdown(WebRTCSession obj)
        {
            Dispose();
        }

        public async void Dispose()
        {
            shutdown.Cancel();

            connection.MessageReceived -= OnMessageReceived;
            connection.Dispose();

            session.Peer.LocalSdpReadytoSend -= SendSdpMessage;
            session.Peer.IceCandidateReadytoSend -= SendIceCandidateMessage;

            session.Peer.RenegotiationNeeded -= OnRenegotiate;
            session.Peer.Connected -= OnPeerConnected;
            session.PostShutdown -= OnPostShutdown;
            session.Initialized -= OnPeerConnectionInitialized;
            session.Dispose();

            await ShutdownConnectionAsync();

            service.Unregister(this);
        }

        private async Task ShutdownConnectionAsync()
        {
            if (shutdown.IsCancellationRequested)
            {
                return;
            }

            try
            {
                string body = $"{{ \"signalFromId\": \"{localId}\" }}";
                Util.Log($"MediaServerSignaler.ResumeConnectionAsync('{service.ServerAddress}broadcast/{streamName}/shutdown', '{Util.PrettyPrint(body, 60)}...')");
                await NetworkUtil.PostJsonAsync($"{service.ServerAddress}broadcast/{streamName}/shutdown", body);
            }
            catch
            {
                Util.Log($"Failed to ResumeConnectionAsync, waiting 2 seconds");
                await Task.Delay(2000);
                await ResumeConnectionAsync();
            }
        }

        private void OnRenegotiate()
        {
            Util.WriteLine("MediaServerSignaler.RenegotiationNeeded");
            receivedOffer = false;

            Util.WriteLine("MediaServerSignaler.peer.CreateOffer");
            session.Peer.CreateOffer();
        }
    }
}

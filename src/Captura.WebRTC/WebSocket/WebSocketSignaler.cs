using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Captura.Models.WebRTC
{

    public class WebSocketSignaler : WebSocketBehavior, IDisposable
    {
        public bool IsClient => false;
        public bool receivedOffer = false;
        
        private WebRTCSession session;
        private PeerConnection peer;
        
        public bool HasSocketConnection { get; private set; }

        public WebSocketSignaler(WebRTCSession session)
        {
            this.session = session;
            this.peer = session.Peer;

            this.peer.RenegotiationNeeded += OnRenegotiate;
        }

        public void Dispose()
        {
            HasSocketConnection = false;

            this.peer.RenegotiationNeeded -= OnRenegotiate;
        }

        private void OnRenegotiate()
        {
            Util.WriteLine("WebSocketSignaller.RenegotiationNeeded");

            // If already connected, update the connection on the fly.
            // If not, wait for user action and don't automatically connect.
            if (peer.IsConnected && HasSocketConnection)
            {
                receivedOffer = false;

                Util.WriteLine("WebSocketSignaller.peer.CreateOffer");
                peer.CreateOffer();
            }
        }

        protected override void OnOpen()
        {
            Util.WriteLine($"WebSocketSignaller.Open");
            HasSocketConnection = true;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Util.WriteLine($"WebSocketSignaller.Closed {e.Code} {e.Reason} {e.WasClean}");
            Dispose();
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Util.WriteLine($"WebSocketSignaller.Error {e.Message}");
            session.Dispose();
            Dispose();
        }

        protected override async void OnMessage(MessageEventArgs e)
        {
            var msg = e.Data;
            var jsonMsg = JObject.Parse(msg);

            Util.WriteLine($"WebSocketSignaller.OnMessage {Util.PrettyPrint(msg, 20)}...");

            if ((string)jsonMsg["type"] == "ice")
            {
                while (!peer.Initialized)
                {
                    // This delay is needed due to an initialise bug in the Microsoft.MixedReality.WebRTC
                    // nuget packages up to version 0.2.3. On master awaiting pc.InitializeAsync does end 
                    // up with the pc object being ready.
                    Util.WriteLine("WebSocketSignaller.peer.NotInitialized - Sleeping for 1s while peer connection is initialising...");
                    await Task.Delay(1000);
                }

                Util.WriteLine("WebSocketSignaller.AddIceCandidate");
                peer.AddIceCandidate((string)jsonMsg["sdpMLineindex"], (int)jsonMsg["sdpMid"], (string)jsonMsg["candidate"]);
            }
            else if ((string)jsonMsg["type"] == "sdp")
            {
                Util.WriteLine("WebSocketSignaller.OnMessage - Received SDP offer.");

                peer.IceCandidateReadytoSend += (string candidate, int sdpMlineindex, string sdpMid) =>
                {
                    var iceCandidate = new JObject {
                        { "type", "ice" },
                        { "candidate", candidate },
                        { "sdpMLineindex", sdpMlineindex },
                        { "sdpMid", sdpMid}
                    }.ToString();

                    Util.WriteLine($"WebSocketSignaller.SendIceCandidate - {Util.PrettyPrint(iceCandidate, 20)}");
                    Context.WebSocket.Send(iceCandidate);
                };

                peer.LocalSdpReadytoSend += (string type, string sdp) =>
                {
                    var msgType = receivedOffer ? "answer" : "offer";
                    var sdpAnswer = new JObject {
                        { "type", "sdp" },
                        { msgType, sdp }
                    }.ToString();

                    Util.WriteLine($"WebSocketSignaller.SendSDP - {Util.PrettyPrint(sdpAnswer, 20)}");
                    Context.WebSocket.Send(sdpAnswer);
                };

                if (!peer.Initialized)
                {
                    Util.WriteLine($"WebSocketSignaller.peer.InitializeAsync");
                    await peer.InitializeAsync(PeerConnectionConfig.Default);
                }

                if (jsonMsg.ContainsKey("answer"))
                {
                    var answer = (string)jsonMsg["answer"];
                    Util.WriteLine($"WebSocketSignaller.SetRemoteDescription - answer='{Util.PrettyPrint(answer, 20)}...'");
                    await peer.SetRemoteDescriptionAsync("answer", answer);
                }
                else if (jsonMsg.ContainsKey("offer"))
                {
                    var offer = (string)jsonMsg["offer"];
                    Util.WriteLine($"WebSocketSignaller.SetRemoteDescription - offer='{Util.PrettyPrint(offer, 20)}...'");
                    await peer.SetRemoteDescriptionAsync("offer", offer);
                    receivedOffer = true;

                    Util.WriteLine($"WebSocketSignaller.CreateAnswer");
                    if (!peer.CreateAnswer())
                    {
                        Console.WriteLine("WebSocketSignaller - Failed to create peer connection answer, closing peer connection.");
                        Dispose();
                    }
                }
                else
                {
                    throw new Exception("Unknown message " + msg);
                }

                Util.WriteLine("WebSocketSignaller.Complete");
            }
        }
    }
}

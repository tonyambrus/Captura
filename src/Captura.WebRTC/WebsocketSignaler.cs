using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Captura.Models.WebRTC
{
    public class WebSocketSignaler : IDisposable
    {
        public event Action Opened;
        public event Action<CloseEventArgs> Closed;
        public event Action<WebSocket, ErrorEventArgs> Error;

        public bool IsClient => false;
        public bool receivedOffer = false;
        
        private WebSocketSession session;
        private PeerConnection peer;

        public WebSocketSignaler(WebSocketSession session, PeerConnection peer)
        {
            this.session = session;
            this.peer = peer;

            this.peer.RenegotiationNeeded += OnRenegotiate;
        }

        public void Dispose()
        {
            this.peer.RenegotiationNeeded -= OnRenegotiate;
        }

        private void OnRenegotiate()
        {
            Debug.WriteLine("pc.RenegotiationNeeded");

            // If already connected, update the connection on the fly.
            // If not, wait for user action and don't automatically connect.
            if (peer.IsConnected && session.HasSocketConnection)
            {
                receivedOffer = false;

                Debug.WriteLine("pc.CreateOffer");
                peer.CreateOffer();
            }
        }

        public async void OnMessage(string msg)
        {
            JObject jsonMsg = JObject.Parse(msg);

            if ((string)jsonMsg["type"] == "ice")
            {
                while (!peer.Initialized)
                {
                    // This delay is needed due to an initialise bug in the Microsoft.MixedReality.WebRTC
                    // nuget packages up to version 0.2.3. On master awaiting pc.InitializeAsync does end 
                    // up with the pc object being ready.
                    Debug.WriteLine("Sleeping for 1s while peer connection is initialising...");
                    await Task.Delay(1000);
                }

                peer.AddIceCandidate((string)jsonMsg["sdpMLineindex"], (int)jsonMsg["sdpMid"], (string)jsonMsg["candidate"]);
            }
            else if ((string)jsonMsg["type"] == "sdp")
            {
                Debug.WriteLine("Received remote peer SDP offer.");

                peer.IceCandidateReadytoSend += (string candidate, int sdpMlineindex, string sdpMid) =>
                {
                    Debug.WriteLine($"Sending ice candidate: {candidate}");
                    var iceCandidate = new JObject {
                        { "type", "ice" },
                        { "candidate", candidate },
                        { "sdpMLineindex", sdpMlineindex },
                        { "sdpMid", sdpMid}
                    };
                    session.Context.WebSocket.Send(iceCandidate.ToString());
                };

                peer.IceStateChanged += (newState) =>
                {
                    Debug.WriteLine($"ice connection state changed to {newState}.");
                };

                peer.LocalSdpReadytoSend += (string type, string sdp) =>
                {
                    // Send our SDP answer to the remote peer.
                    var msgType = receivedOffer ? "answer" : "offer";
                    var sdpAnswer = new JObject {
                        { "type", "sdp" },
                        { msgType, sdp }
                    };

                    var msg = sdpAnswer.ToString();
                    Debug.WriteLine($"SDP answer ready, sending to remote peer. {msg}");
                    session.Context.WebSocket.Send(msg);
                };

                if (!peer.Initialized)
                {
                    Debug.WriteLine($"Reinitializing PC");
                    await peer.InitializeAsync(PeerConnectionConfig.Default);
                }

                if (jsonMsg.ContainsKey("answer"))
                {
                    Debug.WriteLine($"setting Remote SDP answer.");
                    await peer.SetRemoteDescriptionAsync("answer", (string)jsonMsg["answer"]);
                }
                else if (jsonMsg.ContainsKey("offer"))
                {
                    Debug.WriteLine($"Setting Remote SDP offer");
                    await peer.SetRemoteDescriptionAsync("offer", (string)jsonMsg["offer"]);
                    receivedOffer = true;

                    Debug.WriteLine($"Creating answer SDP");
                    if (!peer.CreateAnswer())
                    {
                        Console.WriteLine("Failed to create peer connection answer, closing peer connection.");
                        peer.Close();
                        session.Context.WebSocket.Close();
                    }
                }
                else
                {
                    throw new Exception("Unknown message " + msg);
                }

                Debug.WriteLine("Ready.");
            }
        }
    }
}

using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Captura.Models.WebRTC
{
    public class WebsocketSignaler : IDisposable
    {
        public class WebRtcSession : WebSocketBehavior
        {
            public PeerConnection pc { get; private set; }

            public event Action<WebRtcSession, string> MessageReceived;
            public event Action<WebRtcSession, CloseEventArgs> Closed;
            public new event Action<WebRtcSession, ErrorEventArgs> Error;
            public event Action<WebRtcSession> Opened;

            public WebRtcSession(PeerConnection pc)
            {
                this.pc = pc ?? new PeerConnection();
            }

            protected override void OnOpen()
            {
                Opened(this);
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                MessageReceived(this, e.Data);
            }


            protected override void OnError(ErrorEventArgs e)
            {
                Error(this, e);
            }

            protected override void OnClose(CloseEventArgs e)
            {
                Closed(this, e);
            }
        }

        private const int WEBSOCKET_PORT = 8081;
        private static WebSocketServer webSocketServer;

        public event Action Opened;
        public event Action<CloseEventArgs> Closed;
        public event Action<WebSocket, ErrorEventArgs> Error;

        public bool IsClient => false;
        public bool receivedOffer = false;

        public WebsocketSignaler(PeerConnection pc, string certPath = null, int port = WEBSOCKET_PORT, bool secure = true)
        {
            try
            {
                // Start web socket server.
                Debug.WriteLine("Starting web socket server...");
                webSocketServer = new WebSocketServer(IPAddress.Any, port, secure);
                if (secure)
                {
                    webSocketServer.SslConfiguration.ServerCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(certPath);
                    webSocketServer.SslConfiguration.CheckCertificateRevocation = false;
                }

                //webSocketServer.Log.Level = WebSocketSharp.LogLevel.Debug;
                webSocketServer.AddWebSocketService<WebRtcSession>("/", () =>
                {
                    var session = new WebRtcSession(pc);
                    session.Opened += (s) => Opened();
                    session.MessageReceived += MessageReceived;
                    session.Error += (s, e) => Error(s.Context.WebSocket, e);
                    session.Closed += (s, e) => Closed(e);
                    return session;
                });
                webSocketServer.Start();

                Debug.WriteLine($"Waiting for browser web socket connection to {webSocketServer.Address}:{webSocketServer.Port}...");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        public void Dispose()
        {
            webSocketServer?.Stop();
            webSocketServer = null;
        }

        private async void MessageReceived(WebRtcSession session, string msg)
        {
            //Debug.WriteLine($"web socket recv: {msg.Length} bytes");

            JObject jsonMsg = JObject.Parse(msg);

            if ((string)jsonMsg["type"] == "ice")
            {
                //Debug.WriteLine($"Adding remote ICE candidate {msg}.");

                while (!session.pc.Initialized)
                {
                    // This delay is needed due to an initialise bug in the Microsoft.MixedReality.WebRTC
                    // nuget packages up to version 0.2.3. On master awaiting pc.InitializeAsync does end 
                    // up with the pc object being ready.
                    Debug.WriteLine("Sleeping for 1s while peer connection is initialising...");
                    await Task.Delay(1000);
                }

                session.pc.AddIceCandidate((string)jsonMsg["sdpMLineindex"], (int)jsonMsg["sdpMid"], (string)jsonMsg["candidate"]);
            }
            else if ((string)jsonMsg["type"] == "sdp")
            {
                Debug.WriteLine("Received remote peer SDP offer.");

                var config = new PeerConnectionConfiguration();

                session.pc.IceCandidateReadytoSend += (string candidate, int sdpMlineindex, string sdpMid) =>
                {
                    Debug.WriteLine($"Sending ice candidate: {candidate}");
                    JObject iceCandidate = new JObject {
                        { "type", "ice" },
                        { "candidate", candidate },
                        { "sdpMLineindex", sdpMlineindex },
                        { "sdpMid", sdpMid}
                    };
                    session.Context.WebSocket.Send(iceCandidate.ToString());
                };

                session.pc.IceStateChanged += (newState) =>
                {
                    Debug.WriteLine($"ice connection state changed to {newState}.");
                };

                session.pc.LocalSdpReadytoSend += (string type, string sdp) =>
                {

                    // Send our SDP answer to the remote peer.
                    var msgType = receivedOffer ? "answer" : "offer";
                    JObject sdpAnswer = new JObject {
                        { "type", "sdp" },
                        { msgType, sdp }
                    };

                    var msg = sdpAnswer.ToString();
                    Debug.WriteLine($"SDP answer ready, sending to remote peer. {msg}");
                    session.Context.WebSocket.Send(msg);
                };

                if (jsonMsg.ContainsKey("answer"))
                {
                    Debug.WriteLine($"setting Remote SDP answer.");
                    await session.pc.SetRemoteDescriptionAsync("answer", (string)jsonMsg["answer"]);
                }
                else if (jsonMsg.ContainsKey("offer"))
                {
                    Debug.WriteLine($"Reinitializing PC");
                    await session.pc.InitializeAsync(config);
                    
                    Debug.WriteLine($"Setting Remote SDP offer");
                    await session.pc.SetRemoteDescriptionAsync("offer", (string)jsonMsg["offer"]);
                    receivedOffer = true;

                    Debug.WriteLine($"Creating answer SDP");
                    if (!session.pc.CreateAnswer())
                    {
                        Console.WriteLine("Failed to create peer connection answer, closing peer connection.");
                        session.pc.Close();
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

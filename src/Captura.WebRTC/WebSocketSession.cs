using Microsoft.MixedReality.WebRTC;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Captura.Models.WebRTC
{
    public class WebSocketSession : WebSocketBehavior, IDisposable
    {
        private WebSocketService service;
        private WebRTCConnection webrtc;
        private PeerConnection peer;
        private SceneVideoSource source;
        private WebSocketSignaler signaler;
        private CancellationTokenSource cancelSession;
        private Task runTask;

        public bool HasPeerConnection { get; private set; }
        public bool HasSocketConnection { get; private set; }
        public bool IsConnected => HasPeerConnection && HasSocketConnection;

        public WebSocketSession(WebSocketService service, WebRTCConnection webrtc)
        {
            this.service = service;
            this.webrtc = webrtc;
            this.cancelSession = new CancellationTokenSource();
            this.peer = new PeerConnection();
            this.source = new SceneVideoSource() { PeerConnection = peer };
            this.signaler = new WebSocketSignaler(this, peer);

            this.peer.IceStateChanged += OnIceStateChanged;
            this.webrtc.VideoFrameReady += OnFrameReady;

            this.runTask = Run();
        }

        public void Dispose()
        {
            HasSocketConnection = false;
            HasPeerConnection = false;

            webrtc.VideoFrameReady -= OnFrameReady;

            cancelSession.Cancel();

            signaler?.Dispose();
            signaler = null;

            source?.Dispose();
            source = null;

            peer?.Dispose();
            peer = null;
        }

        private void OnIceStateChanged(IceConnectionState newState)
        {
            Debug.WriteLine($"WebRTCConnection.pc.IceStateChanged {newState}");

            switch (newState)
            {
                case IceConnectionState.Connected:
                    HasPeerConnection = true;
                    break;

                case IceConnectionState.Disconnected:
                    HasPeerConnection = false;
                    CloseConnection();
                    break;
            }
        }

        private void OnFrameReady(byte[] buffer, int width, int height)
        {
            if (IsConnected)
            {
                source.OnFrameReady(buffer, width, height);
            }
        }

        protected override void OnOpen() 
        {
            Debug.WriteLine($"WebRTCConnection.signaler.Opened");
            HasSocketConnection = true;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.WriteLine($"WebRTCConnection.signaler.Closed {e.Code} {e.Reason} {e.WasClean}");
            CloseConnection();
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.WriteLine($"WebRTCConnection.signaler.Error {e.Message}");
            CloseConnection();
        }

        protected override void OnMessage(MessageEventArgs e) => signaler.OnMessage(e.Data);

        private void CloseConnection()
        {
            cancelSession.Cancel();
            HasSocketConnection = false;
        }

        private async Task WaitForConnectionClose()
        {
            try
            {
                await Task.Delay(-1, cancelSession.Token);
            }
            catch (TaskCanceledException)
            {
            }
        }

        public async Task Run()
        {
            Debug.WriteLine("WebRTCConnection.Run");

            try
            {
                Debug.WriteLine("WebRTCConnection.pc.InitializeAsync");
                await peer.InitializeAsync(PeerConnectionConfig.Default);

                Debug.WriteLine("WebRTCConnection.source.StartTrack");
                source.StartTrack();

                Debug.WriteLine("WebRTCConnection.pc.AddLocalAudioTrackAsync");
                await peer.AddLocalAudioTrackAsync();

                Debug.WriteLine("WebRTCConnection.WaitForConnectionClose");
                await WaitForConnectionClose();

                Debug.WriteLine("WebRTCConnection.ConnectionClosed");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error: {e}");
            }
            finally
            {
                Debug.WriteLine("WebRTCConnection.Disposing");
                Dispose();
                Debug.WriteLine("WebRTCConnection.Disposed");
            }
        }
    }
}

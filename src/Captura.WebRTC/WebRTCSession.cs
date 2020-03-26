using Microsoft.MixedReality.WebRTC;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{
    public class WebRTCSession : IDisposable
    {
        private WebRTCConnection webrtc;
        private PeerConnection peer;
        private SceneVideoSource source;
        private CancellationTokenSource cancelSession;

        public IceConnectionState ConnectionState { get; private set; }
        public bool IsConnected => ConnectionState == IceConnectionState.Connected;
        public PeerConnection Peer => peer;

        public delegate ISignaler SignallerFactory(WebRTCSession session, PeerConnection peer);

        public WebRTCSession(WebRTCConnection webrtc)
        {
            this.webrtc = webrtc;
            this.cancelSession = new CancellationTokenSource();
            this.peer = new PeerConnection();
            this.source = new SceneVideoSource() { PeerConnection = peer };

            this.peer.IceStateChanged += OnIceStateChanged;
            
            this.webrtc.VideoFrameReady += OnFrameReady;
            this.webrtc.Register(this);

            var _ = Run();
        }

        public void Dispose()
        {
            ConnectionState = IceConnectionState.Disconnected;

            webrtc.VideoFrameReady -= OnFrameReady;
            webrtc.Unregister(this);

            cancelSession.Cancel();

            source?.Dispose();
            source = null;

            peer?.Dispose();
            peer = null;
        }

        private void OnIceStateChanged(IceConnectionState newState)
        {
            Util.WriteLine($"WebRTCSession.IceStateChanged {newState}");

            ConnectionState = newState;
            if (newState == IceConnectionState.Disconnected)
            { 
                cancelSession.Cancel();
            }
        }

        private void OnFrameReady(byte[] buffer, int width, int height)
        {
            if (IsConnected)
            {
                source.OnFrameReady(buffer, width, height);
            }
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
            Util.WriteLine("WebRTCSession.Run");

            try
            {
                Util.WriteLine("WebRTCSession.peer.InitializeAsync");
                await peer.InitializeAsync(PeerConnectionConfig.Default);

                Util.WriteLine("WebRTCSession.source.StartTrack");
                source.StartTrack();

                Util.WriteLine("WebRTCSession.peer.AddLocalAudioTrackAsync");
                await peer.AddLocalAudioTrackAsync();

                Util.WriteLine("WebRTCSession.WaitForConnectionClose");
                await WaitForConnectionClose();

                Util.WriteLine("WebRTCSession.ConnectionClosed");
            }
            catch (Exception e)
            {
                Util.WriteLine($"WebRTCSession.Error: {e}");
            }
            finally
            {
                Util.WriteLine("WebRTCSession.Disposing");
                Dispose();
                Util.WriteLine("WebRTCSession.Disposed");
            }
        }
    }
}

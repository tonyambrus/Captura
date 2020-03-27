using Microsoft.MixedReality.WebRTC;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{
    public class WebRTCSession : IDisposable
    {
        private WebRTCHost webrtc;
        private PeerConnection peer;
        private SceneVideoSource source;
        private CancellationTokenSource cancelSession;

        public IceConnectionState ConnectionState { get; private set; }
        public bool IsConnected { get; private set; }
        public PeerConnection Peer => peer;

        private bool started;

        public event Action<WebRTCSession> Initialized;
        public event Action<WebRTCSession> PreShutdown;
        public event Action<WebRTCSession> PostShutdown;

        public WebRTCSession(WebRTCHost webrtc)
        {
            this.webrtc = webrtc;
            this.cancelSession = new CancellationTokenSource();
            this.peer = new PeerConnection();
            this.source = new SceneVideoSource() { PeerConnection = peer };

            this.peer.IceStateChanged += OnIceStateChanged;
            this.peer.Connected += OnConnected;
            
            this.webrtc.VideoFrameReady += OnFrameReady;
            this.webrtc.Register(this);
        }

        private void OnConnected()
        {
            Util.WriteLine($"WebRTCSession.OnConnected");
            IsConnected = true;
        }

        public void Dispose()
        {
            ConnectionState = IceConnectionState.Disconnected;

            webrtc.VideoFrameReady -= OnFrameReady;
            webrtc.Unregister(this);

            cancelSession.Cancel();

            source.Dispose();

            peer.Dispose();
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

        public void Start()
        {
            if (started)
            {
                throw new Exception("Already started");
            }

            started = true;
            var _ = StartAsync();
        }

        private async Task StartAsync()
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

                Initialized?.Invoke(this);

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
                PreShutdown?.Invoke(this);

                Util.WriteLine("WebRTCSession.Disposing");
                Dispose();
                Util.WriteLine("WebRTCSession.Disposed");
            }

            PostShutdown?.Invoke(this);
        }
    }
}

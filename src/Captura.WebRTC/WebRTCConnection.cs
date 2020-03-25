using Microsoft.MixedReality.WebRTC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Captura.Models.WebRTC
{

    class WebRTCConnection : IDisposable
    {
        private PeerConnection pc;
        private SceneVideoSource source;
        private WebsocketSignaler signaler;

        public bool IsConnected { get; private set; }

        public WebRTCConnection()
        {
            pc = new PeerConnection();

            source = new SceneVideoSource();
            source.PeerConnection = pc;

            pc.Connected += () => {
                Debug.WriteLine("pc.Connected");
                IsConnected = true;
            };

            pc.RenegotiationNeeded += () => {
                Debug.WriteLine("pc.RenegotiationNeeded");
                //var _ = ConnectAsync();
                
                // If already connected, update the connection on the fly.
                // If not, wait for user action and don't automatically connect.
                if (pc.IsConnected)
                {
                    pc.CreateOffer();
                    signaler.receivedOffer = false;
                }
            };

            signaler = new WebsocketSignaler(pc, secure: false);
            signaler.Opened += () =>
            {
                Debug.WriteLine($"signaler.Opened");
            };
            signaler.Error += (s, e) =>
            {
                Debug.WriteLine($"signaler.Error {e.Message} {s.IsAlive}");
            };
            signaler.Closed += (e) =>
            {
                Debug.WriteLine($"signaler.Closed {e.Code} {e.Reason} {e.WasClean}");
                IsConnected = false;
                
                var _ = ConnectAsync();
            };

            var _ = ConnectAsync();
        }

        public async Task ConnectAsync()
        {
            Debug.WriteLine("ConnectAsync");

            Debug.WriteLine("source?.StopTrack");
            source?.StopTrack();

            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer>() {
                    new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                }
            };

            Debug.WriteLine("pc?.InitializeAsync");
            await pc?.InitializeAsync(config);
            
            Debug.WriteLine("source?.StartTrack");
            source?.StartTrack();

            Debug.WriteLine("pc?.AddLocalAudioTrackAsync");
            try
            {
                await pc?.AddLocalAudioTrackAsync();
            }
            catch(Exception e)
            {
                Debug.WriteLine($"Error: {e}");
            }
        }


        public bool WriteFrame(byte[] videoBuffer, int width, int height)
        {
            if (IsConnected)
            {
                return source.OnFrameReady(videoBuffer, width, height);
            }

            return false;
        }

        public void Dispose()
        {
            source?.Dispose();
            source = null;

            pc?.Dispose();
            pc = null;

            signaler?.Dispose();
            signaler = null;
        }
    }
}

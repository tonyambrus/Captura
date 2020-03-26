using System;
using System.Collections.Generic;

namespace Captura.Models.WebRTC
{
    public class WebRTCConnection : IDisposable
    {
        private WebSocketService service;
        private List<WebSocketSession> sessions = new List<WebSocketSession>();

        public event Action<byte[], int, int> VideoFrameReady;

        public WebRTCConnection(WebRTCSettings _settings)
        {
            service = new WebSocketService(CreateSession, _settings.Port);
        }

        private WebSocketSession CreateSession(WebSocketService service)
        {
            var session = new WebSocketSession(service, this);
            lock (sessions)
            {
                sessions.Add(session);
            }
            return session;
        }

        public void Dispose()
        {
            lock (sessions)
            {
                foreach (var session in sessions)
                {
                    session.Dispose();
                }
                sessions.Clear();
            }

            service?.Dispose();
            service = null;
        }

        public bool WriteFrame(byte[] videoBuffer, int width, int height)
        {
            if (VideoFrameReady != null)
            {
                VideoFrameReady(videoBuffer, width, height);
                return true;
            }

            return false;
        }
    }
}

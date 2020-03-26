using System;
using System.Collections.Generic;

namespace Captura.Models.WebRTC
{
    public class WebRTCConnection : IDisposable
    {
        private WebSocketService service;
        private List<WebRTCSession> sessions = new List<WebRTCSession>();

        public event Action<byte[], int, int> VideoFrameReady;

        public WebRTCConnection(WebRTCSettings _settings)
        {
            service = new WebSocketService(svc => new WebSocketSignaler(new WebRTCSession(this)), _settings.Port);
        }

        public void Register(WebRTCSession session)
        {
            lock (sessions)
            {
                sessions.Add(session);
            }
        }

        public bool Unregister(WebRTCSession session)
        {
            lock (sessions)
            {
                return sessions.Remove(session);
            }
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

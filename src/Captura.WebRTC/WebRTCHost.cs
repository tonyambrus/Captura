using ScreenShare;
using System;
using System.Collections.Generic;

namespace Captura.Models.WebRTC
{
    public class WebRTCHost : IDisposable
    {
        private IDisposable service;
        private IDisposable screenShare;
        private List<WebRTCSession> sessions = new List<WebRTCSession>();

        public event Action<byte[], int, int> VideoFrameReady;

        public WebRTCHost(WebRTCSettings settings)
        {
            screenShare = new ScreenShare.ScreenShare();

            service = new MediaServerService(this, "http://192.168.1.158:3000/channel/mediaserver/", "mediaserver");

            //if (true || settings.Mode == WebRTCEndpoint.NodeDSS)
            //{
            //    service = new NodeDssService(this, settings.MediaServerUrl, settings.MediaServerStreamName);
            //}
            //else if (settings.Mode == WebRTCEndpoint.WebSocket)
            //{
            //    service = new WebSocketService(this, settings.WebSocketPath, settings.WebSocketPort);
            //}
            //else if (settings.Mode == WebRTCEndpoint.MediaServer)
            //{ 
            //    service = new MediaServerService(this, settings.MediaServerUrl, settings.MediaServerStreamName);
            //}
            //else
            //{
            //    throw new Exception($"Invalid mode {settings.Mode}");
            //}
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
            screenShare?.Dispose();
            screenShare = null;

            lock (sessions)
            {
                foreach (var session in sessions.ToArray())
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

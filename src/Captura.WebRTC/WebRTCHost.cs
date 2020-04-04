﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Captura.Models.WebRTC
{
    public class WebRTCHost : IDisposable
    {
        private IDisposable service;
        private ScreenShare.ScreenShare screenShare;
        private List<WebRTCSession> sessions = new List<WebRTCSession>();
        private CancellationTokenSource shutdown;

        public event Action<byte[], int, int> VideoFrameReady;

        public WebRTCHost(WebRTCSettings settings)
        {
            Run(settings);
        }

        private async void Run(WebRTCSettings settings)
        {
            shutdown = new CancellationTokenSource();

            bool useScreenShare = true;
            if (useScreenShare)
            {
                screenShare = new ScreenShare.ScreenShare(settings.MediaServerUrl, settings.MediaServerStreamName);
                await screenShare.Start(shutdown.Token);

                if (screenShare != null && !shutdown.IsCancellationRequested)
                {
                    service = new MediaServerService(this, screenShare.ChannelAddress, screenShare.StreamName);
                }
            }
            else
            {
                if (settings.Mode == WebRTCEndpoint.NodeDSS)
                {
                    service = new NodeDssService(this, settings.MediaServerUrl, settings.MediaServerStreamName);
                }
                else if (settings.Mode == WebRTCEndpoint.WebSocket)
                {
                    service = new WebSocketService(this, settings.WebSocketPath, settings.WebSocketPort);
                }
                else if (settings.Mode == WebRTCEndpoint.MediaServer)
                { 
                    service = new MediaServerService(this, settings.MediaServerUrl, settings.MediaServerStreamName);
                }
                else
                {
                    throw new Exception($"Invalid mode {settings.Mode}");
                }
            }
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

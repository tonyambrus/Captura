using Microsoft.MixedReality.WebRTC;
using System;

namespace Captura.Models.WebRTC
{
    public abstract class VideoSource : IDisposable
    {
        /// <summary>
        /// Frame queue holding the pending frames enqueued by the video source itself,
        /// which a video renderer needs to read and display.
        /// </summary>
        public IVideoFrameQueue FrameQueue { get; protected set; }

        /// <summary>
        /// Event invoked from the main Unity thread when the video stream starts.
        /// This means that video frames are available and the renderer should start polling.
        /// </summary>
        public event Action VideoStreamStarted;

        /// <summary>
        /// Event invoked from the main Unity thread when the video stream stops.
        /// This means that the video frame queue is not populated anymore, though some frames
        /// may still be present in it that may be rendered.
        /// </summary>
        public event Action VideoStreamStopped;

        protected void NotifyVideoStreamStopped() => VideoStreamStopped?.Invoke();
        protected void NotifyVideoStreamStarted() => VideoStreamStarted?.Invoke();

        public virtual void Dispose() { }
    }
}

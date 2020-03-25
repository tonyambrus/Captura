using Microsoft.MixedReality.WebRTC;
using System;

namespace Captura.Models.WebRTC
{
    public abstract class CustomVideoSource<T> : VideoSource where T : class, IVideoFrameStorage, new()
    {
        /// <summary>
        /// Peer connection this local video source will add a video track to.
        /// </summary>
        public PeerConnection PeerConnection;

        /// <summary>
        /// Name of the track.
        /// </summary>
        /// <remarks>
        /// This must comply with the 'msid' attribute rules as defined in
        /// https://tools.ietf.org/html/draft-ietf-mmusic-msid-05#section-2, which in
        /// particular constraints the set of allows characters to those allowed for a
        /// 'token' element as specified in https://tools.ietf.org/html/rfc4566#page-43:
        /// - Symbols [!#$%'*+-.^_`{|}~] and ampersand &amp;
        /// - Alphanumerical [A-Za-z0-9]
        /// </remarks>
        /// <seealso xref="SdpTokenAttribute.ValidateSdpTokenName"/>
        public string TrackName;

        /// <summary>
        /// Automatically add the video track to the peer connection when the Unity component starts.
        /// </summary>
        public bool AutoAddTrackOnStart = true;

        /// <summary>
        /// Video track source providing video frames to the local video track.
        /// </summary>
        public ExternalVideoTrackSource Source { get; private set; }

        /// <summary>
        /// Video track encapsulated by this component.
        /// </summary>
        public LocalVideoTrack Track { get; private set; }

        /// <summary>
        /// Frame queue holding the pending frames enqueued by the video source itself,
        /// which a video renderer needs to read and display.
        /// </summary>
        protected VideoFrameQueue<T> _frameQueue;

        /// <summary>
        /// Add a new track to the peer connection and start the video track playback.
        /// </summary>
        public void StartTrack()
        {
            // Ensure the track has a valid name
            string trackName = TrackName;
            if (trackName == null || trackName.Length == 0)
            {
                // Generate a unique name (GUID)
                trackName = Guid.NewGuid().ToString();
                TrackName = trackName;
            }
            //SdpTokenAttribute.Validate(trackName, allowEmpty: false);

            // Create the external source
            var nativePeer = PeerConnection;
            //< TODO - Better abstraction
            if (typeof(T) == typeof(I420AVideoFrameStorage))
            {
                Source = ExternalVideoTrackSource.CreateFromI420ACallback(OnFrameRequested);
            }
            else if (typeof(T) == typeof(Argb32VideoFrameStorage))
            {
                Source = ExternalVideoTrackSource.CreateFromArgb32Callback(OnFrameRequested);
            }
            else
            {
                throw new NotSupportedException("");
            }

            // Create the local video track
            if (Source != null)
            {
                Track = nativePeer.AddCustomLocalVideoTrack(trackName, Source);
                if (Track != null)
                {
                    NotifyVideoStreamStarted();
                }
            }
        }

        /// <summary>
        /// Stop the video track playback and remove the track from the peer connection.
        /// </summary>
        public void StopTrack()
        {
            if (Track != null)
            {
                var nativePeer = PeerConnection;
                nativePeer.RemoveLocalVideoTrack(Track);
                Track.Dispose();
                Track = null;
                NotifyVideoStreamStopped();
            }
            if (Source != null)
            {
                Source.Dispose();
                Source = null;
            }
            _frameQueue.Clear();
        }

        protected CustomVideoSource()
        {
            _frameQueue = new VideoFrameQueue<T>(3);
            FrameQueue = _frameQueue;
            //PeerConnection.OnInitialized.AddListener(OnPeerInitialized);
            //PeerConnection.OnShutdown.AddListener(OnPeerShutdown);

            OnEnable();
        }

        public override void Dispose()
        {
            OnDisable();

            StopTrack();
            //PeerConnection.OnInitialized.RemoveListener(OnPeerInitialized);
            //PeerConnection.OnShutdown.RemoveListener(OnPeerShutdown);
        }

        protected void OnEnable()
        {
            if (Track != null)
            {
                Track.Enabled = true;
            }
        }

        protected void OnDisable()
        {
            if (Track != null)
            {
                Track.Enabled = false;
            }
        }

        private void OnPeerInitialized()
        {
            if (AutoAddTrackOnStart)
            {
                StartTrack();
            }
        }

        private void OnPeerShutdown()
        {
            StopTrack();
        }

        protected abstract void OnFrameRequested(in FrameRequest request);
    }
}

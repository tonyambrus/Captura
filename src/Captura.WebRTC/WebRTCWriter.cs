using Captura.Audio;
using System.Threading.Tasks;

namespace Captura.Models
{

    /// <summary>
    /// Writes an AVI file.
    /// </summary>
    class WebRTCWriter : IVideoFileWriter
    {
        #region Fields
        WebRTC.WebRTCHost _connection;
        byte[] _videoBuffer;
        int _width;
        int _height;
        readonly WebRTCCodec _codec;
        readonly object _syncLock = new object();
        
        /// <summary>
        /// Gets whether Audio is recorded.
        /// </summary>
        public bool SupportsAudio => true;
        #endregion

        /// <summary>
        /// Creates a new instance of <see cref="WebRTCWriter"/>.
        /// </summary>
        /// <param name="FileName">Output file path.</param>
        /// <param name="Codec">The Avi Codec.</param>
        /// <param name="ImageProvider">The image source.</param>
        /// <param name="FrameRate">Video Frame Rate.</param>
        /// <param name="AudioProvider">The audio source. null = no audio.</param>
        public WebRTCWriter(string FileName, WebRTCCodec Codec, WebRTCSettings settings, IImageProvider ImageProvider, int FrameRate, IAudioProvider AudioProvider = null)
        {
            _codec = Codec;

            _width = ImageProvider.Width;
            _height = ImageProvider.Height;
            _videoBuffer = new byte[_width * _height * 4];
            _connection = new WebRTC.WebRTCHost(settings);
        }
        
        /// <summary>
        /// Writes an Image frame.
        /// </summary>
        public void WriteFrame(IBitmapFrame Frame)
        {
            if (!(Frame is RepeatFrame))
            {
                using (Frame)
                {
                    Frame.CopyTo(_videoBuffer);
                }
            }

            lock (_syncLock)
            {
                _connection.WriteFrame(_videoBuffer, _width, _height);
            }
        }

        /// <summary>
        /// Write audio block to Audio Stream.
        /// </summary>
        /// <param name="Buffer">Buffer containing audio data.</param>
        /// <param name="Length">Length of audio data in bytes.</param>
        public void WriteAudio(byte[] Buffer, int Offset, int Length)
        {
            //lock (_syncLock)
            //    _audioStream?.WriteBlock(Buffer, Offset, Length);
        }

        /// <summary>
        /// Frees all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            lock (_syncLock)
            {
                _connection.Dispose();
                _connection = null;
            }

            _videoBuffer = null;
        }
    }
}

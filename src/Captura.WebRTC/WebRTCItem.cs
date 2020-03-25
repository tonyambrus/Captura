namespace Captura.Models
{
    class WebRTCItem : IVideoWriterItem
    {
        readonly WebRTCCodec _codec;

        public WebRTCItem(WebRTCCodec Codec, string Description)
        {
            _codec = Codec;
            this.Description = Description;
        }

        public string Extension { get; } = ".rtc";

        public string Description { get; }

        public IVideoFileWriter GetVideoFileWriter(VideoWriterArgs Args)
        {
            _codec.Quality = Args.VideoQuality;

            return new WebRTCWriter(Args.FileName, _codec, Args.ImageProvider, Args.FrameRate, Args.AudioProvider);
        }
        
        public override string ToString() => _codec.Name;
    }
}

using SharpAvi;

namespace Captura.Models
{
    /// <summary>
    /// Represents an WebRTC Codec.
    /// </summary>
    class WebRTCCodec
    {
        // ReSharper disable once InconsistentNaming
        internal FourCC FourCC { get; }

        /// <summary>
        /// Name of the Codec
        /// </summary>
        public string Name { get; }

        // ReSharper disable once InconsistentNaming
        internal WebRTCCodec(FourCC FourCC, string Name)
        {
            this.FourCC = FourCC;
            this.Name = Name;
        }
        
        /// <summary>
        /// Quality of the encoded Video... 0 to 100 (default is 70) (Not supported by all Codecs). 
        /// </summary>
        public int Quality { get; set; } = 70;

        /// <summary>Identifier used for non-compressed data.</summary>
        public static WebRTCCodec Uncompressed { get; } = new WebRTCCodec(new FourCC(0), "Uncompressed");
    }
}

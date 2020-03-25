using SharpAvi.Codecs;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Captura.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class WebRTCWriterProvider : IVideoWriterProvider
    {
        public string Name => "WebRTC";

        public IEnumerator<IVideoWriterItem> GetEnumerator()
        {
            yield return new WebRTCItem(WebRTCCodec.Uncompressed, "Uncompressed WebRTC");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => Name;

        public IVideoWriterItem ParseCli(string Cli)
        {
            var writers = this.ToArray();
            return writers[0];
        }

        public string Description => "Encode Avi videos using SharpAvi.";
    }
}
using Microsoft.MixedReality.WebRTC;
using System.Collections.Generic;

namespace Captura.Models.WebRTC
{
    public static class PeerConnectionConfig
    {
        public static readonly PeerConnectionConfiguration Default = new PeerConnectionConfiguration
        {
            IceServers = new List<IceServer>() 
            {
                new IceServer  
                {
                    Urls = 
                    {
                        "stun:stun.l.google.com:19302",
                        "stun:stun1.l.google.com:19302",
                        "stun:stun2.l.google.com:19302",
                        "stun:stun3.l.google.com:19302",
                        "stun:stun4.l.google.com:19302",
                        "stun:stun.stunprotocol.org:3478",
                    }
                }
            }
        };
    }
}

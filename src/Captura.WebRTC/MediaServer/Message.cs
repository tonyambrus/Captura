using System;

namespace Captura.Models.WebRTC
{
    [Serializable]
    public class Message
    {
        /// <summary>
        /// Possible message types as-serialized on the wire
        /// </summary>
        public enum WireMessageType
        {
            Unknown = 0,
            // An SDP message initializing a webrtc connection.
            Offer,
            // An SDP mesage finalizing a webrtc connection.
            Answer,
            // ICE message for NAT handling.
            Ice,
            // A request to a known server for a connection.
            Request,
            // A response from a known server with a unique connection id.
            Response,
        }

        /// <summary>
        /// Convert a message type from <see xref="string"/> to <see cref="WireMessageType"/>.
        /// </summary>
        /// <param name="stringType">The message type as <see xref="string"/>.</param>
        /// <returns>The message type as a <see cref="WireMessageType"/> object.</returns>
        public static WireMessageType WireMessageTypeFromString(string stringType)
        {
            if (string.Equals(stringType, "offer", StringComparison.OrdinalIgnoreCase))
            {
                return WireMessageType.Offer;
            }
            else if (string.Equals(stringType, "answer", StringComparison.OrdinalIgnoreCase))
            {
                return WireMessageType.Answer;
            }
            throw new ArgumentException($"Unkown signaler message type '{stringType}'");
        }

        /// <summary>
        /// The message type
        /// </summary>
        public int MessageType;

        /// <summary>
        /// The primary message contents
        /// </summary>
        public string Data;

        /// <summary>
        /// The data separator needed for proper ICE serialization
        /// </summary>
        public string IceDataSeparator;
    }
}

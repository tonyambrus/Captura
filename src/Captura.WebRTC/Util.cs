using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Captura.Models.WebRTC
{
    public static class Util
    {
        public static string PrettyPrint(string message, int maxLength = int.MaxValue)
        {
            message = message.Substring(0, Math.Min(message.Length, maxLength));
            return Regex.Replace(message, "[\n\r\t ]+", " ");
        }

        public static void WriteLine(string text)
        {
#if DEBUG
            Debug.WriteLine(text);
#endif
        }
    }
}

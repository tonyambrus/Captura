using System.Windows.Media.Imaging;

namespace ScreenShare
{
    public partial class ScreenShareView
    {
        public void SetImage(BitmapImage bitmap)
        {
            qrCode.Source = bitmap;
        }
    }
}

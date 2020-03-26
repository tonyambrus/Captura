namespace Captura
{
    public class WebRTCSettings : PropertyStore
    {
        public int Port
        {
            get => Get<int>(8090);
            set => Set(value);
        }
    }
}
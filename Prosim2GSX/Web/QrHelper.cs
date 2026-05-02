using QRCoder;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Prosim2GSX.Web
{
    // QR-code rendering for the App Settings "Web Interface" panel. Uses
    // QRCoder's PngByteQRCode (no System.Drawing dependency) so the resulting
    // PNG bytes can be wrapped in a frozen BitmapImage for WPF binding.
    public static class QrHelper
    {
        // pixelsPerModule sizes the rendered PNG — 6 yields ~ 200 px for a
        // typical URL+token payload, fits comfortably in the App Settings
        // section without dominating the layout.
        public static ImageSource Generate(string text, int pixelsPerModule = 6)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
                var png = new PngByteQRCode(data);
                byte[] bytes = png.GetGraphic(pixelsPerModule);

                var bitmap = new BitmapImage();
                using var ms = new MemoryStream(bytes);
                bitmap.BeginInit();
                // OnLoad so the bitmap doesn't keep a reference to ms.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                // Freeze so the image can cross threads (some property paths
                // hit getters off the UI thread during dispatch).
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}

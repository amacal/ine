using ine.Domain;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ine.Extensions
{
    public static class ImageExtensions
    {
        public static ImageSource ToBitmap(this Captcha captcha)
        {
            BitmapImage image = new BitmapImage();

            using (var memory = new MemoryStream(captcha.Data))
            {
                memory.Seek(0, SeekOrigin.Begin);

                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = memory;
                image.EndInit();
                image.Freeze();
            }

            return image;
        }
    }
}

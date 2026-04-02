using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace MangaReader.ViewCommon;

public class Utils
{
    public static void DisposeImageSource(Image image)
    {
        if (image.Source is Bitmap bitmap)
        {
            image.Source = null;
            bitmap.Dispose();
        }
    }    
}
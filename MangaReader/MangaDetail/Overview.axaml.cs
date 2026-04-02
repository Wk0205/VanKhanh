using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MangaReader.DomainCommon;

namespace MangaReader.MangaDetail;

public partial class Overview : UserControl, IDisposable
{
    private readonly CancellationTokenSource cts = new();
    public Overview()
    {
        InitializeComponent();
    }

    public Overview(Http http, string title, int chapterNumber, string description, string coverUrl) : this()
    {
        this.TitleTextBlock.Text = title;
        if (chapterNumber == 0)
        {
            this.ChapterNumberTextBlock.Text = "This manga is banned";
            this.ChapterNumberTextBlock.Foreground = Brushes.White;
            this.ChapterNumberTextBlock.Background = Brushes.DeepPink;
            this.ChapterNumberTextBlock.Padding = new Thickness(5);
        }
        else
        {
            this.ChapterNumberTextBlock.Text = chapterNumber + " chapters";
        }
        this.DescriptionTextBlock.Text = description;
        this.LoadCover(http, coverUrl);
    }

    private async void LoadCover(Http http, string url)
    {
        var token = cts.Token;
        try
        {
            var data = await http.GetBytesAsync(url, token);
            using var stream = new MemoryStream(data);
            
            var oldImage = this.CoverImage.Source as IDisposable;
            this.CoverImage.Source = new Bitmap(stream);
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                oldImage.Dispose();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
        catch (Exception)
        {
            var oldImage = this.CoverImage.Source as IDisposable;
            
            this.Border.Background = Brushes.DeepPink;
            
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                oldImage.Dispose();
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    public void Dispose()
    {
        cts.Cancel();
        cts.Dispose();
        ViewCommon.Utils.DisposeImageSource(this.CoverImage);
    }
}
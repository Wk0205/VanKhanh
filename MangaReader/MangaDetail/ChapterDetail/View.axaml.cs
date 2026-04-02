using System;
using System.IO;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using MangaReader.DomainCommon;

namespace MangaReader.MangaDetail.ChapterDetail;

public partial class View: UserControl, IView, IDisposable
{
    private readonly Presenter? presenter;
    private bool isDisposed =  false;
    
    public View()
    {
        InitializeComponent();
    }

    public View(string chapterUrl, Http http) : this()
    {
        var domain = new Domain(chapterUrl, http);
        presenter = new Presenter(domain, this);

        this.ErrorPanel.RetryButton.Click += (s, e) => presenter.Retry();
    }

    public void ShowLoadingChapter()
    {
        this.ProgressBar.IsVisible = true;
        this.ErrorPanel.IsVisible = false;
        this.MainContent.IsVisible = false;
    }

    public void ShowChapterContent(Chapter chapter)
    {
        this.ProgressBar.IsVisible = false;
        this.ErrorPanel.IsVisible = false;
        this.MainContent.IsVisible = true;
        this.TitleTextBlock.Text = chapter.Title;
        this.PageListStackPanel.Children.Clear();
        foreach (var _ in chapter.PageUrls)
        {
            var border = new Border
            {
                Background = Brushes.Silver,
                MinHeight = 40,
                Opacity = 0.1
            };
            this.PageListStackPanel.Children.Add(border);
        }
    }

    public void ShowErrorPanel(string message)
    {
        this.ProgressBar.IsVisible = false;
        this.ErrorPanel.IsVisible = true;
        this.MainContent.IsVisible = false;
        this.ErrorPanel.MessageTextBlock.Text = message;
    }

    public void ShowPage(int index, byte[]? imageData)
    {
        if (isDisposed) return;
        var errorBackground = Brushes.DeepPink;
        var border = (this.PageListStackPanel.Children[index] as Border)!;
        border.Opacity = 1;
        if (imageData == null)
        {
            border.Background = errorBackground;
            return;
        }
        using var stream = new MemoryStream(imageData);
        try
        {
            border.Child = new Image { Source = new Bitmap(stream) };
        }
        catch (Exception)
        {
            border.Background = errorBackground;
        }
    }

    public double GetPageViewportWidth()
    {
        return this.ScrollViewer.Bounds.Width;
    }

    public void SetPagePanelWidth(double value)
    {
        this.PageListStackPanel.Width = value;
    }

    public void SetTitleAndZoomPanelVisible(bool value)
    {
        this.TitleTextBlock.IsVisible = value;
        this.ZoomPanel.IsVisible = value;
    }

    public void ToggleTitleAndZoomPanelVisible()
    {
        this.TitleTextBlock.IsVisible = !this.TitleTextBlock.IsVisible;
        this.ZoomPanel.IsVisible = !this.ZoomPanel.IsVisible;
    }

    private void PlusButton_OnClick(object? sender, RoutedEventArgs e)
    {
        presenter?.ZoomIn();
    }

    private void MinusButton_OnClick(object? sender, RoutedEventArgs e)
    {
        presenter?.ZoomOut();
    }

    private void FitButton_OnClick(object? sender, RoutedEventArgs e)
    {
        presenter?.ZoomFit();
    }

    public void Dispose()
    {
        presenter?.Dispose();
        foreach (var control in this.PageListStackPanel.Children)
        {
            var border = (control as Border)!;
            if (border.Child is Image image)
            {
                ViewCommon.Utils.DisposeImageSource(image);
            }
        }
        isDisposed = true;
    }

    private void PageListStackPanel_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        presenter?.OnPagePanelClicked();
    }

    private void PageListStackPanel_OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        presenter?.OnPagePanelScrolled(e.Delta.X,  e.Delta.Y);
    }

    private void ScrollViewer_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        presenter?.OnPageViewportWidthChanged();
    }
}
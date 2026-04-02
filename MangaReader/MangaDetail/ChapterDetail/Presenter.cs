using System;
using System.Collections.Generic;
using System.Threading;
using MangaReader.DomainCommon;

namespace MangaReader.MangaDetail.ChapterDetail;

internal enum State
{
    Loading,
    Error,
    Success,
    Disposed
}

public class Presenter : IDisposable
{
    private readonly Domain domain;
    private readonly IView view;
    
    private State state;

    private double zoomFactor = 1.0;
    private CancellationTokenSource? cts;

    public Presenter(Domain domain, IView view)
    {
        this.domain = domain;
        this.view = view;
        this.Load();
    }
    
    private async void Load()
    {
        state = State.Loading;
        view.ShowLoadingChapter();
        cts = new CancellationTokenSource();
        try
        {
            var chapter = await domain.LoadChapters(cts.Token);
            view.ShowChapterContent(chapter);
            state = State.Success;
            cts.Dispose();
            cts = new CancellationTokenSource();
            this.LoadPages(chapter.PageUrls, cts.Token);
        }
        catch (NetworkException ex)
        {
            view.ShowErrorPanel(ex.Message);
            state = State.Error;
        }
        catch (ParseException)
        {
            view.ShowErrorPanel("Oops! Something went wrong");
            state = State.Error;
        }
        catch (OperationCanceledException)
        {
            state = State.Disposed;
        }
    }

    private async void LoadPages(IReadOnlyList<string> urls, CancellationToken token)
    {
        for (var index = 0; index < urls.Count; index++)
        {
            byte[]? imageData;
            try
            {
                imageData = await domain.LoadBytes(urls[index], token);
            }
            catch (NetworkException)
            {
                imageData = null;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            view.ShowPage(index, imageData);
        }    
    }

    public void Retry()
    {
        if (state != State.Error) return;
        this.Load();
    }

    public void ZoomIn()
    {
        zoomFactor += 0.1;
        view.SetPagePanelWidth(view.GetPageViewportWidth()*zoomFactor);
    }

    public void ZoomOut()
    {
        zoomFactor -= 0.1;
        if (zoomFactor < 0.1) zoomFactor = 0.1;
        view.SetPagePanelWidth(view.GetPageViewportWidth()*zoomFactor);
    }

    public void ZoomFit()
    {
        zoomFactor = 1.0;
        view.SetPagePanelWidth(view.GetPageViewportWidth());
    }

    public void OnPageViewportWidthChanged()
    {
        view.SetPagePanelWidth(view.GetPageViewportWidth() * zoomFactor);
    }

    public void OnPagePanelScrolled(double deltaX, double deltaY)
    {
        if (Math.Abs(deltaX) >= Math.Abs(deltaY)) return;
        if (deltaX < 0)
        {
            view.SetTitleAndZoomPanelVisible(false);
        }
        else if (deltaX > 0)
        {
            view.SetTitleAndZoomPanelVisible(true);
        }
    }

    public void OnPagePanelClicked()
    {
        view.ToggleTitleAndZoomPanelVisible();
    }

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
        state = State.Disposed;
    }
    
}   
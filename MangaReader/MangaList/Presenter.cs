using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using MangaReader.DomainCommon;
using MangaReader.MangaList;

namespace MangaReader.MangaList;

public class Presenter
{
    private readonly Domain domain;
    private CancellationTokenSource? cts;
    private readonly IView? view;
    private Task? task;
    private int currentPageIndex = 1;
    private int totalPageNumber = 1;
    private bool isLoading;
    private MangaList? list;

    public Presenter(Domain domain,  IView? view)
    {
        this.domain = domain;
        this.view = view;
        view.SetFavoritesMangas(domain.GetFavoritesMangaTitles());
        this.Load();
    }

    private void ShowLoading()
    {
        view?.SetLoadingVisible(true);
        view?.SetErrorPanelVisible(false);
        view?.SetMainContentVisible(false);
    }

    private void ShowError(string errorMessage)
    {
        view?.SetLoadingVisible(false);
        view?.SetMainContentVisible(false);
        view?.SetErrorMessage(errorMessage);
        view?.SetErrorPanelVisible(true);
    }

    private void ShowNoManga()
    {
        view?.SetTotalMangaNumber(text: "No manga");
        view?.SetCurrentPageButtonContent("No page");
        view?.SetCurrentPageButtonEnabled(false);
        view?.SetFirstButtonAndPrevButtonEnabled(false);
        view?.SetLastButtonAndNextButtonEnabled(false);
        view?.SetListBoxContent(Enumerable.Empty<Item>());
        view?.SetLoadingVisible(false);
        view?.SetMainContentVisible(true);
        view?.SetErrorPanelVisible(false);
    }

    private void ShowMangaList(MangaList list)
    {
        view?.SetTotalMangaNumber(text: list.TotalMangaNumber + " mangas");
        view?.SetFirstButtonAndPrevButtonEnabled(currentPageIndex > 1);
        view?.SetCurrentPageButtonContent("Page " + currentPageIndex + " of " + list.TotalPageNumber);
        view?.SetCurrentPageButtonEnabled(true);
        view?.SetNumericUpDownValue(currentPageIndex);
        view?.SetNumericUpDownMaximum(list.TotalPageNumber);
        view?.SetLastButtonAndNextButtonEnabled(currentPageIndex < list.TotalPageNumber);
        
        view?.SetListBoxContent(
            list.CurrentPage.Select(manga =>
                

                     new Item(title: manga.Title,
                        category: manga.Category,
                        status: manga.Status,
                        isFavorites: domain.IsFavoritesManga(manga.MangaUrl))
                )
            );
        
        view?.SetMainContentVisible(true);
        view?.SetErrorPanelVisible(false);
        view?.SetLoadingVisible(false);
    }

    public async void Load()
    {
        if (isLoading) return;
        isLoading = true;
        
        try 
        {
            this.ShowLoading();

            if (cts != null)
            {
                cts.Cancel();
                if (task != null)
                {
try { await task; }
                    catch (Exception) { } 
                    task = null;
                }
                cts = null;
            }

            list = null;
            string? errorMessage = null;
            
            try 
            { 
                list = await domain.LoadMangaList(currentPageIndex, view?.GetFilterText() ?? ""); 
            }
            catch (NetworkException ex) { errorMessage = "Network error: " + ex.Message; }
            catch (ParseException) { errorMessage = "Oops! Something went wrong."; }
            catch (Exception ex) { errorMessage = "Lỗi không xác định: " + ex.Message; } 

            if (list == null)
                this.ShowError(errorMessage ?? "Lỗi tải dữ liệu");
            else if (list.TotalMangaNumber <= 0 || list.TotalPageNumber <= 0)
                this.ShowNoManga();
            else
            {
                totalPageNumber = list.TotalPageNumber;
                if (currentPageIndex > totalPageNumber) currentPageIndex = totalPageNumber;
                this.ShowMangaList(list);
                
                cts = new CancellationTokenSource();
                var coverUrls = list.CurrentPage.Select(manga => manga.CoverUrl); 
                task = this.LoadCovers(coverUrls, cts.Token);
            }
        }
        finally 
        {
         
            isLoading = false;
        }
    }

    private async Task LoadCovers(IEnumerable<string> urls, CancellationToken token)
    {
        var index = -1;
        foreach (var url in urls)
        {
            index++;
            byte[]? bytes;
            try { bytes = await domain.LoadBytes(url, token); }
            catch (NetworkException) { bytes = null; }
            if (token.IsCancellationRequested) break;
            view.SetCover(index, bytes);
        }
    }

    public void GoNextPage()
    {
        if (isLoading || currentPageIndex >= totalPageNumber) return;
        currentPageIndex++;
        view.SetNumericUpDownValue(currentPageIndex);
        this.Load();
    }

    public void GoPrevPage()
    {
        if (isLoading || currentPageIndex <= 1) return;
        currentPageIndex--;
        view.SetNumericUpDownValue(currentPageIndex);
        this.Load();
    }

    public void GoFirstPage()
    {
        if (isLoading || currentPageIndex <= 1) return;
        currentPageIndex = 1;
        view.SetNumericUpDownValue(currentPageIndex);
        this.Load();
    }

    public void GoLastPage()
    {
        if (isLoading || currentPageIndex >= totalPageNumber) return;
        currentPageIndex = totalPageNumber;
        view.SetNumericUpDownValue(currentPageIndex);
        this.Load();
    }

    public void GoSpecificPage()
    {
        if (isLoading ) return;
        view.HideFlyout();
        var pageIndex = view.GetNumericUpDownValue();
        if (pageIndex < 1 || pageIndex > totalPageNumber) return;
        currentPageIndex = pageIndex;
        this.Load();
    }
public void ApplyFilter()
    {
        currentPageIndex = 1;
        this.Load();
    }

    public void SelectManga(int index)
    {
        if (list == null) return;
        if (index < 0 || index >= list.CurrentPage.Count) return;
        var mangaUrl = list.CurrentPage[index].MangaUrl;
        view.OpenMangaDetail(mangaUrl);
    }

    public void SelectFavoritesManga(int index)
    {
        var mangaUrl = domain.GetFavoritesMangaUrl(index);
        if (mangaUrl != null)
        {
            view.OpenMangaDetail(mangaUrl);
        }
    }

    public void ToggleFavoritesManga(int index)
    {
        if (list == null) return;
        if (index < 0 || index >= list.CurrentPage.Count) return;
        var manga = list.CurrentPage[index];
        if (domain.IsFavoritesManga(manga.MangaUrl))
        {
            domain.RemoveFavoritesManga(manga.MangaUrl);
            view.UpdateFavoritesManga(index, false);
        }
        else
        {
            domain.AddFavoritesManga(manga.MangaUrl, manga.Title);
            view.UpdateFavoritesManga(index, true);
        }
        view.SetFavoritesMangas(domain.GetFavoritesMangaTitles());
    }
    public void ClearFavoritesManga()
    {
        domain.ClearFavoritesManga();
        view?.SetFavoritesMangas(domain.GetFavoritesMangaTitles());
        view?.ClearAllFavoriteIcons();
    }
}
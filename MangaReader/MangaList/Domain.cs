using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MangaReader.DomainCommon;
using System.Linq;
using HtmlAgilityPack;
using Fizzler.Systems.HtmlAgilityPack;
using MangaReader2026.MangaList;

namespace MangaReader.MangaList;

public class Manga
{
    public string Title { get; init; }
    public string Category { get; init; }
    public string CoverUrl { get; init; }
    public string Status { get; init; }
    public string MangaUrl { get; init; }

    public Manga(string title, string category, string coverUrl, string status, string mangaUrl)
    {
        Title = title;
        Category = category;
        CoverUrl = coverUrl;
        Status = status;
        MangaUrl = mangaUrl;
    }
}

public class FavoritesManga
{
    public string Url { get; }
    public string Title { get; }

    public FavoritesManga(string url, string title)
    {
        Url = url;
        Title = title;
    }
}

public class MangaList
{
    public int TotalMangaNumber { get; init; }
    public int TotalPageNumber { get; init; }
    public List<Manga> CurrentPage { get; init; }

    public MangaList(int totalMangaNumber, int totalPageNumber, List<Manga> currentPage)
    {
        TotalMangaNumber = totalMangaNumber;
        TotalPageNumber = totalPageNumber;
        CurrentPage = currentPage;
    }
}

public class Domain
{
    private readonly string baseUrl;
    private readonly Http http;
    private readonly List<FavoritesManga> favoriteMangas;
    private readonly IDb db;

    public Domain(string baseUrl, Http http, IDb db)
    {
        this.baseUrl = baseUrl;
        this.http = http;
        this.db = db;
        this.favoriteMangas = db.LoadFavoritesMangas();
    }
   
    private void SortFavoriteMangas()
    {
        favoriteMangas.Sort((manga1, manga2) =>
            string.Compare(manga1.Title, manga2.Title, StringComparison.OrdinalIgnoreCase)
        );
    }
    private Task<string> DownloadHtml(int page, string filterText)
    {
        if (page < 1) page = 1;
        
        string url = $"{baseUrl}/truyen-tranh?page={page}&q={filterText}";
        
        return http.GetStringAsync(url);
    }

    private List<Manga> ParseMangaList(HtmlNode section)
    {
        var mangas = new List<Manga>();
        var bookNodes = section.QuerySelectorAll("article").ToArray();

        foreach (var node in bookNodes)
        {
            var linkNode = node.SelectSingleNode(".//a[contains(@class,'manga-link')]");
            var titleNode = node.SelectSingleNode(".//h3[contains(@class,'manga-title')]");
            var imageNode = node.SelectSingleNode(".//div[contains(@class,'cover-bg')]");
            var chapterNode = node.SelectSingleNode(".//span[contains(@class,'pill')]");
            
            var title = titleNode?.InnerText.Trim();
            var status = chapterNode?.InnerText.Trim() + " Chương";
            var category = "Đang Cập Nhật";
            
            var rawCoverStyle = imageNode?.GetAttributeValue("Style", "") ?? "";
            string coverUrl = "";
            
            if (!string.IsNullOrEmpty(rawCoverStyle))
            {
                var startIndex = rawCoverStyle.IndexOf("/");
                var endIndex = rawCoverStyle.IndexOf("');");
                
                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    coverUrl = baseUrl + rawCoverStyle.Substring(startIndex, endIndex - startIndex);
                }
            }

            var mangaUrl = linkNode?.GetAttributeValue("href", "");
            var manga = new Manga(title, category, coverUrl, status, mangaUrl);
            mangas.Add(manga);
        }

        return mangas;
    }

    private MangaList Parse(string html)
    {
        try
        {
            var xmlStartAt = html.IndexOf("");
            var xmlEndAt = html.IndexOf("");
            
            if (xmlStartAt != -1 && xmlEndAt != -1 && xmlEndAt > xmlStartAt)
            {
                html = html.Substring(xmlStartAt, xmlEndAt - xmlStartAt);
            }
            
            File.WriteAllText("Page.html", html);
            var doc = new HtmlDocument();
            doc.LoadHtml("<html> </head> <body> <main> <div>" + html + "</div> </main> </body> </html>");

            var section = doc.DocumentNode;
            if(section == null)
                return new MangaList(0, 0, new List<Manga>());
            
            var pageLinks = section.SelectNodes("//ul[contains(@class,'pagination')]//a[contains(@class,'page-link')]");
            
            var safePageLinks = pageLinks ?? Enumerable.Empty<HtmlNode>();
            
            var pageNumbers = safePageLinks
                .Select(a => a.InnerText.Trim())
                .Where(text => int.TryParse(text, out var _))
                .Select(int.Parse)
                .ToList();
            
            var totalPages = pageNumbers.Any() ? pageNumbers.Max() : 1;
            
            return new MangaList(totalPages * 24, totalPages, ParseMangaList(section));
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw new ParseException();
        }
    }
    public IEnumerable<string> GetFavoritesMangaTitles()
    {
        return favoriteMangas.Select(favoriteManga => favoriteManga.Title);
    }

    public string? GetFavoritesMangaUrl(int index)
    {
        if (index < 0 || index > favoriteMangas.Count)
            return null;
        return favoriteMangas[index].Url;
    }

    public bool IsFavoritesManga(string mangaUrl)
    {
        return favoriteMangas.Exists(manga => manga.Url == mangaUrl);
    }

    public void AddFavoritesManga(string url, string title)
    {
        if (IsFavoritesManga(url)) return;
        var manga = new FavoritesManga(url, title);
        favoriteMangas.Add(manga);
        SortFavoriteMangas();
        db.InsertFavoritesManga(manga);
    }

    public void RemoveFavoritesManga(string url)
    {
        favoriteMangas.RemoveAll(manga => manga.Url == url);
        db.DeleteFavoritesManga(url);
    }

    public void ClearFavoritesManga()
    {
        favoriteMangas.Clear();
        db.ClearFavoritesManga();
    }
    
    
    public async Task<MangaList> LoadMangaList(int page, string filterText = "")
    {
        var html = await this.DownloadHtml(page, filterText);
        return Parse(html);
    }

    public Task<byte[]> LoadBytes(string url, CancellationToken token)
    {
        return http.GetBytesAsync(url, token);
    }
}
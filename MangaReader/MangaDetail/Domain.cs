using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MangaReader.DomainCommon;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using MangaReader.ViewCommon;

namespace MangaReader.MangaDetail;

public class Chapter
{
    public string Title { get; }
    public string Url { get; }

    public Chapter(string title, string url)
    {
        Title = title;
        Url = url;
    }
}

public class Manga
{
    public string Title { get; }
    public string Description { get; }
    public string CoverUrl { get; }
    public List<Chapter> Chapters { get; }

    public Manga(string title, string description, string coverUrl, List<Chapter> chapters)
    {
        Title = title;
        Description = description;
        CoverUrl = coverUrl;
        Chapters = chapters;
    }
}

public class Domain
{
    private readonly string mangaUrl;
    private readonly Http http;

    public Domain(string mangaUrl, Http http)
    {
        this.mangaUrl = mangaUrl;
        this.http = http;
    }

    public async Task<Manga> LoadManga(CancellationToken token)
    {
        var html = await http.GetStringAsync(mangaUrl, token);
        File.WriteAllText("Before.html", html);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var docNode = doc.DocumentNode;
        var tNode = docNode.QuerySelector("head > title");
        var metaNodes = docNode.QuerySelectorAll("head > meta").ToArray();

        string ParseTitle()
        {
            string title = Html.Decode(tNode.InnerText.Trim());
            Console.WriteLine(title);
            return title;
        }

        string ParseDescription()
        {
            var desMeta = metaNodes.FirstOrDefault(meta => meta.Attributes["name"]?.Value == "description");
            if (desMeta == null) return "Không có mô tả.";
            
            var desc = Html.Decode(desMeta.Attributes["content"].Value);
            Console.WriteLine(desc);
            return desc;
        }

        string ParseCoverUrl()
        {
            // Dùng FirstOrDefault để an toàn hơn
            var imgMeta = metaNodes.FirstOrDefault(meta => meta.Attributes["property"]?.Value == "og:image");
            if (imgMeta == null) return ""; // Trả về chuỗi rỗng nếu không có ảnh

            var img = Html.Decode(imgMeta.Attributes["content"].Value);

            // FIX LỖI ẢNH BÌA: Chuẩn hóa URL
            if (img.StartsWith("//"))
            {
                img = "https:" + img; // Bổ sung giao thức
            }
            else if (img.StartsWith("/") && !img.StartsWith("//"))
            {
                // Xử lý nếu url chỉ bắt đầu bằng 1 dấu '/'
                var uri = new Uri(mangaUrl);
                img = $"{uri.Scheme}://{uri.Host}{img}";
            }

            Console.WriteLine(img);
            return img;
        }

        List<Chapter> ParseChapters()
        {
            var chapterNodes = docNode.QuerySelector(
                "body > main > div > section.manga-detail > div.manga-grid " +
                "> div.manga-main > section.chapters > div.chapter-table > div.chapter-body");
            var list = new List<Chapter>();
            
            if (chapterNodes != null)
            {
                foreach (var chapterNode in chapterNodes.QuerySelectorAll("a.chapter-row"))
                {
                    var title = chapterNode.QuerySelector("span.chapter-title").InnerText.Trim();
                    var url = chapterNode.Attributes["href"].Value;
                    var chapter = new Chapter(title, url);
                    Console.WriteLine($"Add chapter: {title}:{url}");
                    list.Add(chapter);
                }
            }
            Console.WriteLine("Finished loading chapters");
            return list;
        }

        try
        {
            return new Manga(
                title: ParseTitle(),
                description: ParseDescription(),
                coverUrl: ParseCoverUrl(),
                chapters: ParseChapters()
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"Parse Error: {e.Message}");
            throw new ParseException();
        }
    }
}
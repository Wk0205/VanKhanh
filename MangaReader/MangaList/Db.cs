using System.Collections.Generic;
using System.Linq;
using LiteDB;
using MangaReader2026.MangaList;

namespace MangaReader.MangaList;

public class FavoritesMangaDto
{
    [BsonId] public string Url { get; init; } = null!;
    public string Title { get; init; } = null!;
}

public class Db : IDb
{
    private readonly string dbFile;

    public Db(string dbFile)
    {
        this.dbFile = dbFile;
    }

    private ILiteCollection<FavoritesMangaDto> GetFavoritesMangasCollection(ILiteDatabase db)
    {
        return db.GetCollection<FavoritesMangaDto>("FavoritesMangas");
    }
    public List<FavoritesManga> LoadFavoritesMangas()
    {
        using var db = new LiteDatabase(dbFile);
        // var a = this.GetFavoritesMangasCollection(db);
        // var b = a.FindAll();
        // // var c = b.Select(dto => new FavoritesManga(dto.Url, dto.Title)).ToList();
        // // var d = c.ToList();
        // return new List<FavoritesManga>();
        return this.GetFavoritesMangasCollection(db) // ILiteCollection<FavoritesMangaDto>
            .FindAll() // IEnumerable<FavoritesMangaDto>
            .Select(dto => new FavoritesManga(dto.Url, dto.Title)) // IEnumerable<FavoritesManga>
            .ToList(); // List<FavoritesManga>
    }

    public void InsertFavoritesManga(FavoritesManga manga)
    {
        using var db = new LiteDatabase(dbFile);
        this.GetFavoritesMangasCollection(db) // ILiteCollection<FavoritesMangaDto>
            .Insert(new FavoritesMangaDto { Url = manga.Url, Title = manga.Title });
    }

    public void DeleteFavoritesManga(string url)
    {
        using var db = new LiteDatabase(dbFile);
        this.GetFavoritesMangasCollection(db).Delete(url);
    }

    public void ClearFavoritesManga()
    {
        using var db = new LiteDatabase(dbFile);
        this.GetFavoritesMangasCollection(db).DeleteAll();
    }
}
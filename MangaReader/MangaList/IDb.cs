using System.Collections.Generic;
using MangaReader.MangaList;

namespace MangaReader2026.MangaList;

public interface IDb
{
    List<FavoritesManga> LoadFavoritesMangas();
    void InsertFavoritesManga(FavoritesManga manga);
    void DeleteFavoritesManga(string url);
    public void ClearFavoritesManga();
}
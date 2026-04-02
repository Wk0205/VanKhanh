namespace MangaReader.MangaDetail.ChapterDetail;

public interface IView
{
    void ShowLoadingChapter();
    void ShowChapterContent(Chapter chapter);
    void ShowErrorPanel(string message);
    void ShowPage(int index, byte[]? imageData);
    double GetPageViewportWidth();
    void SetPagePanelWidth(double value);
    void SetTitleAndZoomPanelVisible(bool value);
    void ToggleTitleAndZoomPanelVisible();
    
}
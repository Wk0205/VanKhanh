using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using MangaReader.DomainCommon;

namespace MangaReader.MangaList;

public partial class View : Window, IView
{
    private readonly Presenter? presenter;
    private readonly Http? http;
    private readonly List<ItemControl> itemControls = new();

    public View()
    {
        InitializeComponent();
        UpdateThemeMenuChecks(ThemeVariant.Default);
    }

    public View(string baseUrl, Http http, string dbFile): this () 
    {
       this.http = http;
       var db = new Db(dbFile);
       var domain = new Domain(baseUrl, http, db);
       presenter = new Presenter(domain, this);
       this.ErrorPanel.RetryButton.Click += (sender, args) => presenter.Load();
    }

    // ✅ SỬA Ở ĐÂY: dùng Header thay vì IsChecked
    private void UpdateThemeMenuChecks(ThemeVariant theme)
    {
        DefaultThemeMenuItem.Header = theme == ThemeVariant.Default ? "✓ Default" : "Default";
        LightThemeMenuItem.Header = theme == ThemeVariant.Light ? "✓ Light" : "Light";
        DarkThemeMenuItem.Header = theme == ThemeVariant.Dark ? "✓ Dark" : "Dark";
    }

    public void SetLoadingVisible(bool value)
    {
        this.LoadingProgressBar.IsVisible = value;
    }

    public void SetErrorPanelVisible(bool value)
    {
        this.ErrorPanel.IsVisible = value;
    }

    public void SetMainContentVisible(bool value)
    {
        this.MainContent.IsVisible = value;
    }

    public void SetTotalMangaNumber(string text)
    {
        this.TotalMangaNumberLabel.Content = text;
    }

    public void SetCurrentPageButtonContent(string content)
    {
        this.CurrentPageButton.Content = content;
    }

    public void SetCurrentPageButtonEnabled(bool value)
    {
        this.CurrentPageButton.IsEnabled = value;
    }

    public void SetNumericUpDownMaximum(int value)
    {
        this.NumericUpDown.Maximum = value;
    }

    public void SetNumericUpDownValue(int value)
    {
        this.NumericUpDown.Value = value;
    }

    public int GetNumericUpDownValue()
    {
        return (int)this.NumericUpDown.Value!;
    }
    
    public string? GetFilterText()
    {
        return this.FilterTextBox.Text?.Trim() ?? string.Empty;
    }

    public void SetListBoxContent(IEnumerable<Item> items)
    {
        this.MangaListBox.Items.Clear();
        foreach (var itemControl in itemControls)
        {
            ViewCommon.Utils.DisposeImageSource(itemControl.CoverImage);
        }
        itemControls.Clear();
        var index = -1;
        foreach (var item in items)
        {
            index++;
            var itemControl = new ItemControl
            {
                Title = item.Title,
                Status = item.Status,
                CoverToolTip = item.ToolTip,
                IsFavorites = item.IsFavorites
            };
            var i = index;
            itemControl.FavoritesButton.Click += (sender, args) => presenter?.ToggleFavoritesManga(i);
            itemControls.Add(itemControl);
            this.MangaListBox.Items.Add(new ListBoxItem { Content = itemControl });
        }

        FavoritesMenuItem.Items.Add(new Separator());
        var itemClear = new MenuItem {Header = "Clear"};
        itemClear.Click += (sender, args) => presenter?.ClearFavoritesManga();
        FavoritesMenuItem.Items.Add(itemClear);
    }

    public void SetCover(int index, byte[]? bytes)
    {
        var errorCoverBackground = Brushes.DeepPink;
        var itemControl = itemControls[index];
        if (bytes == null)
        {
            itemControl.CoverBorder.Background = errorCoverBackground;
            return;
        }

        using var ms = new MemoryStream(bytes);
        try
        {
            itemControl.CoverImage.Source = new Bitmap(ms);
        }
        catch (Exception)
        {
            itemControl.CoverBorder.Background = errorCoverBackground;
        }
    }

    public void SetFirstButtonAndPrevButtonEnabled(bool value)
    {
        this.FirstButton.IsEnabled = value;
        this.PrevButton.IsEnabled = value;
    }

    public void SetLastButtonAndNextButtonEnabled(bool value)
    {
        this.LastButton.IsEnabled = value;
        this.NextButton.IsEnabled = value;
    }

    public void HideFlyout()
    {
        this.CurrentPageButton.Flyout?.Hide();
    }

    public void SetErrorMessage(string text)
    {
        this.ErrorPanel.MessageTextBlock.Text = text;
    }

    private void MyListBox_OnDoubleTapped(object sender, TappedEventArgs e)
    {
       presenter?.SelectManga(this.MangaListBox.SelectedIndex);
    }

    private void MyListBox_OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            presenter?.SelectManga(this.MangaListBox.SelectedIndex);
        }
    }
    
    private void PrevButton_OnClick(object sender, RoutedEventArgs e) => presenter?.GoPrevPage();
    private void NextButton_OnClick(object sender, RoutedEventArgs e) => presenter?.GoNextPage();
    private void FirstButton_OnClick(object sender, RoutedEventArgs e) => presenter?.GoFirstPage();
    private void LastButton_OnClick(object sender, RoutedEventArgs e) => presenter?.GoLastPage();
    private void GoButton_OnClick(object sender, RoutedEventArgs e) => presenter?.GoSpecificPage();

    private void NumericUpDown_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        presenter?.GoSpecificPage();
    }
    
    private void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        this.FilterTextBox.Text = string.Empty;
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        presenter?.ApplyFilter();
    }

    private void FilterTextBox_OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            presenter?.ApplyFilter();
        }
    }

    private void DefaultTheme_Click(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeVariant.Default;
        UpdateThemeMenuChecks(ThemeVariant.Default);
    }

    private void LightTheme_Click(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeVariant.Light;
        UpdateThemeMenuChecks(ThemeVariant.Light);
    }

    private void DarkTheme_Click(object? sender, RoutedEventArgs e)
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        UpdateThemeMenuChecks(ThemeVariant.Dark);
    }

    public void SetFavoritesMangas(IEnumerable<string> mangaTitles)
    {
        this.FavoritesMenuItem.Items.Clear();
        var index = -1;
        foreach (var title in mangaTitles)
        {
            index++;
            var item = new MenuItem {Header = title};
            var i = index;
            item.Click += (sender, args) => presenter?.SelectFavoritesManga(i);
            this.FavoritesMenuItem.Items.Add(item);
        }
    }

    public void UpdateFavoritesManga(int index, bool value)
    {
        itemControls[index].IsFavorites = value;
    }

    public void OpenMangaDetail(string mangaUrl)
    {
        var detailWindow = new MangaReader.MangaDetail.View(mangaUrl, this.http!);
        detailWindow.Show();
    }

    public void ClearAllFavoriteIcons()
    {
        foreach (var itemControl in itemControls)
        {
            itemControl.IsFavorites = false;
        }
    }
}
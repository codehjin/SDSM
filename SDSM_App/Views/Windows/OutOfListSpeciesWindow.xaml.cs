using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SDSM_App.Enums;
using SDSM_App.Helpers;
using SDSM_App.InjectableServices;

namespace SDSM_App.Views.Windows;

/// <summary>
/// 일괄등록 목록 밖 종 확인 창(2026-10-22 §22-3). 고른 처리(<see cref="Result"/>)만 돌려준다.
/// 창을 닫기(X)로 닫으면 취소.
/// </summary>
public partial class OutOfListSpeciesWindow : Window, ITransientService
{
    public OutOfListSpeciesWindow()
    {
        InitializeComponent();
    }

    public OutOfListAction Result { get; private set; } = OutOfListAction.Cancel;

    public void SetContent(string taxon, IReadOnlyList<OutOfListSpecies> species)
    {
        HeaderTextBlock.Text = $"{taxon} — 종목록에 없는 종 {species.Count}종({species.Sum(s => s.Rows):N0}행)";
        SpeciesGridView.ItemsSource = species;
    }

    /// <summary>창을 띄워 처리를 묻는다(주 창이 있으면 그 가운데).</summary>
    public static OutOfListAction Ask(string taxon, IReadOnlyList<OutOfListSpecies> species)
    {
        var window = App.Current.Services.GetRequiredService<OutOfListSpeciesWindow>();
        var owner = Application.Current.MainWindow;
        if (owner != null && owner.IsVisible && !ReferenceEquals(owner, window))
            window.Owner = owner;
        else
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

        window.SetContent(taxon, species);
        window.ShowDialog();
        return window.Result;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => Close(OutOfListAction.AddToList);

    private void SaveOnlyButton_Click(object sender, RoutedEventArgs e) => Close(OutOfListAction.SaveOnly);

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close(OutOfListAction.Cancel);

    private void Close(OutOfListAction action)
    {
        Result = action;
        DialogResult = true;
    }
}

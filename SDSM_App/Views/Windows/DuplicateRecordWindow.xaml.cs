using System.Windows;
using SDSM_App.Enums;
using SDSM_App.InjectableServices;

namespace SDSM_App.Views.Windows;

/// <summary>
/// 일괄등록 중복 확인 창. 고른 처리(<see cref="Result"/>)와 '나머지도 같게'(<see cref="ApplyToRest"/>)만 돌려준다.
/// 창을 닫기(X)로 닫으면 기본값 건너뛰기.
/// </summary>
public partial class DuplicateRecordWindow : Window, ITransientService
{
    public DuplicateRecordWindow()
    {
        InitializeComponent();
    }

    public DuplicateAction Result { get; private set; } = DuplicateAction.Skip;

    public bool ApplyToRest => ApplyToRestCheckBox.IsChecked == true;

    public void SetContent(string header, string newText, string existingText, string? note = null)
    {
        HeaderTextBlock.Text = header;
        NewTextBlock.Text = newText;
        ExistingTextBlock.Text = existingText;

        //하천 표기 차이 등 — 있을 때만 보인다(2026-10-21 N4)
        NoteTextBlock.Text = note ?? string.Empty;
        NoteBorder.Visibility = string.IsNullOrWhiteSpace(note) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e) => Close(DuplicateAction.Skip);

    private void ReplaceButton_Click(object sender, RoutedEventArgs e) => Close(DuplicateAction.Replace);

    private void AddButton_Click(object sender, RoutedEventArgs e) => Close(DuplicateAction.Add);

    private void Close(DuplicateAction action)
    {
        Result = action;
        DialogResult = true;
    }
}

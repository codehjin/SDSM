using FMRRC_App.Helpers;
using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using SDSM_Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 생물조사 식생단면
/// </summary>
public partial class VegetationENVControl : UserControl, ITransientService
{
    private readonly VegetationENVControlViewModel _vm;

    public VegetationENVControl(VegetationENVControlViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        Loaded += UserControl_Loaded;
        SizeChanged += UserControl_SizeChanged;
    }

    #region This 이벤트

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (App.Current.Services.GetService(typeof(VegetationENVEditorControl)) is VegetationENVEditorControl editor)
        {
            editor.CloseClick += (s, e) =>
            {
                EditorLayout.Visibility = Visibility.Collapsed;
            };
            EditorContentControl.Content = editor;
        }
    }

    private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var height = e.NewSize.Height - (SearchLayout.ActualHeight + 160);

        if (height > 60)
        {
            DataRadGridView.Height = height;
        }
        else
        {
            DataRadGridView.Height = 60;
        }
    }

    #endregion

    #region Control 이벤트    

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            if (e.Delta > 0)
                scrollViewer.LineLeft();
            else
                scrollViewer.LineRight();
        }
    }


    //초기화
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SearchText = string.Empty;
        SearchQuery();
    }

    //검색
    private void SearchBox_QuerySubmitted(object sender, Telerik.Windows.Controls.AutoSuggestBox.QuerySubmittedEventArgs e)
    {
        SearchQuery();
    }

    //검색
    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SearchQuery();
        }
    }

    //삭제
    private void DeleteRadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadButton button && button.Tag is string tag)
        {
            List<VegetationENVView> deleteData = [];
            if (tag == "selection")
            {
                deleteData = [.. DataRadGridView.SelectedItems.Cast<VegetationENVView>()];
            }
            else if (tag == "page")
            {
                deleteData = [.. DataRadGridView.Items.Cast<VegetationENVView>()];
            }
            _vm.DeleteFormVisibleExecutedCommand.Execute(deleteData);
        }
    }

    //자료 출력
    private void DataExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataRadGridView.Items.Count == 0) return;

        DataExportHelper.ExportGridViewToExcel(
            DataRadGridView,
            "식생단면_환경",
            progress => DataRadGridView.BusyIndicatorProgressValue = progress,
            () =>
            {
                //추가 완료 처리 필요시 구현 
                // UI는 콜백에서 변경

                DataRadGridView.BusyIndicatorProgressValue = 0;
            },
            showColumnGroups: true
        );
    }

    #endregion

    #region 메서드

    //검색
    private void SearchQuery()
    {
        DataRadGridView.FilterDescriptors.SuspendNotifications();
        foreach (Telerik.Windows.Controls.GridViewColumn column in DataRadGridView.Columns)
        {
            column.ClearFilters();
        }
        DataRadGridView.FilterDescriptors.ResumeNotifications();

        DataRadGridView.SortDescriptors.Clear();

        _vm.SearchExecutedCommand.Execute(null);
    }

    #endregion
}

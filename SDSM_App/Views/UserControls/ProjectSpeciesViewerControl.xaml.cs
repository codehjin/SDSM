using SDSM_App.InjectableServices;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 통계 사업장 출현종
/// </summary>
public partial class ProjectSpeciesViewerControl : UserControl, ITransientService
{
    private readonly Style? _cellLeftStyle = Application.Current.FindResource("GridViewCellLeftStyle") as Style;

    public ProjectSpeciesViewerControl()
    {
        InitializeComponent();

        SizeChanged += Page_SizeChanged;
    }

    //Default Foreground
    public DataTable DataDataTable
    {
        get { return (DataTable)GetValue(DataDataTableProperty); }
        set { SetValue(DataDataTableProperty, value); }
    }
    public static readonly DependencyProperty DataDataTableProperty = DependencyProperty.Register("DataDataTable", typeof(DataTable), typeof(ProjectSpeciesViewerControl), new PropertyMetadata(DataChangedCallBack));

    public event RoutedEventHandler CloseClick
    {
        add { AddHandler(CloseClickEvent, value); }
        remove { RemoveHandler(CloseClickEvent, value); }
    }
    public static readonly RoutedEvent CloseClickEvent = EventManager.RegisterRoutedEvent("CloseClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ProjectSpeciesViewerControl));

    private void CloseButton_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(CloseClickEvent));

    #region This 이벤트

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var height = e.NewSize.Height - 205;

        if (height > 110)
        {
            DataTableRadGridView.Height = height;
        }
        else
        {
            DataTableRadGridView.Height = 110;
        }
    }

    #endregion

    #region Control 이벤트

    private void DataTableRadGridView_AutoGeneratingColumn(object sender, GridViewAutoGeneratingColumnEventArgs e)
    {
        var column = (GridViewDataColumn)e.Column;

        if (column != null)
        {
            switch (column.Header.ToString())
            {
                case "사업장":
                    column.MinWidth = 80;
                    column.MaxWidth = 100;
                    break;
                case "과업대표하천":
                    column.MinWidth = 80;
                    column.MaxWidth = 120;
                    break;
                default:
                    var header = column.Header.ToString() ?? "";
                    if (header.Contains('_'))
                    {
                        //컬럼 그룹
                        column.ColumnGroupName = header[..(header.IndexOf('_'))];

                        // 컬럼명
                        column.Header = header[(header.IndexOf('_') + 1)..];
                    }

                    if (column.Header.ToString() == "종수")
                    {
                        column.MinWidth = 80;
                        column.Width = GridViewLength.Auto;
                        column.MaxWidth = 80;
                        column.DataType = typeof(string);
                    }
                    else
                    {
                        column.MinWidth = 80;
                        column.Width = GridViewLength.Auto;
                        column.MaxWidth = 420;
                        column.TextWrapping = TextWrapping.Wrap;
                        if (_cellLeftStyle != null)
                            column.CellStyle = _cellLeftStyle;
                        column.DataType = typeof(string);
                    }
                    break;
            }
        }
    }

    #endregion

    #region 메서드

    private static void DataChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProjectSpeciesViewerControl control)
        {
            try
            {
                control.SetProjectColumns();
            }
            catch { }
        }
    }

    //사업장 과업대표하천 컬럼그룹 동적생성
    private void SetProjectColumns()
    {
        var dt = DataDataTable;
        if (dt == null || dt.Rows.Count == 0)
            return;

        var headers = new List<string>();

        foreach (DataColumn column in dt.Columns)
        {
            var header = column.ColumnName ?? "";
            if (header.Contains('_'))
            {
                // "_" 다음 문자열만 남김
                headers.Add(header[..(header.IndexOf('_'))]);
            }
        }

        //사업장 출현종
        DataTableRadGridView.ColumnGroups.SuspendNotifications();

        DataTableRadGridView.ColumnGroups.Clear();

        foreach (var header in headers.Distinct())
        {
            DataTableRadGridView.ColumnGroups.Add(new GridViewColumnGroup { Name = header, Header = header });
        }

        DataTableRadGridView.ColumnGroups.ResumeNotifications();
    }

    #endregion
}

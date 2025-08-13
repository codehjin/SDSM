using SDSM_App.InjectableServices;
using SDSM_App.Models;
using SDSM_App.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Resources;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Map;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 홈
/// 주제도 7~15
/// 지도 7~14
/// </summary>
public partial class HomePage : Page, ITransientService
{
    private readonly HomePageViewModel _vm;

    private DirectoryInfo _mbDirectory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mbtiles"));
    private RadCallout _clickCallout = new();
    private readonly CalloutPopupSettings _popupSettings;
    private FrameworkElement? _pointTarget;

    public HomePage(HomePageViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        Loaded += Page_Loaded;
        Unloaded += Page_Unloaded;
        radMap.Loaded += RadMap_Loaded;
        radMap.ZoomChanged += RadMap_ZoomChanged;
        radMap.CenterChanged += RadMap_CenterChanged;

        _popupSettings = new CalloutPopupSettings()
        {
            AutoClose = false,
            ShowAnimationDuration = 0.25d,
            CloseAnimationDuration = 0.25d,
            ShowAnimationType = CalloutAnimation.FadeAndMove,
            CloseAnimationType = CalloutAnimation.FadeAndMove,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
            CanPopupExceedScreen = true,
        };

        //위치클릭 팝업
        if (TryFindResource("MapPinCalloutStyle") is Style clickStyle)
        {
            _clickCallout = new RadCallout() { Style = clickStyle };
            _clickCallout.SetBinding(ContentProperty, new Binding());
        }
    }

    #region This 이벤트

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mbDirectory.Exists)
        {
            var osFile = _mbDirectory.EnumerateFiles("*.*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false })
                                     .FirstOrDefault(file => file.Name.Equals("os_1-6.mbtiles", StringComparison.OrdinalIgnoreCase));
            if (osFile == null)
            {
                Uri osUri = new("/SDSM_App;component/Assets/MBTiles/os_1-6.mbtiles", UriKind.Relative);
                StreamResourceInfo? streamInfo = Application.GetResourceStream(osUri);

                if (streamInfo != null)
                {
                    using Stream stream = streamInfo.Stream;
                    if (stream != null)
                    {
                        FileStream fileStream = new(Path.Combine(_mbDirectory.FullName, "os_1-6.mbtiles"), FileMode.CreateNew);
                        for (int i = 0; i < stream.Length; i++)
                        {
                            fileStream.WriteByte((byte)stream.ReadByte());
                        }
                        fileStream.Close();
                    }
                }
            }
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        CalloutPopupService.CloseAll();
        radMap.Dispose();
    }

    #endregion

    #region Control 이벤트

    //지도 초기화
    private void RadMap_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mbDirectory.Exists)
        {
            AddMbTilesProvider("os_1-6");
            AddMbTilesProvider("vworld_road");
        }
    }

    //지도 레이어 변경
    private void MapLayerRadRadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadRadioButton button && button.Tag is string tag)
        {
            if (tag == "vworld_road")
            {
                RemoveMbTilesProvider("vworld_hybrid");
            }
            else
            {
                RemoveMbTilesProvider("vworld_road");
            }
            AddMbTilesProvider(tag);
        }
    }

    //포인트 클릭
    private void Point_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is MapModel item)
        {
            if (_clickCallout != null && _clickCallout.DataContext is MapModel openPoint)
            {
                if (item.Location.ToString() != openPoint.Location.ToString())
                {
                    MapPinCalloutPopupOpen(border, item);
                }
                else
                {
                    MapPinCalloutPopupClose();
                }
            }
            else
            {
                MapPinCalloutPopupOpen(border, item);
            }
        }
    }

    //콜팝업 닫기
    private void MapPinCalloutCloseRadButton_Click(object sender, RoutedEventArgs e)
    {
        MapPinCalloutPopupClose();
    }

    //조사포인트 검색
    private void RadAutoCompleteBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MapPinCalloutPopupClose();
        _vm.SearchValueSelectionChangedExecutedCommand.Execute(null);
    }

    //좌표 변경 창 열기
    private void LatLngChangeViewOpenRadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadButton button && button.DataContext is MapModel mapModel)
        {
            _vm.LatLngChangeViewOpenExectedCommand.Execute(mapModel);

            MapPinCalloutPopupClose();
        }
    }

    //좌표 변경 목록 선택 
    private void LatLngChangeRadGridView_SelectionChanged(object sender, SelectionChangeEventArgs e)
    {
        if(sender is RadGridView gridView)
        {
            _vm.LatLngChangeSelectedItems.Clear();
            _vm.LatLngChangeSelectedItems.AddRange(gridView.SelectedItems.Cast<MapData>());
            _vm.LatLngChangeSelectionCount = _vm.LatLngChangeSelectedItems.Count;
        }
    }

    //복사
    private async void AllCopyRadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadButton button && button.Tag is Tuple<object, object> tuple)
        {
            try
            {
                button.IsEnabled = false;

                if (tuple.Item1 is RadGridView gridView && tuple.Item2 is TextBlock copyTextBlock)
                {
                    var selectedItem = gridView.SelectedItem ?? null;
                    gridView.SelectionMode = SelectionMode.Multiple;
                    gridView.SelectAll();

                    var copyCommand = RadGridViewCommands.Copy as RoutedUICommand;
                    copyCommand?.Execute(null, gridView);

                    gridView.UnselectAll();
                    gridView.SelectionMode = SelectionMode.Single;
                    gridView.SelectedItem = selectedItem;

                    copyTextBlock.Text = "목록 복사됨";
                    await Task.Delay(500);
                    copyTextBlock.Text = "목록 복사";
                }
                ;
            }
            catch (Exception ex)
            {
                GB.LogWrite(GetType().Name, nameof(AllCopyRadButton_Click), ex.Message);
            }
            finally
            {
                button.IsEnabled = true;
            }
        }
    }

    //주제도 전체선택해제
    private void AllThemeMapUnCheckedRadButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in _vm.ThemeMapCollection)
        {
            item.IsSelected = false;
            item.IsVisibleLegend = false;
        }

        RemoveMbTilesProvider("주제도");
    }

    //주제도 이미지

    private void ThemeMapCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ContentItem item)
        {
            AddMbTilesProvider(item.MbTilesName, item.OpacityName);
        }
    }

    //주제도 이미지
    private void ThemeMapCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.DataContext is ContentItem item)
        {
            RemoveMbTilesProvider(item.MbTilesName);
        }
    }

    #endregion

    #region 클러스터 이벤트

    private void ClusterMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            if (element.DataContext is ClusterData cluster)
            {
                //너비와 높이 음수 에러 처리
                try
                {
                    LocationRect bestViewRect = PointVisualizationLayer.GetBestView(cluster.Children);
                    radMap.SetView(bestViewRect);
                }
                catch { }
            }
        }

        e.Handled = true;
    }

    private void ClusterMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            if (element != null && element.DataContext is ClusterData data)
            {
                if (data != null)
                {
                    if (data.ClusterState != ClusterState.ExpandedToPolygon)
                    {
                        data.HideExpanded = false;
                        data.ClusterState = ClusterState.ExpandedToPolygon;
                    }
                    else
                    {
                        data.ClusterState = ClusterState.Collapsed;
                    }
                }
            }
            e.Handled = true;
        }
    }

    #endregion    

    #region Map

    private void RadMap_ZoomChanged(object? sender, EventArgs e)
    {
        MapPinCalloutPopupClose();
    }

    private void RadMap_CenterChanged(object? sender, EventArgs e)
    {
        MapPinCalloutPopupClose();
    }

    //클릭 좌표
    private void RadMap_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var mousePostion = Mouse.GetPosition(radMap);
        var location = Location.GetCoordinates(radMap, mousePostion);

        var degress = $"{location.Latitude:F6}, {location.Longitude:F6}";
        _vm.Degress = degress;

        if (_vm.LatLngChangeViewVisibility == Visibility.Visible)
        {
            _vm.AfterLat = Math.Round(location.Latitude, 6, MidpointRounding.AwayFromZero);
            _vm.AfterLng = Math.Round(location.Longitude, 6, MidpointRounding.AwayFromZero);
        }
    }

    //클릭좌표 복사
    private void CopyRadMenuItem_Click(object sender, Telerik.Windows.RadRoutedEventArgs e)
    {
        // 클립보드에 복사
        Clipboard.SetText(_vm.Degress);
    }

    //확대축소
    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.Zoom = (int)radMap.Zoom + 1;
    }
    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.Zoom = (int)(radMap.Zoom + 0.99) - 1;
    }

    //zomm 이동시만 클러스터 반응함 : 선택장소 초기화
    private void ZommMoveGesture()
    {
        var currentZoom = _vm.Zoom;
        if (currentZoom <= 3)
        {
            _vm.Zoom = (int)radMap.Zoom + 1;
        }
        else
        {
            _vm.Zoom = (int)(radMap.Zoom + 0.99) - 1;
        }
        _vm.Zoom = currentZoom;
    }

    private void AddMbTilesProvider(string division, string opacityName = "")
    {
        if (division == "os_1-6")
        {
            var osFile = Path.Combine($"{_mbDirectory.FullName}", "os_1-6.mbtiles");
            if (File.Exists(osFile))
            {
                var mbTilesProvider = new MbTilesMapProvider
                {
                    Source = new Uri(osFile, UriKind.RelativeOrAbsolute),
                };

                radMap.Providers.Insert(1, mbTilesProvider);
            }
        }
        else if (division == "vworld_road" || division == "vworld_hybrid")
        {
            var vwFile = Path.Combine($"{_mbDirectory.FullName}", $"{division}.mbtiles");
            if (File.Exists(vwFile))
            {
                var mbTilesProvider = new MbTilesMapProvider
                {
                    Source = new Uri(vwFile, UriKind.RelativeOrAbsolute),
                };

                radMap.Providers.Insert(2, mbTilesProvider);
            }
        }
        else
        {
            var opacityBinding = new Binding(opacityName);

            var file = Path.Combine($"{_mbDirectory.FullName}", $"{division}.mbtiles");
            if (File.Exists(file))
            {
                var mbTilesProvider = new MbTilesMapProvider
                {
                    Source = new Uri(file, UriKind.RelativeOrAbsolute),
                };

                if (opacityBinding != null)
                {
                    BindingOperations.SetBinding(mbTilesProvider, MbTilesMapProvider.OpacityProperty, opacityBinding);
                }

                radMap.Providers.Add(mbTilesProvider);
            }
        }
        ZommMoveGesture();
    }

    //MbTilesProvider 제거
    private void RemoveMbTilesProvider(string division)
    {
        var providers = radMap.Providers.Where(m => m.GetType() == typeof(MbTilesMapProvider)).ToList();
        if (providers.Count > 0)
        {
            foreach (var provider in providers)
            {
                if (provider is MbTilesMapProvider mapProvider)
                {
                    if (mapProvider.Source.OriginalString.Contains(division))
                    {
                        radMap.Providers.Remove(provider);
                    }
                }
            }
            ZommMoveGesture();
        }
    }

    #endregion

    #region 메서드

    //팝업 열기
    private void MapPinCalloutPopupOpen(FrameworkElement placementTarget, object item)
    {
        if (_clickCallout != null)
        {
            _clickCallout.DataContext = item;
            _pointTarget = placementTarget;

            _vm.PopupPointsExecutedCommand.Execute(item);

            CalloutPopupService.Show(_clickCallout, _pointTarget, _popupSettings);
        }
    }

    //팝업 닫기
    private void MapPinCalloutPopupClose()
    {
        CalloutPopupService.CloseAll();
        _clickCallout.DataContext = null;
    }

    #endregion
}

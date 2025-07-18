using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Resources;
using System.Windows.Threading;
using Telerik.Charting;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Map;

namespace SDSM_App.Views.Pages;

/// <summary>
/// 홈
/// </summary>
public partial class HomePage : Page, ITransientService
{
    private readonly HomePageViewModel _vm;

    private DirectoryInfo _mbDirectory = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mbtiles"));

    public HomePage(HomePageViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        Loaded += Page_Loaded;
        Unloaded += Page_Unloaded;
        radMap.Loaded += RadMap_Loaded;
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

    #endregion

    #region Control 이벤트

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        radMap.Dispose();
    }

    //지도 초기화
    private void RadMap_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mbDirectory.Exists)
        {
            AddMbTilesProvider("os_1-6");
            AddMbTilesProvider("vworld_road");
            //AddMbTilesProvider("map");
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

    private void RadMap_ZoomChanged(object sender, EventArgs e)
    {
        //if (_vm != null && _vm.IsCheckedLabelVisible)
        //{
        //    if (_vm.Zoom < 16)
        //    {
        //        foreach (var point in _vm.PointCollection)
        //        {
        //            point.IsVisibleLabel = false;
        //        }
        //    }
        //    else
        //    {
        //        foreach (var point in _vm.PointCollection)
        //        {
        //            point.IsVisibleLabel = true;
        //        }
        //    }
        //}
    }

    //클릭 좌표
    private void RadMap_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var mousePostion = Mouse.GetPosition(radMap);
        var location = Location.GetCoordinates(radMap, mousePostion);
        var degress = $"{location.Latitude:F6}, {location.Longitude:F6}";

        _vm.Degress = degress;
       
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

    private void AddMbTilesProvider(string division)
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
            
        }
        ZommMoveGesture();
    }

    //MbTilesProvider 단일제거
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

    //MbTilesProvider 일괄제거
    private bool RemoveAllMbTilesProvider()
    {
        //if (string.IsNullOrEmpty(_beforeKnpName))
        //    return true;

        //var providers = radMap.Providers.Where(m => m.GetType() == typeof(MbTilesMapProvider)).ToList();
        //if (providers.Count > 0)
        //{
        //    foreach (var provider in providers)
        //    {
        //        if (provider is MbTilesMapProvider mapProvider)
        //        {
        //            if (mapProvider.Source.OriginalString.Contains(_beforeKnpName))
        //            {
        //                radMap.Providers.Remove(provider);
        //            }
        //        }
        //    }
        //    ZommMoveGesture();
        //}

        return true;
    }

    #endregion   
}

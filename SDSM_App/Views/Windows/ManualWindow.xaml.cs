using System.IO;
using System.Windows;
using Telerik.Windows.Documents.Fixed;
using SDSM_App.InjectableServices;
using System.Windows.Media.Animation;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.Windows;

/// <summary>
/// 사용자 메뉴얼
/// </summary>
public partial class ManualWindow : Window, ITransientService
{
    public ManualWindow()
    {
        InitializeComponent();

        StateChanged += MainWindow_StateChanged;

        Loaded += Window_Loaded;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Stream stream = App.GetResourceStream(new Uri("/SDSM_App;component/Assets/Pdfs/manual.pdf", UriKind.Relative)).Stream;
            pdfViewer.DocumentSource = new PdfDocumentSource(stream);
            pdfViewer.DocumentSource.Loaded += DocumentSource_Loaded;
        }
        catch(IOException ex)
        {
            GB.LogWrite(GetType().Name, nameof(Window_Loaded), ex.Message);

            MessageBox.Show("파일 열기 중 오류가 발생했습니다.", "파일 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            GB.LogWrite(GetType().Name, nameof(Window_Loaded), ex.Message);

            MessageBox.Show("파일 열기 중 오류가 발생했습니다.", "파일 오류", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DocumentSource_Loaded(object? sender, EventArgs e)
    {
        //PDFViewer 로딩표시
    }

    #region window

    //minimized button
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    //maximized button
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState != WindowState.Normal)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    //close button
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    //WindowStyle None때문
    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        HandleWinMaximized();
    }

    private void HandleWinMaximized()
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
        }
    }

    #endregion

    #region Pdf Thumbnail

    public double ColumnWidth
    {
        get { return (double)GetValue(ColumnWidthProperty); }
        set { SetValue(ColumnWidthProperty, value); }
    }

    public static readonly DependencyProperty ColumnWidthProperty =
            DependencyProperty.Register("ColumnWidth", typeof(double), typeof(ManualWindow), new PropertyMetadata(ColumnWidthChanged));

    private static void ColumnWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var window = d as ManualWindow;
        if(window != null)
            window.thumbnailColumn.Width = new GridLength((double)e.NewValue);
    }

    private void ThumbnailRadToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.ThumbnailRadToggleButton.IsChecked == true && this.Resources["ThumbnailExpand"] is Storyboard expand)
        {
            expand.Begin();
        }
        if (this.ThumbnailRadToggleButton.IsChecked == false && this.Resources["ThumbnailCollapse"] is Storyboard collapse)
        {
            collapse.Begin();
            UncheckButton(this.ThumbnailRadToggleButton);
        }
    }

    private void UncheckButton(RadToggleButton btn)
    {
        if (btn.IsChecked == true)
        {
            btn.IsChecked = false;
        }

    }

    #endregion
}

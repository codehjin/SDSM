using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 서식수변 작성폼
/// </summary>
public partial class HabitatWaterEdgeEditorControl : UserControl, ITransientService
{
    private readonly HabitatWaterEdgeEditorControlViewModel _vm;

    protected bool isDragging;
    private Point clickPosition;

    public HabitatWaterEdgeEditorControl(HabitatWaterEdgeEditorControlViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        DataContext = _vm;

        SizeChanged += Control_SizeChanged;
        clickPosition = new();
        MouseLeftButtonDown += new MouseButtonEventHandler(Control_MouseLeftButtonDown);
        MouseLeftButtonUp += new MouseButtonEventHandler(Control_MouseLeftButtonUp);
        MouseMove += new MouseEventHandler(Control_MouseMove);
    }

    public event RoutedEventHandler CloseClick
    {
        add { AddHandler(CloseClickEvent, value); }
        remove { RemoveHandler(CloseClickEvent, value); }
    }
    public static readonly RoutedEvent CloseClickEvent = EventManager.RegisterRoutedEvent("CloseClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(HabitatWaterEdgeEditorControl));

    private void CloseButton_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(CloseClickEvent));

    #region TextBox Event

    //공백제거
    private void EmptyClear_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is RadWatermarkTextBox watermarkTextBox)
        {
            if (!string.IsNullOrWhiteSpace(watermarkTextBox.Text))
            {
                watermarkTextBox.Text = watermarkTextBox.Text.Replace(" ", string.Empty);
            }
            else
            {
                watermarkTextBox.Text = null;
            }
        }
        else if (sender is TextBox textBox)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Text.Replace(" ", string.Empty);
            }
            else
            {
                textBox.Text = null;
            }
        }
    }

    //앞뒤공백제거
    private void Trim_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is RadWatermarkTextBox watermarkTextBox)
        {
            if (!string.IsNullOrWhiteSpace(watermarkTextBox.Text))
            {
                watermarkTextBox.Text = watermarkTextBox.Text.Trim();
            }
            else
            {
                watermarkTextBox.Text = null;
            }
        }
        else if (sender is TextBox textBox)
        {
            if (!string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Text.Trim();
            }
            else
            {
                textBox.Text = null;
            }
        }
    }

    #endregion

    #region Drag

    private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (RenderTransform != null)
        {
            RenderTransform = null;
        }
    }

    private void Control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is UserControl control)
        {
            isDragging = true;
            clickPosition = e.GetPosition(this);
            control.CaptureMouse();
        }
    }

    private void Control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is UserControl control)
        {
            isDragging = false;
            control.ReleaseMouseCapture();
        }
    }

    private void Control_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is UserControl control)
        {
            if (isDragging && control != null)
            {
                Point currentPosition = e.GetPosition(Parent as UIElement);

                if (control.RenderTransform is not TranslateTransform transform)
                {
                    transform = new TranslateTransform();
                    control.RenderTransform = transform;
                }

                transform.X = currentPosition.X - clickPosition.X;
                transform.Y = currentPosition.Y - clickPosition.Y;
            }
        }
    }

    #endregion

    private void Upper_LostFocus(object sender, RoutedEventArgs e)
    {
        if(sender is RadWatermarkTextBox textBox)
        {
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = textBox.Text.ToUpper();
            }
        }
    }
}

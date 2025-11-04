using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 식생도 작성폼
/// </summary>
public partial class VegetationEditorControl : UserControl, ITransientService
{
    private readonly VegetationEditorControlViewModel _vm;

    protected bool isDragging;
    private Point clickPosition;

    public VegetationEditorControl(VegetationEditorControlViewModel vm)
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
    public static readonly RoutedEvent CloseClickEvent = EventManager.RegisterRoutedEvent("CloseClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VegetationEditorControl));

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

    //실수값 소수점 1자리
    private void DoubleValueToRound_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is RadNumericUpDown upDown && upDown.Value != null)
        {
            if (double.TryParse(upDown.Value.ToString(), out double value))
            {
                upDown.Value = Math.Round(value, upDown.NumberDecimalDigits, MidpointRounding.AwayFromZero);
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
}

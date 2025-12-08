using SDSM_App.InjectableServices;
using SDSM_App.ViewModels;
using SDSM_Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Telerik.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 식생단면 작성폼
/// </summary>
public partial class VegetationENVEditorControl : UserControl, ITransientService
{
    private readonly VegetationENVEditorControlViewModel _vm;

    protected bool isDragging;
    private Point clickPosition;

    public VegetationENVEditorControl(VegetationENVEditorControlViewModel vm)
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
    public static readonly RoutedEvent CloseClickEvent = EventManager.RegisterRoutedEvent("CloseClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VegetationENVEditorControl));

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

    //실수값 소수점 자리맞춤
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

    #region Control 이벤트

    //출현종 수정 및 삭제
    private void RadButton_PreviewMouseDown(object sender,MouseButtonEventArgs e)
    {
        if(sender is RadButton button && button.Tag is string mode)
        {
            if(mode == "edit")
            {
                _vm.SpeciesEditExecutedCommand.Execute(button.DataContext);
            }
            else
            {
                _vm.SpeciesRemoveExecutedCommand.Execute(button.DataContext);
            }

            e.Handled = true;
        }
    }

    //출현종 수정
    private void SpeciesEditSaveRadButton_Click(object sender, RoutedEventArgs e)
    {
        var model = _vm.SpeciesCollection.FirstOrDefault(m => m == _vm.SpeciesSelectedItem);
        if (model != null)
        {
            model.Layer = _vm.EditSpeciesLayer;
            model.SpeciesKo = _vm.EditSpeciesSpeciesKo;
            model.CoverDegree = _vm.EditSpeciesCoverDegree;

            SpeciesRadGridView.Rebind();

            _vm.SpeciesEditorCollapsedExecutedCommand.Execute(null);
        }
    }

    private void SpeciesAddRadButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.SpeciesCollection.Insert(0, new VegetationENVSpecies
        {
            Layer = _vm.SpeciesLayer,
            SpeciesKo = _vm.SpeciesSpeciesKo,
            CoverDegree = _vm.SpeciesCoverDegree,
        });

        _vm.SpeciesLayer = string.Empty;
        _vm.SpeciesSpeciesKo = string.Empty;
        _vm.SpeciesCoverDegree = null;

        SpeciesLayerRadComboBox.Focus();
    }

    #endregion
}

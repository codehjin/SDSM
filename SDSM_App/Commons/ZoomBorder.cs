using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SDSM_App.Commons;

public class ZoomBorder : Border
{
    public static List<UIElement>? SubChilds { get; set; }

    private UIElement child = null!;

    private Point origin;
    private Point start;

    private TranslateTransform GetTranslateTransform(UIElement element)
    {
        return (TranslateTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is TranslateTransform);
    }

    public ScaleTransform GetScaleTransform(UIElement element)
    {
        return (ScaleTransform)((TransformGroup)element.RenderTransform).Children.First(tr => tr is ScaleTransform);
    }

    public override UIElement Child
    {
        get { return base.Child; }
        set
        {
            if (value != null && value != Child)
                Initialize(value);
            base.Child = value;
        }
    }

    public void Initialize(UIElement element)
    {
        child = element;
        if (child != null)
        {
            TransformGroup group = new();
            ScaleTransform st = new();
            group.Children.Add(st);
            TranslateTransform tt = new();
            group.Children.Add(tt);
            child.RenderTransform = group;
            child.RenderTransformOrigin = new Point(0.0, 0.0);
            MouseWheel += Child_MouseWheel;
            MouseLeftButtonDown += Child_MouseLeftButtonDown;
            MouseLeftButtonUp += Child_MouseLeftButtonUp;
            MouseMove += Child_MouseMove;
            PreviewMouseRightButtonDown += new MouseButtonEventHandler(Child_PreviewMouseRightButtonDown);
        }
    }

    public void Reset()
    {
        if (child != null)
        {
            // reset zoom
            var st = GetScaleTransform(child);
            st.ScaleX = 1.0;
            st.ScaleY = 1.0;

            // reset pan
            var tt = GetTranslateTransform(child);
            tt.X = 0.0;
            tt.Y = 0.0;

            if (SubChilds != null)
            {
                foreach (var subChild in SubChilds)
                {
                    // reset zoom
                    var stSub = GetScaleTransform(subChild);
                    stSub.ScaleX = 1.0;
                    stSub.ScaleY = 1.0;

                    // reset pan
                    var ttSub = GetTranslateTransform(subChild);
                    ttSub.X = 0.0;
                    ttSub.Y = 0.0;
                }
            }
        }
    }

    #region Child Events

    private void Child_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (child != null)
        {
            var st = GetScaleTransform(child);
            var tt = GetTranslateTransform(child);

            double zoom = e.Delta > 0 ? .2 : -.2;
            if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                return;

            Point relative = e.GetPosition(child);
            double absoluteX;
            double absoluteY;

            absoluteX = relative.X * st.ScaleX + tt.X;
            absoluteY = relative.Y * st.ScaleY + tt.Y;

            st.ScaleX += zoom;
            st.ScaleY += zoom;

            tt.X = absoluteX - relative.X * st.ScaleX;
            tt.Y = absoluteY - relative.Y * st.ScaleY;

            if (SubChilds != null)
            {
                foreach (var subChild in SubChilds)
                {
                    var stSub = GetScaleTransform(subChild);
                    var ttSub = GetTranslateTransform(subChild);

                    double zoomSub = e.Delta > 0 ? .2 : -.2;
                    if (!(e.Delta > 0) && (stSub.ScaleX < .4 || stSub.ScaleY < .4))
                        return;

                    //원본객체 Position
                    Point relativeSub = e.GetPosition(child);
                    double absoluteXSub;
                    double absoluteYSub;

                    absoluteXSub = relativeSub.X * stSub.ScaleX + ttSub.X;
                    absoluteYSub = relativeSub.Y * stSub.ScaleY + ttSub.Y;

                    stSub.ScaleX += zoom;
                    stSub.ScaleY += zoom;

                    ttSub.X = absoluteX - relativeSub.X * stSub.ScaleX;
                    ttSub.Y = absoluteY - relativeSub.Y * stSub.ScaleY;
                }
            }

        }
        e.Handled = true;
    }

    private void Child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (child != null)
        {
            var tt = GetTranslateTransform(child);
            start = e.GetPosition(this);
            origin = new Point(tt.X, tt.Y);
            child.CaptureMouse();
        }
    }

    private void Child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        child?.ReleaseMouseCapture();
    }

    void Child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Reset();
    }

    private void Child_MouseMove(object sender, MouseEventArgs e)
    {
        if (child != null)
        {
            if (child.IsMouseCaptured)
            {
                var tt = GetTranslateTransform(child);
                Vector v = start - e.GetPosition(this);
                tt.X = origin.X - v.X;
                tt.Y = origin.Y - v.Y;

                if (SubChilds != null)
                {
                    foreach (var subChjild in SubChilds)
                    {
                        var ttSub = GetTranslateTransform(subChjild);
                        Vector vOther = start - e.GetPosition(this);
                        ttSub.X = origin.X - vOther.X;
                        ttSub.Y = origin.Y - vOther.Y;
                    }
                }
            }
        }
    }

    #endregion
}

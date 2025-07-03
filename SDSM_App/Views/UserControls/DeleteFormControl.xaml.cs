using System.Windows;
using System.Windows.Controls;

namespace SDSM_App.Views.UserControls;

/// <summary>
/// 삭제폼
/// </summary>
public partial class DeleteFormControl : UserControl
{
    public DeleteFormControl()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler DeleteAllClick
    {
        add { AddHandler(DeleteAllClickEvent, value); }
        remove { RemoveHandler(DeleteAllClickEvent, value); }
    }
    public static readonly RoutedEvent DeleteAllClickEvent = EventManager.RegisterRoutedEvent("DeleteAllClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DeleteFormControl));

    private void DeleteAllButton_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(DeleteAllClickEvent));
}

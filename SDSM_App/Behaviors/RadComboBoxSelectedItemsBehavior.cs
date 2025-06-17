using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using Telerik.Windows.Controls;

namespace SDSM_App.Behaviors;

public class RadComboBoxSelectedItemsBehavior : Behavior<RadComboBox>
{
    private RadComboBox ComboBox
    {
        get
        {
            return AssociatedObject as RadComboBox;
        }
    }

    public INotifyCollectionChanged SelectedItems
    {
        get { return (INotifyCollectionChanged)GetValue(SelectedItemsProperty); }
        set { SetValue(SelectedItemsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SelectedItemsProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register("SelectedItems", typeof(INotifyCollectionChanged), typeof(RadComboBoxSelectedItemsBehavior), new PropertyMetadata(OnSelectedItemsPropertyChanged));


    private static void OnSelectedItemsPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        var collection = args.NewValue as INotifyCollectionChanged;
        if (collection != null)
        {
            ((RadComboBoxSelectedItemsBehavior)target).UpdateTransfer(args.NewValue);
            collection.CollectionChanged += ((RadComboBoxSelectedItemsBehavior)target).ContextSelectedItems_CollectionChanged!;
        }
    }

    private void UpdateTransfer(object items)
    {
        Transfer((IList)items, ComboBox.SelectedItems);
        ComboBox.SelectionChanged += ComboSelectionChanged;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
    }

    void ContextSelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeFromEvents();
        Transfer((IList)SelectedItems, ComboBox.SelectedItems);
        SubscribeToEvents();
    }

    private void ComboSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UnsubscribeFromEvents();
        Transfer(ComboBox.SelectedItems, (IList)SelectedItems);
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        ComboBox.SelectionChanged += ComboSelectionChanged;
        if (SelectedItems != null)
        {
            SelectedItems.CollectionChanged += ContextSelectedItems_CollectionChanged!;
        }
    }

    private void UnsubscribeFromEvents()
    {
        ComboBox.SelectionChanged -= ComboSelectionChanged;
        if (SelectedItems != null)
        {
            SelectedItems.CollectionChanged -= ContextSelectedItems_CollectionChanged!;
        }
    }

    public static void Transfer(IList source, IList target)
    {
        if (source == null || target == null)
            return;

        target.Clear();
        foreach (var o in source)
        {
            target.Add(o);
        }
    }
}

using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using Telerik.Windows.Controls;

namespace SDSM_App.Behaviors;

public class RadGridViewMultiSelectBehavior : Behavior<RadGridView>
{
    private RadGridView Grid
    {
        get
        {
            return AssociatedObject as RadGridView;
        }
    }

    public INotifyCollectionChanged SelectedItems
    {
        get { return (INotifyCollectionChanged)GetValue(SelectedItemsProperty); }
        set { SetValue(SelectedItemsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for SelectedItemsProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedItemsProperty =
        DependencyProperty.Register("SelectedItems", typeof(INotifyCollectionChanged), typeof(RadGridViewMultiSelectBehavior), new PropertyMetadata(OnSelectedItemsPropertyChanged));


    private static void OnSelectedItemsPropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs args)
    {
        var collection = args.NewValue as INotifyCollectionChanged;
        if (collection != null)
        {
            collection.CollectionChanged += ((RadGridViewMultiSelectBehavior)target).ContextSelectedItems_CollectionChanged!;
        }
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        Grid.SelectedItems.CollectionChanged += GridSelectedItems_CollectionChanged!;
    }

    void ContextSelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeFromEvents();

        Transfer(SelectedItems as IList, Grid.SelectedItems);

        SubscribeToEvents();
    }

    void GridSelectedItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        UnsubscribeFromEvents();

        Transfer(Grid.SelectedItems, SelectedItems as IList);

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        Grid.SelectedItems.CollectionChanged += GridSelectedItems_CollectionChanged!;

        if (SelectedItems != null)
        {
            SelectedItems.CollectionChanged += ContextSelectedItems_CollectionChanged!;
        }
    }

    private void UnsubscribeFromEvents()
    {
        Grid.SelectedItems.CollectionChanged -= GridSelectedItems_CollectionChanged!;

        if (SelectedItems != null)
        {
            SelectedItems.CollectionChanged -= ContextSelectedItems_CollectionChanged!;
        }
    }

    public static void Transfer(IList? source, IList? target)
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

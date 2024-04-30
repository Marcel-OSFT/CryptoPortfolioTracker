using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace CryptoPortfolioTracker.Helpers;

public static class AncestorSource
{
    public static readonly DependencyProperty AncestorTypeProperty =
        DependencyProperty.RegisterAttached(
            "AncestorType",
            typeof(Type),
            typeof(AncestorSource),
            new PropertyMetadata(default(Type), OnAncestorTypeChanged)
    );

    public static void SetAncestorType(FrameworkElement element, Type value) =>
        element.SetValue(AncestorTypeProperty, value);

    public static Type GetAncestorType(FrameworkElement element) =>
        (Type)element.GetValue(AncestorTypeProperty);

    private static void OnAncestorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var target = (FrameworkElement)d;
        if (target.IsLoaded)
        {
            SetDataContext(target);
        }
        else
        {
            target.Loaded += OnTargetLoaded;
        }
    }

    private static void OnTargetLoaded(object sender, RoutedEventArgs e)
    {
        var target = (FrameworkElement)sender;
        target.Loaded -= OnTargetLoaded;
        SetDataContext(target);
    }

    private static void SetDataContext(FrameworkElement target)
    {
        var ancestorType = GetAncestorType(target);
        if (ancestorType != null)
        {
            target.DataContext = FindParent(target, ancestorType);
        }
    }

    private static object FindParent(DependencyObject dependencyObject, Type ancestorType)
    {
        var parent = VisualTreeHelper.GetParent(dependencyObject);
        if (parent == null)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return null;
        }
#pragma warning restore CS8603 // Possible null reference return.

        if (ancestorType.IsAssignableFrom(parent.GetType()))
        {
            return parent;
        }

        return FindParent(parent, ancestorType);
    }
}

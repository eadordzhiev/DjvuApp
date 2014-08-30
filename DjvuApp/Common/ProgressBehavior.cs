using System;
using Windows.ApplicationModel;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace DjvuApp.Common
{
    public class ProgressIndicatorBehavior : DependencyObject, IBehavior
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text",
            typeof(string),
            typeof(ProgressIndicatorBehavior),
            new PropertyMetadata(null, OnTextChanged));

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible",
            typeof(bool),
            typeof(ProgressIndicatorBehavior),
            new PropertyMetadata(false, OnIsVisibleChanged));

        public double? Value
        {
            get { return (double?)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value",
            typeof(object),
            typeof(ProgressIndicatorBehavior),
            new PropertyMetadata(null, OnValueChanged));

        public DependencyObject AssociatedObject { get; private set; }

        public void Attach(DependencyObject associatedObject)
        {
        }

        public void Detach()
        {
        }

        private static async void OnIsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled)
                return;

            bool isvisible = (bool)e.NewValue;
            if (isvisible)
            {
                await StatusBar.GetForCurrentView().ProgressIndicator.ShowAsync();
            }
            else
            {
                await StatusBar.GetForCurrentView().ProgressIndicator.HideAsync();
            }
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled)
                return;

            string text = null;
            if (e.NewValue != null)
            {
                text = e.NewValue.ToString();
            }
            StatusBar.GetForCurrentView().ProgressIndicator.Text = text;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignMode.DesignModeEnabled)
                return;

            double? val = (double?)e.NewValue;
            StatusBar.GetForCurrentView().ProgressIndicator.ProgressValue = val;
        }
    }

}

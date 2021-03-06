﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace DjvuApp.Dialogs.Internal
{
    public sealed partial class BusyIndicatorInternal : UserControl
    {
        public string TaskDescription
        {
            get { return (string)GetValue(TaskDescriptionProperty); }
            set { SetValue(TaskDescriptionProperty, value); }
        }

        public static readonly DependencyProperty TaskDescriptionProperty =
            DependencyProperty.Register("TaskDescription", typeof(string), typeof(BusyIndicatorInternal), new PropertyMetadata(null));

        private TaskCompletionSource<object> _completionSource;

        public BusyIndicatorInternal()
        {
            this.InitializeComponent();
        }

        public void OnOpen()
        {
            openingAnimation.Begin();
        }

        public async Task OnClose()
        {
            _completionSource = new TaskCompletionSource<object>();
            
            closingAnimation.Begin();

            await _completionSource.Task;
        }

        private void ClosingAnimation_Completed(object sender, object e)
        {
            _completionSource.SetResult(null);
        }
    }
}

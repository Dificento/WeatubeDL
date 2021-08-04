﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.ComponentModel;
using Weatube.Properties;

namespace Weatube
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
        }

        private void SuggestionPanel_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((sender as DockPanel).IsEnabled)
            {
                var sb = Application.Current.Resources["ShowSuggested"] as Storyboard;
                sb.Begin(sender as DockPanel);
            }
            else
            {
                var sb = Application.Current.Resources["HideSuggested"] as Storyboard;
                sb.Begin(sender as DockPanel);
            }

        }

        private void QueuePanel_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((sender as DockPanel).IsEnabled)
            {
                var sb = Application.Current.Resources["ShowPanel"] as Storyboard;
                sb.Begin(sender as DockPanel);
            }
            else
            {
                var sb = Application.Current.Resources["HidePanel"] as Storyboard;
                sb.Begin(sender as DockPanel);
            }

        }
    }
}
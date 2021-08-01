using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace Weatube
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
            Console.WriteLine("WORKING!!!!!!");
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

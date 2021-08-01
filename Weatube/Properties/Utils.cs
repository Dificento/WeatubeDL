using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Weatube.Properties
{
    static class Utils
    {
        public static double PercentFromOutput(string output)
        {
            if (output != null) Console.WriteLine(output);
            return output != null && output.Contains("%") ?
                Convert.ToDouble(output.Split('%')[0].Split(']')[1].Trim()) / 100f : 100;
        }

        public static LinearGradientBrush DownloadStateChange(double percent)
        {
            if (percent > 1)
            {
                return null;
            }
            return new LinearGradientBrush()
            {
                GradientStops =
                {
                    new GradientStop((Color)Application.Current.Resources["BaseBackgroundColor"], 0.0),
                    new GradientStop((Color)Application.Current.Resources["BaseBackgroundColor"], percent),
                    new GradientStop((Color)Application.Current.Resources["BrightBackgroundColor"], percent),
                    new GradientStop((Color)Application.Current.Resources["BrightBackgroundColor"], 1.0)
                }
            };
        }
    }
}

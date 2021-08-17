using System;
using System.Windows;
using System.Windows.Media;

namespace Weatube.Properties
{
    static class Utils
    {
        public static double PercentFromOutput(string output, string err = null)
        {
            if (err != null && err.Contains("ERROR"))
            {
                Console.WriteLine(err);
                return 404d;
            }
            return output != null && output.Contains("%") ?
                double.Parse(output.Split('%')[0].Split(']')[1].Trim(), System.Globalization.CultureInfo.InvariantCulture) / 100f : 100;
        }

        public static LinearGradientBrush DownloadStateChange(double percent)
        {
            var brush = new LinearGradientBrush();
            switch (percent)
            {
                case double p when p < 1.0:
                    brush.GradientStops = new GradientStopCollection()
                    {
                        new GradientStop((Color)Application.Current.Resources["GreenColor"], 0.0),
                        new GradientStop((Color)Application.Current.Resources["GreenColor"], percent),
                        new GradientStop(Colors.Transparent, percent),
                        new GradientStop(Colors.Transparent, 1.0)
                    };
                    break;

                case double p when p == 404d:
                    brush.GradientStops = new GradientStopCollection()
                    {
                        new GradientStop((Color)Application.Current.Resources["RedColor"], 0.0),
                        new GradientStop((Color)Application.Current.Resources["RedColor"], 1.0)
                    };
                    break;

                case double p when p == 1.0:
                    brush.GradientStops = new GradientStopCollection()
                    {
                        new GradientStop((Color)Application.Current.Resources["GreenColor"], 0.0),
                        new GradientStop((Color)Application.Current.Resources["GreenColor"], 1.0)
                    };
                    break;

                default:
                    return null;
            };
            return brush;
        }
    }
}

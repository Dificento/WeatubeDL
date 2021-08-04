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
            //if (output != null) Console.WriteLine(output);
            return output != null && output.Contains("%") ?
                Convert.ToDouble(output.Split('%')[0].Split(']')[1].Trim()) / 100f : 100;
        }

        public static LinearGradientBrush DownloadStateChange(double percent)
        {
            var brush = new LinearGradientBrush();
            switch (percent)
            {
                case double p when p < 1.0:
                    brush.GradientStops = new GradientStopCollection()
                    {
                        new GradientStop((Color)Application.Current.Resources["VideoPanelStartColor"], 0.0),
                        new GradientStop((Color)Application.Current.Resources["VideoPanelStartColor"], percent),
                        new GradientStop((Color)Application.Current.Resources["BrightBackgroundColor"], percent),
                        new GradientStop((Color)Application.Current.Resources["BrightBackgroundColor"], 1.0)
                    };
                    break;

                case double p when p == 404d:
                    brush.GradientStops = new GradientStopCollection()
                    {
                        new GradientStop(Colors.Red, 0.0),
                        new GradientStop(Colors.Red, 1.0)
                    };
                    break;

                case double p when p == 1.0:
                    brush.GradientStops = new GradientStopCollection()
                    {
                        new GradientStop(Colors.Green, 0.0),
                        new GradientStop(Colors.Green, 1.0)
                    };
                    break;

                default:
                    return null;
            };
            return brush;
        }
    }
}

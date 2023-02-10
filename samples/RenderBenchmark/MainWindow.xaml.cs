using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace RenderBenchmark
{
    public class MainWindow : Window
    {

        public class CustomRender : Control
        {
            private Pen _iPen;
            private readonly FormattedText _formattedText;

            public CustomRender()
            {
                _iPen = new Pen(Brushes.DarkBlue, 1);
                _formattedText = new FormattedText("Text Text Text", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, Typeface.Default, 20, Brushes.DarkRed);
                //InitializeComponent();
            }

            private int _loops = 0;

            public override void Render(DrawingContext context)
            {
                if (_loops == 500)
                {
                    JetBrains.Profiler.SelfApi.DotTrace.StopCollectingData();
                    JetBrains.Profiler.SelfApi.DotTrace.SaveData();
                    Environment.Exit(0);
                }

                // Square Rectangle
                var rect = new Rect(10, 10, 50, 50);
                for (int i = 0; i < 500; i++)
                    context.DrawRectangle(_iPen, rect);

                // Rounded Rectangle
                rect = new Rect(70, 10, 50, 50);
                for (int i = 0; i < 500; i++)
                    context.DrawRectangle(_iPen, rect, 10);

                // Text
                var point = new Point(130, 10);
                for (int i = 0; i < 500; i++)
                    context.DrawText(_formattedText, point);

                _loops++;

                Dispatcher.UIThread.Post(InvalidateVisual);
            }
        }
        public MainWindow()
        {

            this.InitializeComponent();

            Content = new CustomRender();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

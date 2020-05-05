using Bricker.Game;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Bricker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private
        private readonly Main _main;
        private readonly DispatcherTimer _timer;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MainWindow()
        {
            //designer stuff
            InitializeComponent();

            //vars
            _main = new Main(this);
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(_main.Config.HighFrameRate ? 15 : 30), DispatcherPriority.Background, Timer_Callback, _skia.Dispatcher);

            //events
            _skia.PaintSurface += (s, e) => _main.DrawFrame(e);
            Loaded += (s, e) => _main.StartProgramLoop();
            PreviewKeyDown += (s, e) =>
            {
                e.Handled = true;
                _main.LogKeyPress(e.Key);
            };
        }

        /// <summary>
        /// Fired by timer to invalidate canvas and force another frame draw.
        /// </summary>
        private void Timer_Callback(object s, EventArgs e)
        {
            _skia.InvalidateVisual();
        }
    }
}

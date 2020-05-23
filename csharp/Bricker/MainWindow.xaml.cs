using Bricker.Game;
using Common.Standard.Configuration;
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
        private DispatcherTimer _timer;
        private bool _highFrameRate;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MainWindow()
        {
            //designer stuff
            InitializeComponent();

            //vars
            _main = new Main(this);
            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(_main.Config.HighFrameRate ? 15 : 45), DispatcherPriority.Background, Timer_Callback, _skia.Dispatcher);
            _highFrameRate = _main.Config.HighFrameRate;

            //events
            _skia.PaintSurface += (s, e) => _main.DrawFrame(e);
            Loaded += (s, e) => _main.StartProgramLoop();
            Closed += (s, e) => _main.WindowClosing();
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
            if (GameConfig.Instance.HighFrameRate != _highFrameRate)
            {
                _highFrameRate = GameConfig.Instance.HighFrameRate;
                _timer.Stop();
                _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(GameConfig.Instance.HighFrameRate ? 15 : 45), DispatcherPriority.Background, Timer_Callback, _skia.Dispatcher);
            }
            _skia.InvalidateVisual();
        }
    }
}

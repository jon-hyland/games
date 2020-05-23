using Bricker.Configuration;
using Common.Rendering;
using Common.Standard.Error;
using Common.Standard.Extensions;
using Common.Standard.Threading;
using Common.Windows.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Bricker.Rendering
{
    /// <summary>
    /// Stores images for background tiles (experimental).
    /// </summary>
    public class Images
    {
        //const
        private const int CACHE_SIZE = 10;

        //private
        private readonly Random _random = new Random();
        private readonly List<string> _files = new List<string>();
        private readonly Queue<Image> _cache = new Queue<Image>();
        private readonly SimpleTimer _timer = null;

        //public
        public int FileCount => _files.Count;
        public int CacheCount => _cache.Count;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Images(Config config)
        {
            try
            {
                if ((!String.IsNullOrWhiteSpace(config.ImageFolder)) && (Directory.Exists(config.ImageFolder)))
                {
                    _files.AddRange(Directory.GetFiles(config.ImageFolder)
                        .Where(f => Path.GetExtension(f)
                        .ToLower()
                        .In(".jpg", ".jpeg", ".png", ".bmp")));
                }
                if (_files.Count == 0)
                    return;
                _timer = new SimpleTimer(Timer_Callback, 1000);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Returns a random image.
        /// </summary>
        private Image LoadRandomImage(int minSize, int maxSize)
        {
            lock (this)
            {
                int index = _random.Next(_files.Count);
                int size = _random.Next(minSize, maxSize);
                using (Image i = new Image(_files[index]))
                {
                    if (i.Width >= i.Height)
                    {
                        double ratio = i.Height / i.Width;
                        double width = size;
                        double height = size * ratio;
                        return i.Resize(width, height);
                    }
                    else
                    {
                        double ratio = i.Width / i.Height;
                        double width = size * ratio;
                        double height = size;
                        return i.Resize(width, height);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a random image from cache, or null.
        /// </summary>
        public Image GetRandomImageFromCache()
        {
            lock (this)
            {
                return _cache.Count > 0 ? _cache.Dequeue() : null;
            }
        }

        /// <summary>
        /// Fired by timer.
        /// </summary>
        private void Timer_Callback()
        {
            if (!RenderProps.ScaleSet)
                return;

            int cacheSize = Math.Max(CACHE_SIZE, _files.Count);
            lock (this)
            {
                if (_cache.Count >= cacheSize)
                    return;
            }

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Image image = LoadRandomImage(200, 350);
                    lock (this)
                    {
                        _cache.Enqueue(image);
                        if (_cache.Count >= cacheSize)
                            break;
                    }
                }
                catch
                {
                }
            }
        }


    }
}

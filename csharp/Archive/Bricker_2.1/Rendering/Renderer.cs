using Bricker.Configuration;
using Bricker.Error;
using Bricker.Game;
using Bricker.Utilities;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Bricker.Rendering
{
    /// <summary>
    /// Contains graphics rendering logic.
    /// </summary>
    public class Renderer : IDisposable
    {
        //private
        private readonly Window _window;
        private double _skiaWidth;
        private double _skiaHeight;
        private double _displayScale;
        private double _sideWidth;
        private double _leftX;
        private double _rightX;
        private readonly SKTypeface _typeface;
        private readonly SKPaint _linePaint;
        private readonly SKPaint _rectPaint;
        private readonly SKPaint _textPaint;
        private MenuProperties _menuProperties;
        private ScoreEntryProperties _scoreEntryProperties;
        private readonly CpsCalculator _fps;
        private SKColor _clearColor => !Config.Debug ? Colors.Black : Colors.DebugBlack;

        //public
        public double SkiaWidth => _skiaWidth;
        public double SkiaHeight => _skiaHeight;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Renderer(Window window)
        {
            _window = window;
            _displayScale = 1;
            _typeface = SKTypeface.FromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "zorque.ttf"));
            _linePaint = new SKPaint()
            {
                Color = Colors.TextWhite,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Square,
                StrokeJoin = SKStrokeJoin.Bevel,
                StrokeWidth = (float)(2 * _displayScale),
                IsAntialias = Config.AntiAlias
            };
            _rectPaint = new SKPaint()
            {
                Color = Colors.TextWhite,
                Style = SKPaintStyle.StrokeAndFill,
                StrokeCap = SKStrokeCap.Square,
                StrokeJoin = SKStrokeJoin.Bevel,
                IsAntialias = Config.AntiAlias
            };
            _textPaint = new SKPaint()
            {
                Color = Colors.TextWhite,
                Typeface = _typeface,
                TextSize = (float)(12 * _displayScale),
                IsStroke = false,
                IsAntialias = Config.AntiAlias
            };
            _menuProperties = null;
            _fps = new CpsCalculator(1);
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _typeface?.Dispose();
            _linePaint?.Dispose();
            _rectPaint?.Dispose();
            _textPaint?.Dispose();
        }

        /// <summary>
        /// Sets menu properties, tells renderer to draw the main menu.
        /// </summary>
        public void SetMenuProperties(MenuSelection selection, bool inGame)
        {
            if (_menuProperties != null)
            {
                _menuProperties.Selection = selection;
                _menuProperties.InGame = inGame;
            }
            else
            {
                _menuProperties = new MenuProperties(selection, inGame);
            }
        }

        /// <summary>
        /// Clears menu properties, tells renderer to not draw the main menu.
        /// </summary>
        public void ClearMenuProperties()
        {
            _menuProperties = null;
        }

        /// <summary>
        /// Sets score-entry properties, tells renderer to draw score-entry dialog.
        /// </summary>
        public void SetScoreEntryProperties(string initials)
        {
            if (_scoreEntryProperties != null)
                _scoreEntryProperties.Initials = initials;
            else
                _scoreEntryProperties = new ScoreEntryProperties(initials);
        }

        /// <summary>
        /// Clears score-entry properties, tells renderer to not draw score-entry menu.
        /// </summary>
        public void ClearScoreEntryProperties()
        {
            _scoreEntryProperties = null;
        }

        /// <summary>
        /// Gets the display scale of the current screen.  Standard resolution screens usually 1.0, high-DPI screens usually 1.5,
        /// 4K and retina-branded screens often 2.0.
        /// </summary>
        private double GetDisplayScale()
        {
            try
            {
                return PresentationSource.FromVisual(_window).CompositionTarget.TransformToDevice.M11;
            }
            catch
            {
                return 1d;
            }
        }

        /// <summary>
        /// Renders a new frame.
        /// </summary>
        public void DrawFrame(SKPaintSurfaceEventArgs e, Matrix matrix, GameStats stats, List<ExplodingSpace> spaces)
        {
            try
            {
                //vars
                SKImageInfo info = e.Info;
                SKSurface surface = e.Surface;
                SKCanvas frame = surface.Canvas;
                _displayScale = GetDisplayScale();
                _skiaWidth = info.Width / _displayScale;
                _skiaHeight = info.Height / _displayScale;
                _sideWidth = (_skiaWidth - 333d) / 2d;
                _leftX = ((_sideWidth - 250d) / 2d) + 5d;
                _rightX = _sideWidth + 333 + _leftX;

                //fps
                _fps.Increment();

                //clear canvas
                frame.Clear(SKColors.Black);

                //game matrix
                DrawMatrix(frame, matrix);

                //exploding spaces
                DrawExplodingSpaces(frame, spaces);

                //title
                DrawTitle(frame);

                //controls
                DrawControls(frame);

                //next
                DrawNext(frame, matrix);

                //level
                DrawLevel(frame, stats);

                //lines
                DrawLines(frame, stats);

                //current score
                DrawScore(frame, stats);

                //high scores
                DrawHighScores(frame, stats);

                //menu
                DrawMenu(frame);

                //score entry
                DrawScoreEntry(frame);

                //fps
                DrawFps(frame);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Draws the game matrix, once per frame.
        /// </summary>
        private void DrawMatrix(SKCanvas frame, Matrix matrix)
        {
            using (SKBitmap bmp = new SKBitmap((int)(333 * _displayScale), (int)(663 * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    for (int x = 1; x < matrix.Width; x++)
                        for (int y = 1; y < matrix.Height; y++)
                            if (matrix.Color[x, y] != Colors.Black)
                                DrawRect(canvas, matrix.Color[x, y], ((x - 1) * 33) + 2, ((y - 1) * 33) + 2, 32, 32);

                    if (matrix.Brick != null)
                        for (int x = 0; x < matrix.Brick.Width; x++)
                            for (int y = 0; y < matrix.Brick.Height; y++)
                                if (matrix.Brick.Grid[x, y])
                                    DrawRect(canvas, matrix.Brick.Color, (((matrix.Brick.X - 1) + x) * 33) + 2, (((matrix.Brick.Y - 1) + y) * 33) + 2, 32, 32);
                                else if (Config.Debug)
                                    DrawRect(canvas, matrix.Brick.Color, (((matrix.Brick.X - 1) + x) * 33) + 17, (((matrix.Brick.Y - 1) + y) * 33) + 17, 2, 2);

                    for (int i = 1; i <= 10; i++)
                        DrawLine(canvas, Colors.Gray, (i * 33) + 1, 2, (i * 33) + 1, 660, 1);
                    for (int i = 1; i <= 20; i++)
                        DrawLine(canvas, Colors.Gray, 2, (i * 33) + 1, 330, (i * 33) + 1, 1);

                    DrawLine(canvas, Colors.White, 0, 0, 332, 0, 1);
                    DrawLine(canvas, Colors.White, 0, 1, 332, 1, 1);
                    DrawLine(canvas, Colors.White, 0, 0, 0, 662, 1);
                    DrawLine(canvas, Colors.White, 1, 0, 1, 662, 1);
                    DrawLine(canvas, Colors.White, 0, 662, 332, 662, 1);
                    DrawLine(canvas, Colors.White, 0, 661, 332, 661, 1);
                    DrawLine(canvas, Colors.White, 332, 0, 332, 662, 1);
                    DrawLine(canvas, Colors.White, 331, 0, 331, 662, 1);

                    frame.DrawBitmap(bmp, (float)(_sideWidth * _displayScale), (float)(((_skiaHeight - 663d) / 2d) * _displayScale));
                }
            }
        }

        /// <summary>
        /// Draws game title.
        /// </summary>
        private void DrawTitle(SKCanvas frame)
        {
            double titleHeight = 86;
            double space = -16;
            double copyrightHeight = 16;
            double width = 280;
            double height = titleHeight + space + copyrightHeight;

            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(_clearColor);
                    using (SKBitmap title = RenderText(Colors.White, "bricker", 64))
                    {
                        canvas.DrawBitmap(title, (float)(((width - (title.Width / _displayScale)) / 2) * _displayScale), 0);
                    }
                    using (SKBitmap copyright = RenderText(Colors.TextWhite, $"v{Config.Version}  (c) 2017-2020  john hyland", 12))
                    {
                        canvas.DrawBitmap(copyright, (float)(((width - (copyright.Width / _displayScale)) / 2) * _displayScale), (float)((titleHeight + space) * _displayScale));
                    }
                }
                frame.DrawBitmap(bmp, (float)(((_sideWidth - width) / 2) * _displayScale), (float)(24 * _displayScale));
            }
        }

        /// <summary>
        /// Draws controls readout.
        /// </summary>
        private void DrawControls(SKCanvas frame)
        {
            SKBitmap title = null, left1 = null, left2 = null, left3 = null, left4 = null, left5 = null, left6 = null, right1 = null, right2 = null, right3 = null, right4 = null, right5 = null, right6 = null;

            try
            {
                double width = 240;
                double height = 210;
                double titleSpacing = 54;
                double lineSpacing = 23;

                title = RenderText(Colors.TextWhite, "controls", 28);
                left1 = RenderText(Colors.TextWhite, "left", 18);
                left2 = RenderText(Colors.TextWhite, "right", 18);
                left3 = RenderText(Colors.TextWhite, "down", 18);
                left4 = RenderText(Colors.TextWhite, "rotate", 18);
                left5 = RenderText(Colors.TextWhite, "drop", 18);
                left6 = RenderText(Colors.TextWhite, "pause", 18);
                right1 = RenderText(Colors.TextWhite, "left", 18);
                right2 = RenderText(Colors.TextWhite, "right", 18);
                right3 = RenderText(Colors.TextWhite, "down", 18);
                right4 = RenderText(Colors.TextWhite, "up", 18);
                right5 = RenderText(Colors.TextWhite, "space", 18);
                right6 = RenderText(Colors.TextWhite, "esc", 18);

                using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
                {
                    using (SKCanvas canvas = new SKCanvas(bmp))
                    {
                        canvas.Clear(_clearColor);
                        canvas.DrawBitmap(title, 0, 0);
                        canvas.DrawBitmap(left1, (float)(10 * _displayScale), (float)((titleSpacing * _displayScale) + (lineSpacing * 0 * _displayScale)));
                        canvas.DrawBitmap(left2, (float)(10 * _displayScale), (float)((titleSpacing * _displayScale) + (lineSpacing * 1 * _displayScale)));
                        canvas.DrawBitmap(left3, (float)(10 * _displayScale), (float)((titleSpacing * _displayScale) + (lineSpacing * 2 * _displayScale)));
                        canvas.DrawBitmap(left4, (float)(10 * _displayScale), (float)((titleSpacing * _displayScale) + (lineSpacing * 3 * _displayScale)));
                        canvas.DrawBitmap(left5, (float)(10 * _displayScale), (float)((titleSpacing * _displayScale) + (lineSpacing * 4 * _displayScale)));
                        canvas.DrawBitmap(left6, (float)(10 * _displayScale), (float)((titleSpacing * _displayScale) + (lineSpacing * 5 * _displayScale)));
                        canvas.DrawBitmap(right1, (float)((width * _displayScale) - right1.Width - (10 * _displayScale)), (float)((titleSpacing * _displayScale) + (lineSpacing * 0 * _displayScale)));
                        canvas.DrawBitmap(right2, (float)((width * _displayScale) - right2.Width - (10 * _displayScale)), (float)((titleSpacing * _displayScale) + (lineSpacing * 1 * _displayScale)));
                        canvas.DrawBitmap(right3, (float)((width * _displayScale) - right3.Width - (10 * _displayScale)), (float)((titleSpacing * _displayScale) + (lineSpacing * 2 * _displayScale)));
                        canvas.DrawBitmap(right4, (float)((width * _displayScale) - right4.Width - (10 * _displayScale)), (float)((titleSpacing * _displayScale) + (lineSpacing * 3 * _displayScale)));
                        canvas.DrawBitmap(right5, (float)((width * _displayScale) - right5.Width - (10 * _displayScale)), (float)((titleSpacing * _displayScale) + (lineSpacing * 4 * _displayScale)));
                        canvas.DrawBitmap(right6, (float)((width * _displayScale) - right6.Width - (10 * _displayScale)), (float)((titleSpacing * _displayScale) + (lineSpacing * 5 * _displayScale)));
                        frame.DrawBitmap(bmp, (float)(_leftX * _displayScale), (float)(206 * _displayScale));
                    }
                }
            }
            finally
            {
                title?.Dispose();
                left1?.Dispose();
                left2?.Dispose();
                left3?.Dispose();
                left4?.Dispose();
                left5?.Dispose();
                left6?.Dispose();
                right1?.Dispose();
                right2?.Dispose();
                right3?.Dispose();
                right4?.Dispose();
                right5?.Dispose();
                right6?.Dispose();
            }
        }

        /// <summary>
        /// Draws next brick readout.
        /// </summary>
        private void DrawNext(SKCanvas frame, Matrix matrix)
        {
            double width = 240;
            double height = 172;
            double titleSpacing = 70;

            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(_clearColor);
                    if (matrix.NextBrick != null)
                    {
                        double size = (matrix.NextBrick.Width * 32) + (matrix.NextBrick.Width - 1);
                        using (SKBitmap brick = new SKBitmap((int)(size * _displayScale), (int)(size * _displayScale)))
                        {
                            using (SKCanvas brickCanvas = new SKCanvas(brick))
                            {
                                brickCanvas.Clear(_clearColor);
                                for (int x = 0; x < matrix.NextBrick.Width; x++)
                                    for (int y = 0; y < matrix.NextBrick.Height; y++)
                                        if (matrix.NextBrick.Grid[x, y])
                                            DrawRect(brickCanvas, matrix.NextBrick.Color, x * 33, y * 33, 32, 32);
                            }
                            canvas.DrawBitmap(brick, (float)(((width * _displayScale) - (size * _displayScale)) / 2), (float)((titleSpacing * _displayScale) - (matrix.NextBrick.TopSpace * 33 * _displayScale)));
                        }
                    }
                    using (SKBitmap title = RenderText(Colors.TextWhite, "next", 28))
                    {
                        canvas.DrawBitmap(title, 0, 0);
                    }
                }
                frame.DrawBitmap(bmp, (float)(_leftX * _displayScale), (float)(476 * _displayScale));
            }
        }

        /// <summary>
        /// Draws current level readout.
        /// </summary>
        private void DrawLevel(SKCanvas frame, GameStats stats)
        {
            double width = 240;
            double height = 80;
            double titleSpacing = 28;
            
            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(_clearColor);
                    using (SKBitmap title = RenderText(Colors.TextWhite, "level", 28))
                    {
                        canvas.DrawBitmap(title, 0, 0);
                    }
                    using (SKBitmap level = RenderText(Colors.TextWhite, stats.Level.ToString("N0"), 42))
                    {
                        canvas.DrawBitmap(level, (float)((width * _displayScale) - level.Width), (float)(titleSpacing * _displayScale));
                    }
                }
                frame.DrawBitmap(bmp, (float)(_rightX * _displayScale), (float)(35 * _displayScale));
            }
        }

        /// <summary>
        /// Draws current lines readout.
        /// </summary>
        private void DrawLines(SKCanvas frame, GameStats stats)
        {
            double width = 240;
            double height = 80;
            double space = 28;

            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(_clearColor);
                    using (SKBitmap title = RenderText(Colors.TextWhite, "lines", 28))
                    {
                        canvas.DrawBitmap(title, 0, 0);
                    }
                    using (SKBitmap level = RenderText(Colors.TextWhite, stats.Lines.ToString("N0"), 42))
                    {
                        canvas.DrawBitmap(level, (float)((width * _displayScale) - level.Width), (float)(space * _displayScale));
                    }
                }
                frame.DrawBitmap(bmp, (float)(_rightX * _displayScale), (float)(153 * _displayScale));
            }
        }

        /// <summary>
        /// Draws current score readout.
        /// </summary>
        private void DrawScore(SKCanvas frame, GameStats stats)
        {
            double width = 240;
            double height = 80;
            double space = 28;

            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(_clearColor);
                    using (SKBitmap title = RenderText(Colors.TextWhite, "score", 28))
                    {
                        canvas.DrawBitmap(title, 0, 0);
                    }
                    using (SKBitmap level = RenderText(Colors.TextWhite, stats.Score.ToString("N0"), 42))
                    {
                        canvas.DrawBitmap(level, (float)((width * _displayScale) - level.Width), (float)(space * _displayScale));
                    }
                }
                frame.DrawBitmap(bmp, (float)(_rightX * _displayScale), (float)(271 * _displayScale));
            }
        }

        /// <summary>
        /// Draws high scores readout.
        /// </summary>
        private void DrawHighScores(SKCanvas frame, GameStats stats)
        {
            double width = 240;
            double height = 286;
            double titleSpacing = 54;
            double lineSpacing = 23;

            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(_clearColor);
                    using (SKBitmap title = RenderText(Colors.TextWhite, "high scores", 28))
                    {
                        canvas.DrawBitmap(title, 0, 0);
                    }
                    for (int i = 0; i < stats.HighScores.Count; i++)
                    {
                        HighScore score = stats.HighScores[i];
                        using (SKBitmap left = RenderText(Colors.TextWhite, score.Initials, 18))
                        {
                            canvas.DrawBitmap(left, (float)(10 * _displayScale), (float)((titleSpacing * _displayScale) + (lineSpacing * i * _displayScale)));
                        }
                        using (SKBitmap right = RenderText(Colors.TextWhite, score.Score.ToString("N0"), 18))
                        {
                            canvas.DrawBitmap(right, (float)((width * _displayScale) - right.Width), (float)((titleSpacing * _displayScale) + (lineSpacing * i * _displayScale)));
                        }
                    }
                }
                frame.DrawBitmap(bmp, (float)(_rightX * _displayScale), (float)(389 * _displayScale));
            }
        }

        /// <summary>
        /// Draws main menu.
        /// </summary>
        private void DrawMenu(SKCanvas frame)
        {
            if (_menuProperties == null)
                return;

            double betweenSpacing = 22;
            double itemHeight = 56;
            double width = 400;
            double height = (itemHeight * 3) + (betweenSpacing * 4) + 4;

            SKColor resumeColor = _menuProperties.InGame ? Colors.White : Colors.Gray;
            SKColor newColor = Colors.White;
            SKColor quitColor = Colors.White;
            if (_menuProperties.Selection == MenuSelection.Resume)
                resumeColor = Colors.FluorescentOrange;
            else if (_menuProperties.Selection == MenuSelection.New)
                newColor = Colors.FluorescentOrange;
            else if (_menuProperties.Selection == MenuSelection.Quit)
                quitColor = Colors.FluorescentOrange;

            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(Colors.Black);
                    DrawLine(canvas, Colors.White, 0, 0, width - 1, 0, 1);
                    DrawLine(canvas, Colors.White, 0, 1, width - 1, 1, 1);
                    DrawLine(canvas, Colors.White, 0, height - 2, width - 1, height - 2, 1);
                    DrawLine(canvas, Colors.White, 0, height - 1, width - 1, height - 1, 1);
                    DrawLine(canvas, Colors.White, 0, 0, 0, height - 1, 1);
                    DrawLine(canvas, Colors.White, 1, 0, 1, height - 1, 1);
                    DrawLine(canvas, Colors.White, width - 2, 0, width - 2, height - 1, 1);
                    DrawLine(canvas, Colors.White, width - 1, 0, width - 1, height - 1, 1);

                    using (SKBitmap resumeBmp = RenderText(resumeColor, "resume", 42))
                    {
                        canvas.DrawBitmap(resumeBmp, (float)((bmp.Width - resumeBmp.Width) / 2), (float)(((itemHeight * 0) + (betweenSpacing * 1)) * _displayScale));
                    }
                    using (SKBitmap newBmp = RenderText(newColor, "new game", 42))
                    {
                        canvas.DrawBitmap(newBmp, (float)((bmp.Width - newBmp.Width) / 2), (float)(((itemHeight * 1) + (betweenSpacing * 2)) * _displayScale));
                    }
                    using (SKBitmap quitBmp = RenderText(quitColor, "quit", 42))
                    {
                        canvas.DrawBitmap(quitBmp, (float)((bmp.Width - quitBmp.Width) / 2), (float)(((itemHeight * 2) + (betweenSpacing * 3)) * _displayScale));
                    }
                }
                frame.DrawBitmap(bmp, (float)(((_skiaWidth - width) / 2) * _displayScale), (float)(((_skiaHeight - height) / 2) * _displayScale));
            }
        }

        /// <summary>
        /// Draws score-entry dialog.
        /// </summary>
        private void DrawScoreEntry(SKCanvas frame)
        {
            if (_scoreEntryProperties == null)
                return;

            double spacing = 10;
            double lineHeight = 38;
            double charWidth = 60;
            double charHeight = 82;
            double width = 400;
            double height = (spacing * 4) + (lineHeight * 2) + charHeight + 4;
            string initials = _scoreEntryProperties.Initials.PadRight(3);

            using (SKBitmap bmp = new SKBitmap((int)(width * _displayScale), (int)(height * _displayScale)))
            {
                using (SKCanvas canvas = new SKCanvas(bmp))
                {
                    canvas.Clear(Colors.Black);
                    DrawLine(canvas, Colors.White, 0, 0, width - 1, 0, 1);
                    DrawLine(canvas, Colors.White, 0, 1, width - 1, 1, 1);
                    DrawLine(canvas, Colors.White, 0, height - 2, width - 1, height - 2, 1);
                    DrawLine(canvas, Colors.White, 0, height - 1, width - 1, height - 1, 1);
                    DrawLine(canvas, Colors.White, 0, 0, 0, height - 1, 1);
                    DrawLine(canvas, Colors.White, 1, 0, 1, height - 1, 1);
                    DrawLine(canvas, Colors.White, width - 2, 0, width - 2, height - 1, 1);
                    DrawLine(canvas, Colors.White, width - 1, 0, width - 1, height - 1, 1);

                    using (SKBitmap line1 = RenderText(Colors.White, "new high score!", 28))
                    {
                        canvas.DrawBitmap(line1, (float)((bmp.Width - line1.Width) / 2), (float)((spacing + 2) * _displayScale));
                    }
                    using (SKBitmap line2 = RenderText(Colors.White, "enter initials:", 28))
                    {
                        canvas.DrawBitmap(line2, (float)((bmp.Width - line2.Width) / 2), (float)((spacing + lineHeight + 2) * _displayScale));
                    }
                    using (SKBitmap initialsB = new SKBitmap((int)(charWidth * 3 * _displayScale), (int)(charHeight * _displayScale)))
                    {
                        using (SKCanvas initialsC = new SKCanvas(initialsB))
                        {
                            initialsC.Clear(Colors.Black);
                            using (SKBitmap char1 = RenderText(Colors.FluorescentOrange, initials[0].ToString(), 64))
                            {
                                initialsC.DrawBitmap(char1, (float)((((charWidth * _displayScale) - char1.Width) / 2) + (charWidth * 0 * _displayScale)), (float)(((charHeight * _displayScale) - char1.Height) / 2));
                            }
                            using (SKBitmap char2 = RenderText(Colors.FluorescentOrange, initials[1].ToString(), 64))
                            {
                                initialsC.DrawBitmap(char2, (float)((((charWidth * _displayScale) - char2.Width) / 2) + (charWidth * 1 * _displayScale)), (float)(((charHeight * _displayScale) - char2.Height) / 2));
                            }
                            using (SKBitmap char3 = RenderText(Colors.FluorescentOrange, initials[2].ToString(), 64))
                            {
                                initialsC.DrawBitmap(char3, (float)((((charWidth * _displayScale) - char3.Width) / 2) + (charWidth * 2 * _displayScale)), (float)(((charHeight * _displayScale) - char3.Height) / 2));
                            }
                        }
                        canvas.DrawBitmap(initialsB, (float)((bmp.Width - initialsB.Width) / 2), (float)(((spacing * 2) + (lineHeight * 2) + 2) * _displayScale));
                    }
                }
                frame.DrawBitmap(bmp, (float)(((_skiaWidth - width) / 2) * _displayScale), (float)(((_skiaHeight - height) / 2) * _displayScale));
            }
        }

        /// <summary>
        /// Draws fps readout.
        /// </summary>
        private void DrawFps(SKCanvas frame)
        {
            if (!Config.Debug)
                return;

            using (SKBitmap bmp = RenderText(Colors.TextWhite, $"fps: {(int)_fps.CPS}", 12))
            {
                frame.DrawBitmap(bmp, (float)(10 * _displayScale), (float)(675 * _displayScale));
            }
        }

        /// <summary>
        /// Draws exploding spaces.
        /// </summary>
        private void DrawExplodingSpaces(SKCanvas canvas, List<ExplodingSpace> spaces)
        {
            if ((spaces == null) || (spaces.Count == 0))
                return;

            foreach (ExplodingSpace space in spaces)
            {
                double x = space.X;
                double y = space.Y;
                DrawRect(canvas, space.Color, x, y, 34, 34);
                DrawLine(canvas, Colors.Black, x, y, x + 34, y);
                DrawLine(canvas, Colors.Black, x, y + 34, x + 34, y + 34);
                DrawLine(canvas, Colors.Black, x, y, x, y + 34);
                DrawLine(canvas, Colors.Black, x + 34, y, x + 34, y + 34);
            }
        }

        /// <summary>
        /// Draws line on the specified canvas.
        /// </summary>
        private void DrawLine(SKCanvas canvas, SKColor color, double x0, double y0, double x1, double y1, double width = 2d)
        {
            _linePaint.Color = color;
            _linePaint.StrokeWidth = (float)(width * _displayScale);
            canvas.DrawLine((float)(x0 * _displayScale), (float)(y0 * _displayScale), (float)(x1 * _displayScale), (float)(y1 * _displayScale), _linePaint);
        }

        /// <summary>
        /// Draws rectangle on the specified canvas.
        /// </summary>
        private void DrawRect(SKCanvas canvas, SKColor color, double x, double y, double width, double height)
        {
            _rectPaint.Color = color;
            canvas.DrawRect(SKRect.Create((float)(x * _displayScale), (float)(y * _displayScale), (float)(width * _displayScale), (float)(height * _displayScale)), _rectPaint);
        }

        /// <summary>
        /// Creates and returns a bitmap with text, which must be disposed.
        /// </summary>
        private SKBitmap RenderText(SKColor color, string text, double size)
        {
            _textPaint.Color = color;
            _textPaint.TextSize = (float)(size * _displayScale);
            SKRect r = new SKRect();
            _textPaint.MeasureText(text, ref r);

            int width = (int)(r.Width + (6 * _displayScale));
            int height = (int)(Math.Ceiling((size + (size * 0.333)) * _displayScale));

            float x = (float)(2 * _displayScale);
            float y = (float)((size + (size * 0.05)) * _displayScale);

            SKBitmap b = new SKBitmap(width, height);
            using (SKCanvas c = new SKCanvas(b))
            {
                c.Clear(Colors.Transparent);
                c.DrawText(text, x, y, _textPaint);
            }
            return b;
        }








    }
}

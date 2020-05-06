using Bricker.Configuration;
using Bricker.Error;
using Bricker.Game;
using Bricker.Rendering.Properties;
using Common.Networking.Game;
using Common.Networking.Game.Discovery;
using Common.Rendering;
using Common.Utilities;
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
        private readonly Config _config;
        private double _frameWidth;
        private double _frameHeight;
        private double _displayScale;
        private double _sideWidth;
        private double _leftX;
        private double _rightX;
        private readonly SKTypeface _typeface;
        private readonly SKPaint _linePaint;
        private readonly SKPaint _rectPaint;
        private readonly SKPaint _textPaint;
        private MenuProperties _menuProps;
        private InitialsEntryProperties _initialProps;
        private MessageProperties _messageProps;
        private LobbyProperties _lobbyProps;
        private readonly CpsCalculator _fps;

        //public
        public double FrameWidth => _frameWidth;
        public double FrameHeight => _frameHeight;
        public MenuProperties MenuProps { get => _menuProps; set => _menuProps = value; }
        public InitialsEntryProperties InitialProps { get => _initialProps; set => _initialProps = value; }
        public MessageProperties MessageProps { get => _messageProps; set => _messageProps = value; }
        public LobbyProperties LobbyProps { get => _lobbyProps; set => _lobbyProps = value; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Renderer(Window window, Config config)
        {
            _window = window;
            _config = config;
            _displayScale = 1;
            _typeface = SKTypeface.FromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "zorque.ttf"));
            _linePaint = new SKPaint()
            {
                Color = Colors.White,
                Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Square,
                StrokeJoin = SKStrokeJoin.Bevel,
                StrokeWidth = (float)(2 * _displayScale),
                IsAntialias = RenderProps.AntiAlias
            };
            _rectPaint = new SKPaint()
            {
                Color = Colors.White,
                Style = SKPaintStyle.StrokeAndFill,
                StrokeCap = SKStrokeCap.Square,
                StrokeJoin = SKStrokeJoin.Bevel,
                IsAntialias = RenderProps.AntiAlias
            };
            _textPaint = new SKPaint()
            {
                Color = Colors.White,
                Typeface = _typeface,
                TextSize = (float)(12 * _displayScale),
                IsStroke = false,
                IsAntialias = RenderProps.AntiAlias
            };
            _menuProps = null;
            _initialProps = null;
            _messageProps = null;
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
        public void DrawFrame(SKPaintSurfaceEventArgs e, Matrix matrix, GameStats stats, List<ExplodingSpace> spaces, GameCommunications communications, Opponent opponent)
        {
            try
            {
                //vars
                SKImageInfo info = e.Info;
                _displayScale = GetDisplayScale();
                if (RenderProps.DisplayScale != _displayScale)
                    RenderProps.DisplayScale = _displayScale;
                _frameWidth = info.Width / _displayScale;
                _frameHeight = info.Height / _displayScale;
                _sideWidth = (_frameWidth - 333d) / 2d;
                _leftX = ((_sideWidth - 250d) / 2d) + 5d;
                _rightX = _sideWidth + 333 + _leftX;
                Surface frame = new Surface(e.Surface.Canvas, _frameWidth, _frameHeight);

                //fps
                _fps.Increment();

                //clear surface
                frame.Clear(Colors.Black);

                //game matrix
                DrawMatrix(frame, matrix);

                //opponent matrix
                DrawOpponentMatrix(frame, opponent);

                //exploding spaces
                DrawExplodingSpaces(frame, spaces);

                //title
                DrawTitle(frame);

                //controls
                DrawControls(frame, opponent);

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
                DrawInitialsEntry(frame);

                //message box
                DrawMessageBox(frame);

                //discovery lobby
                DrawLobbyMenu(frame);

                //debug info
                DrawDebugInfo(frame, communications);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Draws the game matrix, once per frame.
        /// </summary>
        private void DrawMatrix(Surface frame, Matrix matrix)
        {
            double width = 333;
            double height = 663;

            using (Surface surface = new Surface(width, height))
            {
                for (int x = 1; x < 12; x++)
                    for (int y = 1; y < 22; y++)
                        if (matrix.Grid[x, y] > 0)
                            surface.DrawRect(Brick.BrickToColor(matrix.Grid[x, y]), ((x - 1) * 33) + 2, ((y - 1) * 33) + 2, 32, 32);

                if (matrix.Brick != null)
                    for (int x = 0; x < matrix.Brick.Width; x++)
                        for (int y = 0; y < matrix.Brick.Height; y++)
                            if (matrix.Brick.Grid[x, y] > 0)
                                surface.DrawRect(matrix.Brick.Color, ((matrix.Brick.X - 1 + x) * 33) + 2, ((matrix.Brick.Y - 1 + y) * 33) + 2, 32, 32);
                            else if (RenderProps.Debug)
                                surface.DrawRect(Colors.BrightWhite, ((matrix.Brick.X - 1 + x) * 33) + 17, ((matrix.Brick.Y - 1 + y) * 33) + 17, 2, 2);

                for (int i = 1; i <= 10; i++)
                    surface.DrawLine(Colors.Gray, (i * 33) + 1, 2, (i * 33) + 1, 660, 1);
                for (int i = 1; i <= 20; i++)
                    surface.DrawLine(Colors.Gray, 2, (i * 33) + 1, 330, (i * 33) + 1, 1);

                surface.DrawLine(Colors.BrightWhite, 0, 0, 332, 0, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 1, 332, 1, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 0, 0, 662, 1);
                surface.DrawLine(Colors.BrightWhite, 1, 0, 1, 662, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 662, 332, 662, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 661, 332, 661, 1);
                surface.DrawLine(Colors.BrightWhite, 332, 0, 332, 662, 1);
                surface.DrawLine(Colors.BrightWhite, 331, 0, 331, 662, 1);

                frame.Blit(surface, _sideWidth, (_frameHeight - 663) / 2);
            }
        }

        /// <summary>
        /// Draws other player's matrix.
        /// </summary>
        private void DrawOpponentMatrix(Surface frame, Opponent opponent)
        {
            if (opponent == null)
                return;

            byte[,] matrix = opponent.GetMatrix();
            double matrixWidth = 164 + 9;
            double matrixHeight = 324 + 19;

            using (Surface surface = new Surface(matrixWidth, matrixHeight))
            {
                for (int x = 1; x < 12; x++)
                    for (int y = 1; y < 22; y++)
                        if (matrix[x, y] > 0)
                            surface.DrawRect(Brick.BrickToColor(matrix[x, y]), ((x - 1) * 17) + 1.5, ((y - 1) * 17) + 1.5, 16, 16);

                surface.DrawLine(Colors.BrightWhite, 1, 1, matrixWidth - 2, 0, 1);
                surface.DrawLine(Colors.BrightWhite, matrixWidth - 2, 1, matrixWidth - 2, matrixHeight - 2, 1);
                surface.DrawLine(Colors.BrightWhite, matrixWidth - 2, matrixHeight - 2, 1, matrixHeight - 2, 1);
                surface.DrawLine(Colors.BrightWhite, 1, matrixHeight - 2, 1, 1, 1);

                frame.Blit(surface, (_sideWidth - surface.Width) / 2, 134);
            }

            double readoutWidth = 240;
            double readoutHeight = 56;
            double textHeight = 24;
            using (Surface surface = new Surface(readoutWidth, readoutHeight))
            {
                surface.DrawText_Left(Colors.White, $"Player: {opponent.Player.Name}", 18, 4);
                surface.DrawText_Right(Colors.White, $"Level: {opponent.Level}        Lines: {opponent.Lines.ToString("N0")}        Score: {opponent.Score.ToString("N0")}", 12, textHeight + 6);
                frame.Blit(surface, _leftX, 134 + matrixHeight + 6);
            }
        }

        /// <summary>
        /// Draws game title.
        /// </summary>
        private void DrawTitle(Surface frame)
        {
            if (RenderProps.Debug)
                return;

            double titleHeight = 86;
            double space = -16;
            double copyrightHeight = 16;
            double width = 280;
            double height = titleHeight + space + copyrightHeight;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Centered(Colors.BrightWhite, "bricker", 64, 0);
                surface.DrawText_Centered(Colors.BrightWhite, $"v{_config.GameVersion}  (c) 2017-2020  john hyland", 12, titleHeight + space);
                frame.Blit(surface, (_sideWidth - width) / 2, 16);
            }
        }

        /// <summary>
        /// Draws controls readout.
        /// </summary>
        private void DrawControls(Surface frame, Opponent opponent)
        {
            if (opponent != null)
                return;

            double width = 240;
            double height = 210;
            double titleSpacing = 54;
            double lineSpacing = 23;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(Colors.White, "controls", 28, 0);
                surface.DrawText_Left(Colors.White, "left", 18, titleSpacing + (lineSpacing * 0), 10);
                surface.DrawText_Left(Colors.White, "right", 18, titleSpacing + (lineSpacing * 1), 10);
                surface.DrawText_Left(Colors.White, "down", 18, titleSpacing + (lineSpacing * 2), 10);
                surface.DrawText_Left(Colors.White, "rotate", 18, titleSpacing + (lineSpacing * 3), 10);
                surface.DrawText_Left(Colors.White, "drop", 18, titleSpacing + (lineSpacing * 4), 10);
                surface.DrawText_Left(Colors.White, "pause", 18, titleSpacing + (lineSpacing * 5), 10);
                surface.DrawText_Right(Colors.White, "left", 18, titleSpacing + (lineSpacing * 0), 10);
                surface.DrawText_Right(Colors.White, "right", 18, titleSpacing + (lineSpacing * 1), 10);
                surface.DrawText_Right(Colors.White, "down", 18, titleSpacing + (lineSpacing * 2), 10);
                surface.DrawText_Right(Colors.White, "up", 18, titleSpacing + (lineSpacing * 3), 10);
                surface.DrawText_Right(Colors.White, "space", 18, titleSpacing + (lineSpacing * 4), 10);
                surface.DrawText_Right(Colors.White, "pause", 18, titleSpacing + (lineSpacing * 5), 10);
                frame.Blit(surface, _leftX, 238);
            }
        }

        /// <summary>
        /// Draws next brick readout.
        /// </summary>
        private void DrawNext(Surface frame, Matrix matrix)
        {
            double width = 240;
            double height = 98;
            double titleSpacing = 54;

            using (Surface surface = new Surface(width, height))
            {
                if (matrix.NextBrick != null)
                {
                    double size = (matrix.NextBrick.Width * 17) + 1;
                    using (Surface brick = new Surface(size, size))
                    {
                        for (int x = 0; x < matrix.NextBrick.Width; x++)
                            for (int y = 0; y < matrix.NextBrick.Height; y++)
                                if (matrix.NextBrick.Grid[x, y] > 0)
                                    brick.DrawRect(matrix.NextBrick.Color, x * 17, y * 17, 16, 16);
                        surface.Blit(
                            brick,
                            (width - size) / 2,
                            titleSpacing - (matrix.NextBrick.TopSpace * 17));
                    }
                }
                surface.DrawText_Left(Colors.White, "next", 28, 0);
                frame.Blit(surface, _leftX, 566);
            }
        }

        /// <summary>
        /// Draws current level readout.
        /// </summary>
        private void DrawLevel(Surface frame, GameStats stats)
        {
            double width = 240;
            double height = 80;
            double space = 28;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(Colors.White, "level", 28, 0);
                surface.DrawText_Right(Colors.White, stats.Level.ToString("N0"), 42, space);
                frame.Blit(surface, _rightX, 35);
            }
        }

        /// <summary>
        /// Draws current lines readout.
        /// </summary>
        private void DrawLines(Surface frame, GameStats stats)
        {
            double width = 240;
            double height = 80;
            double space = 28;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(Colors.White, "lines", 28, 0);
                surface.DrawText_Right(Colors.White, stats.Lines.ToString("N0"), 42, space);
                frame.Blit(surface, _rightX, 153);
            }
        }

        /// <summary>
        /// Draws current score readout.
        /// </summary>
        private void DrawScore(Surface frame, GameStats stats)
        {
            double width = 240;
            double height = 80;
            double space = 28;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(Colors.White, "score", 28, 0);
                surface.DrawText_Right(Colors.White, stats.Score.ToString("N0"), 42, space);
                frame.Blit(surface, _rightX, 271);
            }
        }

        /// <summary>
        /// Draws high scores readout.
        /// </summary>
        private void DrawHighScores(Surface frame, GameStats stats)
        {
            double width = 240;
            double height = 286;
            double titleSpacing = 54;
            double lineSpacing = 23;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(Colors.White, "high scores", 28, 0);
                for (int i = 0; i < stats.HighScores.Count; i++)
                {
                    HighScore score = stats.HighScores[i];
                    surface.DrawText_Left(Colors.White, score.Initials, 18, titleSpacing + (lineSpacing * i), 10);
                    surface.DrawText_Right(Colors.White, score.Score.ToString("N0"), 18, titleSpacing + (lineSpacing * i), 10);
                }
                frame.Blit(surface, _rightX, 389);
            }
        }

        /// <summary>
        /// Draws main menu.
        /// </summary>
        private void DrawMenu(Surface frame)
        {
            MenuProperties props = _menuProps;
            if (props == null)
                return;

            double vertSpacing = 22;
            double horizSpacing = 42;
            double optionSpacing = 22;

            if ((props.Width is Double.NaN) || (props.Height is Double.NaN))
            {
                double maxWidth = 0, totalHeight = 0;
                double headerLineHeight = 0, optionLineHeight = 0;
                foreach (string header in props.Header)
                {
                    Surface.MeasureText(header, props.HeaderSize, out double w, out double h);
                    if (w > maxWidth)
                        maxWidth = w;
                    totalHeight += h;
                    headerLineHeight = h;
                }
                foreach (string option in props.Options)
                {
                    Surface.MeasureText(option, props.OptionsSize, out double w, out double h);
                    if (w > maxWidth)
                        maxWidth = w;
                    totalHeight += h;
                    optionLineHeight = h;
                }
                props.Width = props.Width is Double.NaN ? (2 + horizSpacing + maxWidth + horizSpacing + 2) : props.Width;
                double headerHeight = props.Header.Length > 0 ? vertSpacing : 0;
                props.Height = 2 + totalHeight + vertSpacing + headerHeight + (optionSpacing * (props.Options.Length - 1)) + vertSpacing + 2;
                props.HeaderLineHeight = headerLineHeight;
                props.OptionLineHeight = optionLineHeight;
            }

            double width = props.Width, height = props.Height;
            double y = 2 + vertSpacing;
            using (Surface surface = new Surface(width, height, Colors.Black))
            {
                surface.DrawLine(Colors.BrightWhite, 0, 0, width - 1, 0, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 1, width - 1, 1, 1);
                surface.DrawLine(Colors.BrightWhite, 0, height - 2, width - 1, height - 2, 1);
                surface.DrawLine(Colors.BrightWhite, 0, height - 1, width - 1, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 0, 0, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, 1, 0, 1, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, width - 2, 0, width - 2, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, width - 1, 0, width - 1, height - 1, 1);

                if (props.Header.Length > 0)
                {
                    for (int i = 0; i < props.Header.Length; i++)
                    {
                        string header = props.Header[i];
                        surface.DrawText_Centered(Colors.BrightWhite, header, props.HeaderSize, y);
                        y += props.HeaderLineHeight;
                    }
                    y += vertSpacing;
                }

                for (int i = 0; i < props.Options.Length; i++)
                {
                    if (i > 0)
                        y += optionSpacing;
                    string option = props.Options[i];
                    SKColor color = i == props.SelectionIndex ? Colors.FluorescentOrange : Colors.BrightWhite;
                    if (!props.EnabledOptions[i])
                        color = Colors.Gray;
                    surface.DrawText_Centered(color, option, props.OptionsSize, y);
                    y += props.OptionLineHeight;
                }

                frame.Blit(surface, (_frameWidth - width) / 2, (_frameHeight - height) / 2);
            }
        }

        /// <summary>
        /// Draws score-entry dialog.
        /// </summary>
        private void DrawInitialsEntry(Surface frame)
        {
            InitialsEntryProperties initialProps = _initialProps;
            if (initialProps == null)
                return;

            double spacing = 10;
            double lineHeight = 38;
            double charWidth = 60;
            double charHeight = 82;
            double width = 400;
            double height = (spacing * 4) + (lineHeight * 2) + charHeight + 4;
            string inits = initialProps.Initials.PadRight(3);

            using (Surface surface = new Surface(width, height, Colors.Black))
            {
                surface.DrawLine(Colors.BrightWhite, 0, 0, width - 1, 0, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 1, width - 1, 1, 1);
                surface.DrawLine(Colors.BrightWhite, 0, height - 2, width - 1, height - 2, 1);
                surface.DrawLine(Colors.BrightWhite, 0, height - 1, width - 1, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 0, 0, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, 1, 0, 1, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, width - 2, 0, width - 2, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, width - 1, 0, width - 1, height - 1, 1);

                surface.DrawText_Centered(Colors.BrightWhite, initialProps.Header[0], 28, spacing + 2);
                surface.DrawText_Centered(Colors.BrightWhite, initialProps.Header[1], 28, spacing + lineHeight + 2);
                using (Surface initials = new Surface(charWidth * 3, charHeight))
                {
                    using (Surface char1 = Surface.RenderText(Colors.FluorescentOrange, inits[0].ToString(), 64))
                    {
                        initials.Blit(char1, ((charWidth - char1.Width) / 2) + (charWidth * 0), (charHeight - char1.Height) / 2);
                    }
                    using (Surface char2 = Surface.RenderText(Colors.FluorescentOrange, inits[1].ToString(), 64))
                    {
                        initials.Blit(char2, ((charWidth - char2.Width) / 2) + (charWidth * 1), (charHeight - char2.Height) / 2);
                    }
                    using (Surface char3 = Surface.RenderText(Colors.FluorescentOrange, inits[2].ToString(), 64))
                    {
                        initials.Blit(char3, ((charWidth - char3.Width) / 2) + (charWidth * 2), (charHeight - char3.Height) / 2);
                    }
                    surface.Blit(initials, (surface.Width - initials.Width) / 2, (spacing * 2) + (lineHeight * 2) + 2);
                }

                frame.Blit(surface, (_frameWidth - width) / 2, (_frameHeight - height) / 2);
            }
        }

        /// <summary>
        /// Draws message box.
        /// </summary>
        private void DrawMessageBox(Surface frame)
        {
            MessageProperties messageProps = _messageProps;
            if (messageProps == null)
                return;

            using (Surface text = Surface.RenderText(Colors.BrightWhite, messageProps.Text, messageProps.Size))
            {
                double spacing = 25;
                double buttonHeight = 32;
                double width = text.Width + 4 + (spacing * 2);
                double height = text.Height + 4 + (spacing * 2);
                if (messageProps.Buttons != MessageButtons.None)
                    height += buttonHeight + (spacing * 1.5);

                using (Surface surface = new Surface(width, height, Colors.Black))
                {
                    surface.DrawLine(Colors.BrightWhite, 0, 0, width - 1, 0, 1);
                    surface.DrawLine(Colors.BrightWhite, 0, 1, width - 1, 1, 1);
                    surface.DrawLine(Colors.BrightWhite, 0, height - 2, width - 1, height - 2, 1);
                    surface.DrawLine(Colors.BrightWhite, 0, height - 1, width - 1, height - 1, 1);
                    surface.DrawLine(Colors.BrightWhite, 0, 0, 0, height - 1, 1);
                    surface.DrawLine(Colors.BrightWhite, 1, 0, 1, height - 1, 1);
                    surface.DrawLine(Colors.BrightWhite, width - 2, 0, width - 2, height - 1, 1);
                    surface.DrawLine(Colors.BrightWhite, width - 1, 0, width - 1, height - 1, 1);
                    surface.Blit(text, 2 + spacing, 2 + spacing);

                    if (messageProps.Buttons == MessageButtons.OK)
                    {
                        using (Surface buttonText = Surface.RenderText(Colors.FluorescentOrange, "ok", 24))
                        {
                            surface.Blit(buttonText, (surface.Width - buttonText.Width) / 2, 2 + spacing + text.Height + (spacing * 1.5));
                        }
                    }
                    else if (messageProps.Buttons >= MessageButtons.CancelOK)
                    {
                        string label1 = messageProps.Buttons == MessageButtons.CancelOK ? "cancel" : "no";
                        string label2 = messageProps.Buttons == MessageButtons.CancelOK ? "ok" : "yes";
                        SKColor color1 = messageProps.ButtonIndex == 0 ? Colors.FluorescentOrange : Colors.BrightWhite;
                        SKColor color2 = messageProps.ButtonIndex != 0 ? Colors.FluorescentOrange : Colors.BrightWhite;

                        using (Surface buttonText1 = Surface.RenderText(color1, label1, 24))
                        {
                            surface.Blit(buttonText1, spacing * 1, 2 + spacing + text.Height + (spacing * 1.5));
                        }
                        using (Surface buttonText2 = Surface.RenderText(color2, label2, 24))
                        {
                            surface.Blit(buttonText2, surface.Width - buttonText2.Width - (spacing * 1), 2 + spacing + text.Height + (spacing * 1.5));
                        }
                    }

                    frame.Blit(surface, (frame.Width - surface.Width) / 2, (frame.Height - surface.Height) / 2);
                }
            }
        }

        /// <summary>
        /// Draws discovery lobby.
        /// </summary>
        private void DrawLobbyMenu(Surface frame)
        {
            LobbyProperties lobbyProps = _lobbyProps;
            if (lobbyProps == null)
                return;

            double width = 400;
            double height = 340;
            double y;

            using (Surface surface = new Surface(width, height, Colors.Black))
            {
                surface.DrawLine(Colors.BrightWhite, 0, 0, width - 1, 0, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 1, width - 1, 1, 1);
                surface.DrawLine(Colors.BrightWhite, 0, height - 2, width - 1, height - 2, 1);
                surface.DrawLine(Colors.BrightWhite, 0, height - 1, width - 1, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, 0, 0, 0, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, 1, 0, 1, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, width - 2, 0, width - 2, height - 1, 1);
                surface.DrawLine(Colors.BrightWhite, width - 1, 0, width - 1, height - 1, 1);

                surface.DrawText_Centered(Colors.BrightWhite, "choose opponent", 28, 25);
                surface.DrawText_Centered(Colors.BrightWhite, "other player must accept your invite", 12, 60);

                IReadOnlyList<Player> players = lobbyProps.GetPlayers();
                for (int i = 0; i < players.Count; i++)
                {
                    string ip = players[i].IP.ToString();
                    string initials = players[i].Name;
                    y = 100 + (i * 32);
                    SKColor color = lobbyProps.PlayerIndex == i ? Colors.FluorescentOrange : Colors.BrightWhite;
                    surface.DrawText_Left(color, initials, 24, y, 25);
                    surface.DrawText_Right(color, ip, 24, y, 25);
                }

                y = 100 + (6 * 32) - 6;
                SKColor color1 = lobbyProps.ButtonIndex == 0 ? Colors.FluorescentOrange : Colors.BrightWhite;
                SKColor color2 = lobbyProps.ButtonIndex != 0 ? Colors.FluorescentOrange : Colors.BrightWhite;
                if ((players.Count == 0) && (lobbyProps.ButtonIndex != 1))
                    color2 = Colors.Gray;
                surface.DrawText_Left(color1, "cancel", 24, y, 38);
                surface.DrawText_Right(color2, "ok", 24, y, 38);

                frame.Blit(surface, (frame.Width - surface.Width) / 2, (frame.Height - surface.Height) / 2);
            }
        }

        /// <summary>
        /// Draws fps readout.
        /// </summary>
        private void DrawDebugInfo(Surface frame, GameCommunications communications)
        {
            if (!RenderProps.Debug)
                return;

            List<string> lines = new List<string>();
            lines.Add($"fps:   {(int)_fps.CPS}");
            if (communications != null)
            {
                lines.Add($"heartbeats:   s={communications.HeartbeatsSent}, r={communications.HeartbeatsReceived}");
                lines.Add($"cmd_requests:   s={communications.CommandRequestsSent}, r={communications.CommandRequestsReceived}");
                lines.Add($"cmd_responses:   s={communications.CommandResponsesSent}, r={communications.CommandResponsesReceived}");
                lines.Add($"game_status:   s={communications.DataSent}, r={communications.DataReceived}");
                lines.Add($"com_state:   {communications.ConnectionState}");
            }

            using (Surface surface = Surface.RenderText(Colors.White, lines, 12))
            {
                frame.Blit(surface, 20, 25);
            }
        }

        /// <summary>
        /// Draws exploding spaces.
        /// </summary>
        private void DrawExplodingSpaces(Surface frame, List<ExplodingSpace> spaces)
        {
            if ((spaces == null) || (spaces.Count == 0))
                return;

            foreach (ExplodingSpace space in spaces)
            {
                double x = space.X;
                double y = space.Y;
                frame.DrawRect(space.Color, x, y, 34, 34);
                frame.DrawLine(Colors.Black, x, y, x + 34, y);
                frame.DrawLine(Colors.Black, x, y + 34, x + 34, y + 34);
                frame.DrawLine(Colors.Black, x, y, x, y + 34);
                frame.DrawLine(Colors.Black, x + 34, y, x + 34, y + 34);
            }
        }

    }
}

﻿using Bricker.Configuration;
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
        private double _frame_Width;
        private double _frame_Height;
        private double _displayScale;
        private readonly SKTypeface _typeface;
        private readonly SKPaint _linePaint;
        private readonly SKPaint _rectPaint;
        private readonly SKPaint _textPaint;
        private MenuProperties _menuProps;
        private InitialsEntryProperties _initialProps;
        private MessageProperties _messageProps;
        private LobbyProperties _lobbyProps;
        private readonly CpsCalculator _fps;
        private readonly List<BackgroundTile> _tiles;
        private double _frame_XCenter;
        private double _frame_YCenter;
        private double _player_XCenter;
        private double _player_YCenter;
        private double _player_Width;
        private double _player_Height;
        private double _next_Width;
        private double _next_Height;
        private double _hold_Width;
        private double _hold_Height;
        private double _player_TotalWidth;
        private double _left_Center1;
        private double _left_Center2;
        private double _right_Center1;
        private double _title_XCenter;
        private double _title_YCenter;
        private double _controls_XCenter;
        private double _controls_YCenter;
        private double _opponent_XCenter;
        private double _level_XCenter;
        private double _level_YCenter;
        private double _lines_XCenter;
        private double _lines_YCenter;
        private double _score_XCenter;
        private double _score_YCenter;
        private double _highScores_XCenter;
        private double _highScores_YCenter;
        private readonly bool _fakeOpponent = false;
        private bool _menuUp => _menuProps != null || _initialProps != null || _messageProps != null || _lobbyProps != null;
        private SKColor _primaryWhite => !_menuUp ? Colors.White : Colors.DimWhite;

        //public
        public double FrameWidth => _frame_Width;
        public double FrameHeight => _frame_Height;
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
            _tiles = new List<BackgroundTile>();
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
        public void DrawFrame(SKPaintSurfaceEventArgs e, Matrix matrix, GameStats stats, List<ExplodingSpace> spaces, GameCommunications communications, Opponent opponent, GameState gameState)
        {
            try
            {
                //create fake opponent for rendering
                if ((_fakeOpponent) && (opponent == null))
                {
                    opponent = new Opponent(new Player(_config.GameTitle, _config.GameVersion, _config.LocalIP, _config.GamePort, "OPN"));
                    byte[,] m = (byte[,])matrix.Grid.Clone();
                    opponent.UpdateOpponent(m, 3, 144, 12434, 7, false);
                }

                //vars
                SKImageInfo info = e.Info;
                _displayScale = GetDisplayScale();
                if (RenderProps.DisplayScale != _displayScale)
                    RenderProps.DisplayScale = _displayScale;
                _frame_Width = info.Width / _displayScale;
                _frame_Height = info.Height / _displayScale;
                _frame_XCenter = _frame_Width / 2;
                _frame_YCenter = _frame_Height / 2;
                _player_XCenter = _frame_XCenter;
                _player_YCenter = _frame_YCenter;
                _player_Width = 333;
                _player_Height = 663;
                _next_Width = 132;
                _next_Height = _player_Height;
                _hold_Width = 132;
                _hold_Height = _player_Height;
                _player_TotalWidth = _hold_Width - 2 + _player_Width - 2 + _next_Width;
                _left_Center1 = (_frame_Width - _player_TotalWidth) / 4;
                _left_Center2 = (_frame_Width - _player_Width) / 4;
                _right_Center1 = _frame_Width - ((_frame_Width - _player_TotalWidth) / 4);
                _title_XCenter = _left_Center1;
                _title_YCenter = 88;
                _controls_XCenter = _left_Center1;
                _controls_YCenter = 465;
                _opponent_XCenter = _left_Center1;
                _level_XCenter = _right_Center1;
                _level_YCenter = 75;
                _lines_XCenter = _right_Center1;
                _lines_YCenter = 193;
                _score_XCenter = _right_Center1;
                _score_YCenter = 311;
                _highScores_XCenter = _right_Center1;
                _highScores_YCenter = 532;

                //create surface
                Surface frame = new Surface(e.Surface.Canvas, _frame_Width, _frame_Height, Colors.Black);

                //fps
                _fps.Increment();

                //background
                DrawBackground(frame, stats);

                //game matrix
                DrawMatrix(frame, matrix);

                //hold
                DrawHold(frame, matrix);

                //next
                DrawNext(frame, matrix);

                //opponent matrix
                DrawOpponentMatrix(frame, opponent);

                //exploding spaces
                DrawExplodingSpaces(frame, spaces);

                //title
                DrawTitle(frame);

                //controls
                DrawControls(frame, opponent);

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
                DrawDebugInfo(frame, communications, gameState);
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
            using (Surface surface = new Surface(_player_Width, _player_Height, Colors.Black))
            {
                for (int x = 1; x < 12; x++)
                    for (int y = 1; y < 22; y++)
                        if (matrix.Grid[x, y] > 0)
                            surface.DrawRect(Brick.BrickToColor(matrix.Grid[x, y]), ((x - 1) * 33) + 2, ((y - 1) * 33) + 2, 32, 32);

                Brick brick = matrix.Brick;
                if (brick != null)
                    for (int x = 0; x < brick.Width; x++)
                        for (int y = 0; y < brick.Height; y++)
                            if (brick.Grid[x, y] > 0)
                                surface.DrawRect(brick.Color, ((brick.X - 1 + x) * 33) + 2, ((brick.Y - 1 + y) * 33) + 2, 32, 32);
                            else if (RenderProps.Debug)
                                surface.DrawRect(Colors.White, ((brick.X - 1 + x) * 33) + 17, ((brick.Y - 1 + y) * 33) + 17, 2, 2);

                for (int i = 1; i <= 10; i++)
                    surface.DrawLine(Colors.Gray, (i * 33) + 1, 2, (i * 33) + 1, 660, 1);
                for (int i = 1; i <= 20; i++)
                    surface.DrawLine(Colors.Gray, 2, (i * 33) + 1, 330, (i * 33) + 1, 1);

                surface.DrawLine(_primaryWhite, 0, 0, 332, 0, 1);
                surface.DrawLine(_primaryWhite, 0, 1, 332, 1, 1);
                surface.DrawLine(_primaryWhite, 0, 0, 0, 662, 1);
                surface.DrawLine(_primaryWhite, 1, 0, 1, 662, 1);
                surface.DrawLine(_primaryWhite, 0, 662, 332, 662, 1);
                surface.DrawLine(_primaryWhite, 0, 661, 332, 661, 1);
                surface.DrawLine(_primaryWhite, 332, 0, 332, 662, 1);
                surface.DrawLine(_primaryWhite, 331, 0, 331, 662, 1);

                frame.Blit(surface, _player_XCenter - (_player_Width / 2), _player_YCenter - (_player_Height / 2));
            }
        }

        /// <summary>
        /// Draws next brick readout.
        /// </summary>
        private void DrawHold(Surface frame, Matrix matrix)
        {
            double titleSpacing = 63;
            double brickArea = 80;
            using (Surface surface = new Surface(_hold_Width, _hold_Height, Colors.AlphaBlack192))
            {
                surface.DrawLine(_primaryWhite, 0, 0, _hold_Width - 1, 0, 1);
                surface.DrawLine(_primaryWhite, 0, 1, _hold_Width - 1, 1, 1);
                surface.DrawLine(_primaryWhite, 0, _hold_Height - 2, _hold_Width - 1, _hold_Height - 2, 1);
                surface.DrawLine(_primaryWhite, 0, _hold_Height - 1, _hold_Width - 1, _hold_Height - 1, 1);
                surface.DrawLine(_primaryWhite, 0, 0, 0, _hold_Height - 1, 1);
                surface.DrawLine(_primaryWhite, 1, 0, 1, _hold_Height - 1, 1);
                surface.DrawLine(_primaryWhite, _hold_Width - 2, 0, _hold_Width - 2, _hold_Height - 1, 1);
                surface.DrawLine(_primaryWhite, _hold_Width - 1, 0, _hold_Width - 1, _hold_Height - 1, 1);
                surface.DrawLine(_primaryWhite, 0, 160, _hold_Width - 1, 160, 1);
                surface.DrawLine(_primaryWhite, 0, 161, _hold_Width - 1, 161, 1);
                surface.DrawText_Centered(_primaryWhite, "hold", 28, 20);

                if (matrix.Hold != null)
                {
                    using (Surface container = new Surface(brickArea, brickArea))
                    {
                        byte[,] grid = ReduceBrick(matrix.Hold.Grid);
                        double swidth = (grid.GetLength(0) * 25) + 1;
                        double sheight = (grid.GetLength(1) * 25) + 1;
                        using (Surface brick = new Surface(swidth, sheight))
                        {
                            for (int x = 0; x < grid.GetLength(0); x++)
                                for (int y = 0; y < grid.GetLength(1); y++)
                                    if (grid[x, y] > 0)
                                        brick.DrawRect(Brick.BrickToColor(grid[x, y]), x * 25, y * 25, 24, 24);
                            container.Blit(brick, (brickArea - swidth) / 2, (brickArea - sheight) / 2);
                        }
                        surface.Blit(container, (_next_Width - brickArea) / 2, titleSpacing);
                    }
                }
                frame.Blit(surface, _player_XCenter - (_player_Width / 2) - _hold_Width + 2, _player_YCenter - (_player_Height / 2));
            }
        }

        /// <summary>
        /// Draws next brick readout.
        /// </summary>
        private void DrawNext(Surface frame, Matrix matrix)
        {
            double titleSpacing = 63;
            double brickArea = 80;
            double brickSpacing = 10;
            Brick[] nextBricks = matrix.GetNextBricks();
            using (Surface surface = new Surface(_next_Width, _next_Height, Colors.AlphaBlack192))
            {
                surface.DrawLine(_primaryWhite, 0, 0, _next_Width - 1, 0, 1);
                surface.DrawLine(_primaryWhite, 0, 1, _next_Width - 1, 1, 1);
                surface.DrawLine(_primaryWhite, 0, _next_Height - 2, _next_Width - 1, _next_Height - 2, 1);
                surface.DrawLine(_primaryWhite, 0, _next_Height - 1, _next_Width - 1, _next_Height - 1, 1);
                surface.DrawLine(_primaryWhite, 0, 0, 0, _next_Height - 1, 1);
                surface.DrawLine(_primaryWhite, 1, 0, 1, _next_Height - 1, 1);
                surface.DrawLine(_primaryWhite, _next_Width - 2, 0, _next_Width - 2, _next_Height - 1, 1);
                surface.DrawLine(_primaryWhite, _next_Width - 1, 0, _next_Width - 1, _next_Height - 1, 1);
                surface.DrawText_Centered(_primaryWhite, "next", 28, 20);

                for (int i = 0; i < nextBricks.Length; i++)
                {
                    using (Surface container = new Surface(brickArea, brickArea))
                    {
                        byte[,] grid = ReduceBrick(nextBricks[i].Grid);
                        double swidth = (grid.GetLength(0) * 25) + 1;
                        double sheight = (grid.GetLength(1) * 25) + 1;
                        using (Surface brick = new Surface(swidth, sheight))
                        {
                            for (int x = 0; x < grid.GetLength(0); x++)
                                for (int y = 0; y < grid.GetLength(1); y++)
                                    if (grid[x, y] > 0)
                                        brick.DrawRect(Brick.BrickToColor(grid[x, y]), x * 25, y * 25, 24, 24);
                            container.Blit(brick, (brickArea - swidth) / 2, (brickArea - sheight) / 2);
                        }
                        surface.Blit(container, (_next_Width - brickArea) / 2, titleSpacing + ((brickArea + brickSpacing) * i));
                    }
                }
                frame.Blit(surface, _player_XCenter + (_player_Width / 2) - 2, _player_YCenter - (_player_Height / 2));
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
            double brickSize = 20d;
            double matrixWidth = 2d + (brickSize * 10d) + 9d + 2d;
            double matrixHeight = 2d + (brickSize * 20d) + 19d + 2d;

            double statsHeight = 21;
            double textSpacing = -2;
            double betweenSpacing = 10;
            double headerHeight = (statsHeight * 3) + (textSpacing * 2);

            double width = matrixWidth;
            double height = headerHeight + betweenSpacing + matrixHeight;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(_primaryWhite, opponent.Player.Name, 32, 23);
                using (Surface statsSurface = new Surface(125, headerHeight))
                {
                    statsSurface.DrawText_Left(_primaryWhite, "level", 16, (statsHeight + textSpacing) * 0);
                    statsSurface.DrawText_Left(_primaryWhite, "lines", 16, (statsHeight + textSpacing) * 1);
                    statsSurface.DrawText_Left(_primaryWhite, "score", 16, (statsHeight + textSpacing) * 2);
                    statsSurface.DrawText_Right(_primaryWhite, $"{opponent.Level}", 16, (statsHeight + textSpacing) * 0);
                    statsSurface.DrawText_Right(_primaryWhite, $"{opponent.Lines.ToString("N0")}", 16, (statsHeight + textSpacing) * 1);
                    statsSurface.DrawText_Right(_primaryWhite, $"{opponent.Score.ToString("N0")}", 16, (statsHeight + textSpacing) * 2);
                    surface.Blit(statsSurface, width - statsSurface.Width, 0);
                }
                using (Surface matrixSurface = new Surface(matrixWidth, matrixHeight, Colors.Black))
                {
                    for (int x = 1; x < 12; x++)
                        for (int y = 1; y < 22; y++)
                            if (matrix[x, y] > 0)
                                matrixSurface.DrawRect(Brick.BrickToColor(matrix[x, y]), ((x - 1d) * (brickSize + 1d)) + 1.5d, ((y - 1d) * (brickSize + 1)) + 1.5d, brickSize, brickSize);
                    matrixSurface.DrawLine(_primaryWhite, 0.5d, 0.5d, matrixWidth - 1.5d, 0.5d, 2d);
                    matrixSurface.DrawLine(_primaryWhite, matrixWidth - 1.5d, 0.5d, matrixWidth - 1.5d, matrixHeight - 1.5d, 2d);
                    matrixSurface.DrawLine(_primaryWhite, matrixWidth - 1.5d, matrixHeight - 1.5d, 0.5d, matrixHeight - 1.5d, 2d);
                    matrixSurface.DrawLine(_primaryWhite, 0.5d, matrixHeight - 1.5d, 0.5d, 0.5d, 2d);
                    surface.Blit(matrixSurface, (width - matrixWidth) / 2d, headerHeight + betweenSpacing);
                }
                frame.Blit(surface, _opponent_XCenter - (width / 2), _frame_Height - ((_frame_Height - _player_Height) / 2) - height - 15);
            }
        }

        /// <summary>
        /// Draws game title.
        /// </summary>
        private void DrawTitle(Surface frame)
        {
            double titleHeight = 86;
            double space = -28;
            double copyrightHeight = 16;
            double width = 280;
            double height = titleHeight + space + copyrightHeight;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Centered(_primaryWhite, "bricker", 52, 0);
                surface.DrawText_Centered(_primaryWhite, $"v{_config.DisplayVersion}  © 2017-2020  john hyland", 10, titleHeight + space);
                frame.Blit(surface, _title_XCenter - (width / 2), _title_YCenter - (height / 2));
            }
        }

        /// <summary>
        /// Draws controls readout.
        /// </summary>
        private void DrawControls(Surface frame, Opponent opponent)
        {
            if (opponent != null)
                return;

            double width = 210;
            double height = 230;
            double titleSpacing = 54;
            double lineSpacing = 23;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(_primaryWhite, "controls", 28, 0);
                surface.DrawText_Left(_primaryWhite, "left", 18, titleSpacing + (lineSpacing * 0), 10);
                surface.DrawText_Left(_primaryWhite, "right", 18, titleSpacing + (lineSpacing * 1), 10);
                surface.DrawText_Left(_primaryWhite, "down", 18, titleSpacing + (lineSpacing * 2), 10);
                surface.DrawText_Left(_primaryWhite, "rotate", 18, titleSpacing + (lineSpacing * 3), 10);
                surface.DrawText_Left(_primaryWhite, "drop", 18, titleSpacing + (lineSpacing * 4), 10);
                surface.DrawText_Left(_primaryWhite, "hold", 18, titleSpacing + (lineSpacing * 5), 10);
                surface.DrawText_Left(_primaryWhite, "pause", 18, titleSpacing + (lineSpacing * 6), 10);
                surface.DrawText_Right(_primaryWhite, "left", 18, titleSpacing + (lineSpacing * 0), 10);
                surface.DrawText_Right(_primaryWhite, "right", 18, titleSpacing + (lineSpacing * 1), 10);
                surface.DrawText_Right(_primaryWhite, "down", 18, titleSpacing + (lineSpacing * 2), 10);
                surface.DrawText_Right(_primaryWhite, "up", 18, titleSpacing + (lineSpacing * 3), 10);
                surface.DrawText_Right(_primaryWhite, "space", 18, titleSpacing + (lineSpacing * 4), 10);
                surface.DrawText_Right(_primaryWhite, "c", 18, titleSpacing + (lineSpacing * 5), 10);
                surface.DrawText_Right(_primaryWhite, "esc", 18, titleSpacing + (lineSpacing * 6), 10);
                frame.Blit(surface, _controls_XCenter - (width / 2), _controls_YCenter - (height / 2));
            }
        }

        /// <summary>
        /// Draws current level readout.
        /// </summary>
        private void DrawLevel(Surface frame, GameStats stats)
        {
            double width = 210;
            double height = 80;
            double space = 28;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(_primaryWhite, "level", 28, 0);
                surface.DrawText_Right(_primaryWhite, stats.Level.ToString("N0"), 42, space);
                frame.Blit(surface, _level_XCenter - (width / 2), _level_YCenter - (height / 2));
            }
        }

        /// <summary>
        /// Draws current lines readout.
        /// </summary>
        private void DrawLines(Surface frame, GameStats stats)
        {
            double width = 210;
            double height = 80;
            double space = 28;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(_primaryWhite, "lines", 28, 0);
                surface.DrawText_Right(_primaryWhite, stats.Lines.ToString("N0"), 42, space);
                frame.Blit(surface, _lines_XCenter - (width / 2), _lines_YCenter - (height / 2));
            }
        }

        /// <summary>
        /// Draws current score readout.
        /// </summary>
        private void DrawScore(Surface frame, GameStats stats)
        {
            double width = 210;
            double height = 80;
            double space = 28;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(_primaryWhite, "score", 28, 0);
                surface.DrawText_Right(_primaryWhite, stats.Score.ToString("N0"), 42, space);
                frame.Blit(surface, _score_XCenter - (width / 2), _score_YCenter - (height / 2));
            }
        }

        /// <summary>
        /// Draws high scores readout.
        /// </summary>
        private void DrawHighScores(Surface frame, GameStats stats)
        {
            double width = 210;
            double height = 286;
            double titleSpacing = 54;
            double lineSpacing = 23;

            using (Surface surface = new Surface(width, height))
            {
                surface.DrawText_Left(_primaryWhite, "high scores", 28, 0);
                for (int i = 0; i < stats.HighScores.Count; i++)
                {
                    HighScore score = stats.HighScores[i];
                    surface.DrawText_Left(_primaryWhite, score.Initials, 18, titleSpacing + (lineSpacing * i), 10);
                    surface.DrawText_Right(_primaryWhite, score.Score.ToString("N0"), 18, titleSpacing + (lineSpacing * i), 10);
                }
                frame.Blit(surface, _highScores_XCenter - (width / 2), _highScores_YCenter - (height / 2));
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

            double vertSpacing = 26;
            double horizSpacing = 42;
            double optionSpacing = 22;

            if ((props.Width is Double.NaN) || (props.Height is Double.NaN) || (props.CalculatedHeight is Double.NaN))
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
                props.CalculatedHeight = 2 + totalHeight + vertSpacing + headerHeight + (optionSpacing * (props.Options.Length - 1)) + vertSpacing + 2;
                props.Height = Math.Max(props.Height is Double.NaN ? props.CalculatedHeight : props.Height, props.CalculatedHeight);
                props.HeaderLineHeight = headerLineHeight;
                props.OptionLineHeight = optionLineHeight;
            }

            double width = props.Width, height = props.Height;
            double y = vertSpacing + ((props.Height - props.CalculatedHeight) / 2d);
            using (Surface surface = new Surface(width, height, Colors.Black))
            {
                surface.DrawLine(Colors.White, 0, 0, width - 1, 0, 1);
                surface.DrawLine(Colors.White, 0, 1, width - 1, 1, 1);
                surface.DrawLine(Colors.White, 0, height - 2, width - 1, height - 2, 1);
                surface.DrawLine(Colors.White, 0, height - 1, width - 1, height - 1, 1);
                surface.DrawLine(Colors.White, 0, 0, 0, height - 1, 1);
                surface.DrawLine(Colors.White, 1, 0, 1, height - 1, 1);
                surface.DrawLine(Colors.White, width - 2, 0, width - 2, height - 1, 1);
                surface.DrawLine(Colors.White, width - 1, 0, width - 1, height - 1, 1);

                if (props.Header.Length > 0)
                {
                    for (int i = 0; i < props.Header.Length; i++)
                    {
                        string header = props.Header[i];
                        surface.DrawText_Centered(Colors.White, header, props.HeaderSize, y);
                        y += props.HeaderLineHeight;
                    }
                    y += vertSpacing;
                }

                for (int i = 0; i < props.Options.Length; i++)
                {
                    if (i > 0)
                        y += optionSpacing;
                    string option = props.Options[i];
                    SKColor color = i == props.SelectionIndex ? Colors.FluorescentOrange : Colors.White;
                    if (!props.EnabledOptions[i])
                        color = Colors.Gray;
                    surface.DrawText_Centered(color, option, props.OptionsSize, y);
                    y += props.OptionLineHeight;
                }

                frame.Blit(surface, (_frame_Width - width) / 2, (_frame_Height - height) / 2);
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


            double vSpacing = 32;
            double line1Height = 48;
            double lineSpacing = 0;
            double line2Height = 38;
            double middleSpacing = 14;
            double charWidth = 60;
            double charHeight = 82;
            double width = 420;
            double height = 2 + vSpacing + line1Height + lineSpacing + line2Height + middleSpacing + charHeight + vSpacing + 2;
            double line1Y = 2 + vSpacing;
            double line2Y = line1Y + line1Height + lineSpacing;
            double charY = line2Y + line2Height + middleSpacing;

            string inits = initialProps.Initials.PadRight(3);

            using (Surface surface = new Surface(width, height, Colors.Black))
            {
                surface.DrawLine(Colors.White, 0, 0, width - 1, 0, 1);
                surface.DrawLine(Colors.White, 0, 1, width - 1, 1, 1);
                surface.DrawLine(Colors.White, 0, height - 2, width - 1, height - 2, 1);
                surface.DrawLine(Colors.White, 0, height - 1, width - 1, height - 1, 1);
                surface.DrawLine(Colors.White, 0, 0, 0, height - 1, 1);
                surface.DrawLine(Colors.White, 1, 0, 1, height - 1, 1);
                surface.DrawLine(Colors.White, width - 2, 0, width - 2, height - 1, 1);
                surface.DrawLine(Colors.White, width - 1, 0, width - 1, height - 1, 1);

                surface.DrawText_Centered(Colors.White, initialProps.Header[0], 36, line1Y);
                surface.DrawText_Centered(Colors.White, initialProps.Header[1], 28, line2Y);
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
                    surface.Blit(initials, (surface.Width - initials.Width) / 2, charY);
                }

                frame.Blit(surface, (_frame_Width - width) / 2, (_frame_Height - height) / 2);
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

            using (Surface text = Surface.RenderText(Colors.White, messageProps.Text, messageProps.Size))
            {
                double spacing = 25;
                double buttonHeight = 32;
                double width = text.Width + 4 + (spacing * 2);
                double height = text.Height + 4 + (spacing * 2);
                if (messageProps.Buttons != MessageButtons.None)
                    height += buttonHeight + (spacing * 1.5);

                using (Surface surface = new Surface(width, height, Colors.Black))
                {
                    surface.DrawLine(Colors.White, 0, 0, width - 1, 0, 1);
                    surface.DrawLine(Colors.White, 0, 1, width - 1, 1, 1);
                    surface.DrawLine(Colors.White, 0, height - 2, width - 1, height - 2, 1);
                    surface.DrawLine(Colors.White, 0, height - 1, width - 1, height - 1, 1);
                    surface.DrawLine(Colors.White, 0, 0, 0, height - 1, 1);
                    surface.DrawLine(Colors.White, 1, 0, 1, height - 1, 1);
                    surface.DrawLine(Colors.White, width - 2, 0, width - 2, height - 1, 1);
                    surface.DrawLine(Colors.White, width - 1, 0, width - 1, height - 1, 1);
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
                        SKColor color1 = messageProps.ButtonIndex == 0 ? Colors.FluorescentOrange : Colors.White;
                        SKColor color2 = messageProps.ButtonIndex != 0 ? Colors.FluorescentOrange : Colors.White;

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
            double height = 380;
            double y;

            using (Surface surface = new Surface(width, height, Colors.Black))
            {
                surface.DrawLine(Colors.White, 0, 0, width - 1, 0, 1);
                surface.DrawLine(Colors.White, 0, 1, width - 1, 1, 1);
                surface.DrawLine(Colors.White, 0, height - 2, width - 1, height - 2, 1);
                surface.DrawLine(Colors.White, 0, height - 1, width - 1, height - 1, 1);
                surface.DrawLine(Colors.White, 0, 0, 0, height - 1, 1);
                surface.DrawLine(Colors.White, 1, 0, 1, height - 1, 1);
                surface.DrawLine(Colors.White, width - 2, 0, width - 2, height - 1, 1);
                surface.DrawLine(Colors.White, width - 1, 0, width - 1, height - 1, 1);

                surface.DrawText_Centered(Colors.White, "choose opponent", 28, 25);
                surface.DrawText_Centered(Colors.White, "other player must accept your invite", 12, 60);

                IReadOnlyList<Player> players = lobbyProps.GetPlayers();
                for (int i = 0; i < players.Count; i++)
                {
                    string ip = players[i].IP.ToString();
                    string initials = players[i].Name;
                    y = 100 + (i * 32);
                    SKColor color = lobbyProps.PlayerIndex == i ? Colors.FluorescentOrange : Colors.White;
                    surface.DrawText_Left(color, initials, 24, y, 25);
                    surface.DrawText_Right(color, ip, 24, y, 25);
                }

                y = 100 + (6 * 32) - 6;
                SKColor color1 = lobbyProps.ButtonIndex == 0 ? Colors.FluorescentOrange : Colors.White;
                SKColor color2 = lobbyProps.ButtonIndex != 0 ? Colors.FluorescentOrange : Colors.White;
                if ((players.Count == 0) && (lobbyProps.ButtonIndex != 1))
                    color2 = Colors.Gray;
                surface.DrawText_Left(color1, "cancel", 24, height - 58, 38);
                surface.DrawText_Right(color2, "ok", 24, height - 58, 38);

                frame.Blit(surface, (frame.Width - surface.Width) / 2, (frame.Height - surface.Height) / 2);
            }
        }

        /// <summary>
        /// Draws fps readout.
        /// </summary>
        private void DrawDebugInfo(Surface frame, GameCommunications communications, GameState gameState)
        {
            if (!RenderProps.Debug)
                return;

            List<string> lines = new List<string>();
            lines.Add($"fps:   {(int)_fps.CPS}");
            lines.Add($"game_state:   {gameState}");
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
                frame.Blit(surface, 35, 25);
            }
        }

        /// <summary>
        /// Draws the background.
        /// </summary>
        private void DrawBackground(Surface frame, GameStats stats)
        {
            if (_tiles.Count == 0)
                for (int i = 0; i < 50; i++)
                    _tiles.Add(new BackgroundTile());

            DateTime now = DateTime.Now;
            foreach (BackgroundTile tile in _tiles)
            {
                tile.Move(now, stats.Level);
                using (Surface surface = new Surface(tile.Size, tile.Size, tile.Color))
                {
                    frame.Blit(surface, tile.X - (tile.Size / 2d), tile.Y - (tile.Size / 2d));
                }
            }
        }

        /// <summary>
        /// Draws exploding spaces.
        /// </summary>
        private static void DrawExplodingSpaces(Surface frame, List<ExplodingSpace> spaces)
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

        /// <summary>
        /// Removes empty rows and columns from specified brick, returning a smaller grid.
        /// </summary>
        private static byte[,] ReduceBrick(byte[,] brick)
        {
            int left = Int32.MaxValue, right = Int32.MinValue, top = Int32.MaxValue, bottom = Int32.MinValue;
            for (int x = 0; x < brick.GetLength(0); x++)
            {
                for (int y = 0; y < brick.GetLength(1); y++)
                {
                    if (brick[x, y] > 0)
                    {
                        left = x < left ? x : left;
                        right = x > right ? x : right;
                        top = y < top ? y : top;
                        bottom = y > bottom ? y : bottom;
                    }
                }
            }
            int width = (right - left) + 1;
            int height = (bottom - top) + 1;
            int xOffset = left;
            int yOffset = top;
            byte[,] reduced = new byte[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    reduced[x, y] = brick[x + xOffset, y + yOffset];
            return reduced;
        }

    }
}

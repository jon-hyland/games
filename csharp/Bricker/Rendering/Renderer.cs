using Bricker.Configuration;
using Bricker.Game;
using Bricker.Rendering.Properties;
using Bricker.Rendering.Tiles;
using Common.Standard.Configuration;
using Common.Standard.Error;
using Common.Standard.Game;
using Common.Standard.Networking;
using Common.Standard.Utilities;
using Common.Windows.Rendering;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Dictionary<int, Dictionary<Space, Surface>> _spaces;
        private MenuProperties _menuProps;
        private SettingsProperties _settingProps;
        private InitialsEntryProperties _initialProps;
        private MessageProperties _messageProps;
        private LobbyProperties _lobbyProps;
        private readonly CpsCalculator _fps;
        private readonly Images _images;
        private readonly List<ITile> _tiles;
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
        public SettingsProperties SettingProps { get => _settingProps; set => _settingProps = value; }
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
            _spaces = new Dictionary<int, Dictionary<Space, Surface>>();
            _spaces.Add(32, new Dictionary<Space, Surface>());
            _spaces.Add(24, new Dictionary<Space, Surface>());
            _menuProps = null;
            _initialProps = null;
            _messageProps = null;
            _fps = new CpsCalculator(1);
            _images = new Images(config);
            _tiles = new List<ITile>();
            for (int i = 0; i < 50; i++)
                _tiles.Add(new SolidTile());
            if (_images.FileCount > 0)
                for (int i = 0; i < 3; i++)
                    _tiles.Add(new ImageTile(_images));
        }

        /// <summary>
        /// Cleans up resources.
        /// </summary>
        public void Dispose()
        {
            RenderProps.Dispose();
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
                    opponent = new Opponent(new Player(_config.LocalIP, _config.GameTitle, _config.GameVersion, "OPN"));
                    Space[,] m = matrix.GetGrid(includeBrick: true, includeGhost: GameConfig.Instance.Ghost);
                    opponent.UpdateOpponent(m, 3, 144, 12434, 7);
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
                _player_Width = 324;
                _player_Height = 644;
                _next_Width = 132;
                _next_Height = _player_Height;
                _hold_Width = 132;
                _hold_Height = _player_Height;
                _player_TotalWidth = _hold_Width - 2 + _player_Width - 2 + _next_Width;
                _left_Center1 = ((_frame_Width - _player_TotalWidth) / 4) - 1;
                _left_Center2 = ((_frame_Width - _player_Width) / 4) - 1;
                _right_Center1 = (_frame_Width - ((_frame_Width - _player_TotalWidth) / 4)) + 1;
                _title_XCenter = _left_Center1;
                _title_YCenter = opponent == null ? 88 : 52;
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

                //settings
                DrawSettings(frame);

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
        /// Generates a prerendered surface to repesent each type of space (small colored square).
        /// </summary>
        private Surface CreateBrickSurface(Space space, int size)
        {
            double x = 0;
            double y = 0;
            Surface surface = new Surface(size, size, Colors.Transparent);
            if (space.IsStandard())
            {
                SKColor color = Brick.SpaceToColor(space);
                SKColor lighter = Colors.GetLighter(color);
                SKColor darker = Colors.GetDarker(color);
                surface.DrawRect(color, x, y, size, size);
                surface.DrawLine(lighter, x + 0, y + 0, x + size - 0, y + 0, 1);
                surface.DrawLine(lighter, x + 1, y + 1, x + size - 1, y + 1, 1);
                surface.DrawLine(lighter, x + 2, y + 2, x + size - 2, y + 2, 1);
                surface.DrawLine(lighter, x + size - 0, y + 0, x + size - 0, y + size - 0, 1);
                surface.DrawLine(lighter, x + size - 1, y + 1, x + size - 1, y + size - 1, 1);
                surface.DrawLine(lighter, x + size - 2, y + 2, x + size - 2, y + size - 2, 1);
                surface.DrawLine(darker, x + 0, y + size - 0, x + size - 0, y + size - 0, 1);
                surface.DrawLine(darker, x + 1, y + size - 1, x + size - 1, y + size - 1, 1);
                surface.DrawLine(darker, x + 2, y + size - 2, x + size - 2, y + size - 2, 1);
                surface.DrawLine(darker, x + 0, y + 0, x + 0, y + size - 0, 1);
                surface.DrawLine(darker, x + 1, y + 1, x + 1, y + size - 1, 1);
                surface.DrawLine(darker, x + 2, y + 2, x + 2, y + size - 2, 1);
            }
            else if (space.IsGhost())
            {
                SKColor color = Brick.SpaceToColor(space);
                SKColor lighter = Colors.GetLighter(color);
                SKColor darker = Colors.GetDarker(color);
                surface.DrawRect(color, x, y, size, size);
                surface.DrawLine(lighter, x + 0, y + 0, x + size - 0, y + 0, 1);
                surface.DrawLine(lighter, x + 1, y + 1, x + size - 1, y + 1, 1);
                surface.DrawLine(lighter, x + 2, y + 2, x + size - 2, y + 2, 1);
                surface.DrawLine(lighter, x + size - 0, y + 0, x + size - 0, y + size - 0, 1);
                surface.DrawLine(lighter, x + size - 1, y + 1, x + size - 1, y + size - 1, 1);
                surface.DrawLine(lighter, x + size - 2, y + 2, x + size - 2, y + size - 2, 1);
                surface.DrawLine(darker, x + 0, y + size - 0, x + size - 0, y + size - 0, 1);
                surface.DrawLine(darker, x + 1, y + size - 1, x + size - 1, y + size - 1, 1);
                surface.DrawLine(darker, x + 2, y + size - 2, x + size - 2, y + size - 2, 1);
                surface.DrawLine(darker, x + 0, y + 0, x + 0, y + size - 0, 1);
                surface.DrawLine(darker, x + 1, y + 1, x + 1, y + size - 1, 1);
                surface.DrawLine(darker, x + 2, y + 2, x + 2, y + size - 2, 1);
            }
            return surface;
        }

        /// <summary>
        /// Draws the game matrix, once per frame.
        /// </summary>
        private void DrawMatrix(Surface frame, Matrix matrix)
        {
            using (Surface surface = new Surface(_player_Width, _player_Height, Colors.Black))
            {
                Space[,] grid = matrix.GetGrid(includeBrick: true, includeGhost: GameConfig.Instance.Ghost);
                for (int i = 0; i <= 10; i++)
                    surface.DrawLine(Colors.Gray, (i * 32) + 2, 0, (i * 32) + 2, surface.Height, 1);
                for (int i = 0; i <= 20; i++)
                    surface.DrawLine(Colors.Gray, 0, (i * 32) + 2, surface.Width, (i * 32) + 2, 1);
                for (int x = 1; x < 12; x++)
                {
                    for (int y = 1; y < 22; y++)
                    {
                        if (grid[x, y].Exists())
                        {
                            Space space = grid[x, y];
                            if (!_spaces[32].ContainsKey(space))
                                _spaces[32].Add(space, CreateBrickSurface(space, 32));
                            Surface s = _spaces[32][space];
                            surface.Blit(s, ((x - 1) * 32) + 2, ((y - 1) * 32) + 2);
                        }
                    }
                }
                surface.DrawLine(_primaryWhite, 0, 0, surface.Width, 0, 1);
                surface.DrawLine(_primaryWhite, 0, 1, surface.Width, 1, 1);
                surface.DrawLine(_primaryWhite, 0, 0, 0, surface.Height, 1);
                surface.DrawLine(_primaryWhite, 1, 0, 1, surface.Height, 1);
                surface.DrawLine(_primaryWhite, 0, surface.Height, surface.Width, surface.Height, 1);
                surface.DrawLine(_primaryWhite, 0, surface.Height - 1, surface.Width, surface.Height - 1, 1);
                surface.DrawLine(_primaryWhite, surface.Width, 0, surface.Width, surface.Height, 1);
                surface.DrawLine(_primaryWhite, surface.Width - 1, 0, surface.Width - 1, surface.Height, 1);
                frame.Blit(surface, _player_XCenter - (surface.Width / 2), _player_YCenter - (surface.Height / 2));
            }
        }

        /// <summary>
        /// Draws next brick readout.
        /// </summary>
        private void DrawHold(Surface frame, Matrix matrix)
        {
            double titleSpacing = 63;
            double brickArea = 96;
            using (Surface surface = new Surface(_hold_Width, _hold_Height, Colors.AlphaBlack192))
            {
                surface.DrawLine(_primaryWhite, 0, 0, _hold_Width, 0, 1);
                surface.DrawLine(_primaryWhite, 0, 1, _hold_Width, 1, 1);
                surface.DrawLine(_primaryWhite, 0, _hold_Height, _hold_Width, _hold_Height, 1);
                surface.DrawLine(_primaryWhite, 0, _hold_Height - 1, _hold_Width, _hold_Height - 1, 1);
                surface.DrawLine(_primaryWhite, 0, 0, 0, _hold_Height, 1);
                surface.DrawLine(_primaryWhite, 1, 0, 1, _hold_Height, 1);
                surface.DrawLine(_primaryWhite, _hold_Width, 0, _hold_Width, _hold_Height, 1);
                surface.DrawLine(_primaryWhite, _hold_Width - 1, 0, _hold_Width - 1, _hold_Height, 1);
                surface.DrawLine(_primaryWhite, 0, 170, _hold_Width, 170, 1);
                surface.DrawLine(_primaryWhite, 0, 171, _hold_Width, 171, 1);
                surface.DrawText_Centered(_primaryWhite, "hold", 28, 20);

                Brick hold = matrix.GetHold();
                if (hold != null)
                {
                    using (Surface container = new Surface(brickArea, brickArea))
                    {
                        Space[,] grid = ReduceBrick(hold.Grid);
                        double width = grid.GetLength(0) * 24;
                        double height = grid.GetLength(1) * 24;
                        using (Surface brick = new Surface(width, height))
                        {
                            for (int x = 0; x < grid.GetLength(0); x++)
                            {
                                for (int y = 0; y < grid.GetLength(1); y++)
                                {
                                    if (grid[x, y].IsSolid())
                                    {
                                        Space space = grid[x, y];
                                        if (!_spaces[24].ContainsKey(space))
                                            _spaces[24].Add(space, CreateBrickSurface(space, 24));
                                        Surface s = _spaces[24][space];
                                        brick.Blit(s, x * 24, y * 24);
                                    }
                                }
                            }
                            container.Blit(brick, (brickArea - width) / 2, (brickArea - height) / 2);
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
            double brickArea = 96;
            double brickSpacing = -3;
            Brick[] nextBricks = matrix.GetNextBricks();
            using (Surface surface = new Surface(_next_Width, _next_Height, Colors.AlphaBlack192))
            {
                surface.DrawLine(_primaryWhite, 0, 0, _next_Width, 0, 1);
                surface.DrawLine(_primaryWhite, 0, 1, _next_Width, 1, 1);
                surface.DrawLine(_primaryWhite, 0, _next_Height, _next_Width, _next_Height, 1);
                surface.DrawLine(_primaryWhite, 0, _next_Height - 1, _next_Width, _next_Height - 1, 1);
                surface.DrawLine(_primaryWhite, 0, 0, 0, _next_Height, 1);
                surface.DrawLine(_primaryWhite, 1, 0, 1, _next_Height, 1);
                surface.DrawLine(_primaryWhite, _next_Width, 0, _next_Width, _next_Height, 1);
                surface.DrawLine(_primaryWhite, _next_Width - 1, 0, _next_Width - 1, _next_Height, 1);
                surface.DrawText_Centered(_primaryWhite, "next", 28, 20);

                for (int i = 0; i < nextBricks.Length; i++)
                {
                    using (Surface container = new Surface(brickArea, brickArea))
                    {
                        Space[,] grid = ReduceBrick(nextBricks[i].Grid);
                        double width = grid.GetLength(0) * 24;
                        double height = grid.GetLength(1) * 24;
                        using (Surface brick = new Surface(width, height))
                        {
                            for (int x = 0; x < grid.GetLength(0); x++)
                            {
                                for (int y = 0; y < grid.GetLength(1); y++)
                                {
                                    if (grid[x, y].IsSolid())
                                    {
                                        Space space = grid[x, y];
                                        if (!_spaces[24].ContainsKey(space))
                                            _spaces[24].Add(space, CreateBrickSurface(space, 24));
                                        Surface s = _spaces[24][space];
                                        brick.Blit(s, x * 24, y * 24);
                                    }
                                }
                            }
                            container.Blit(brick, (brickArea - width) / 2, (brickArea - height) / 2);
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

            Space[,] grid = opponent.GetMatrix();
            int brickSize = 24;
            double matrixWidth = 2d + (brickSize * 10d) + 2d;
            double matrixHeight = 2d + (brickSize * 20d) + 2d;

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
                    {
                        for (int y = 1; y < 22; y++)
                        {
                            if (grid[x, y].Exists())
                            {
                                Space space = grid[x, y];
                                if (!_spaces[brickSize].ContainsKey(space))
                                    _spaces[brickSize].Add(space, CreateBrickSurface(space, brickSize));
                                Surface s = _spaces[brickSize][space];
                                matrixSurface.Blit(s, ((x - 1) * brickSize) + 2, ((y - 1) * brickSize) + 2);
                            }
                        }
                    }
                    matrixSurface.DrawLine(_primaryWhite, 0.5d, 0.5d, matrixWidth - 1.5d, 0.5d, 2d);
                    matrixSurface.DrawLine(_primaryWhite, matrixWidth - 1.5d, 0.5d, matrixWidth - 1.5d, matrixHeight - 1.5d, 2d);
                    matrixSurface.DrawLine(_primaryWhite, matrixWidth - 1.5d, matrixHeight - 1.5d, 0.5d, matrixHeight - 1.5d, 2d);
                    matrixSurface.DrawLine(_primaryWhite, 0.5d, matrixHeight - 1.5d, 0.5d, 0.5d, 2d);
                    surface.Blit(matrixSurface, (width - matrixWidth) / 2d, headerHeight + betweenSpacing);
                }
                frame.Blit(surface, _opponent_XCenter - (width / 2), _frame_Height - ((_frame_Height - _player_Height) / 2) - height + 1);
            }
        }

        /// <summary>
        /// Draws game title.
        /// </summary>
        private void DrawTitle(Surface frame)
        {
            if (GameConfig.Instance.Debug)
                return;

            double titleHeight = 86;
            double charWidth = 42;
            double space = -28;
            double copyrightHeight = 16;
            double width = 280;
            double height = titleHeight + space + copyrightHeight;

            using (Surface surface = new Surface(width, height))
            {
                ////using Surface b = new S
                
                //using (Surface title = new Surface(charWidth * 7, titleHeight))
                //{
                //    using (Surface b = new Surface(charWidth, titleHeight))
                //    {
                //        b.DrawText_Centered(Colors.White, "b", 52, 0);
                //        title.Blit(b, 42 * 0, 0);
                //    }
                //    using (Surface b = new Surface(charWidth, titleHeight))
                //    {
                //        b.DrawText_Centered(Colors.White, "r", 52, 0);
                //        title.Blit(b, 42 * 1, 0);
                //    }
                //    using (Surface b = new Surface(charWidth, titleHeight))
                //    {
                //        b.DrawText_Centered(Colors.White, "i", 52, 0);
                //        title.Blit(b, 42 * 2, 0);
                //    }
                //    using (Surface b = new Surface(charWidth, titleHeight))
                //    {
                //        b.DrawText_Centered(Colors.White, "c", 52, 0);
                //        title.Blit(b, 42 * 3, 0);
                //    }
                //    using (Surface b = new Surface(charWidth, titleHeight))
                //    {
                //        b.DrawText_Centered(Colors.White, "k", 52, 0);
                //        title.Blit(b, 42 * 4, 0);
                //    }
                //    using (Surface b = new Surface(charWidth, titleHeight))
                //    {
                //        b.DrawText_Centered(Colors.White, "e", 52, 0);
                //        title.Blit(b, 42 * 5, 0);
                //    }
                //    using (Surface b = new Surface(charWidth, titleHeight))
                //    {
                //        b.DrawText_Centered(Colors.White, "r", 52, 0);
                //        title.Blit(b, 42 * 6, 0);
                //    }
                //    surface.Blit(title, 26, 0);
                //}

                surface.DrawText_Centered(Colors.FluorescentOrange, "bricker", 52, 0);
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
            double itemSpacing = 22;

            if ((props.Width is Double.NaN) || (props.Height is Double.NaN) || (props.CalculatedHeight is Double.NaN))
            {
                double maxWidth = 0, totalHeight = 0;
                double headerLineHeight = 0, itemLineHeight = 0;
                foreach (string header in props.Header)
                {
                    Surface.MeasureText(header, props.HeaderSize, out double w, out double h);
                    if (w > maxWidth)
                        maxWidth = w;
                    totalHeight += h;
                    headerLineHeight = h;
                }
                foreach (string item in props.Items)
                {
                    Surface.MeasureText(item, props.FontSize, out double w, out double h);
                    if (w > maxWidth)
                        maxWidth = w;
                    totalHeight += h;
                    itemLineHeight = h;
                }
                props.Width = props.Width is Double.NaN ? (2 + horizSpacing + maxWidth + horizSpacing + 2) : props.Width;
                double headerHeight = props.Header.Length > 0 ? vertSpacing : 0;
                props.CalculatedHeight = 2 + totalHeight + vertSpacing + headerHeight + (itemSpacing * (props.Items.Length - 1)) + vertSpacing + 2;
                props.Height = Math.Max(props.Height is Double.NaN ? props.CalculatedHeight : props.Height, props.CalculatedHeight);
                props.HeaderLineHeight = headerLineHeight;
                props.ItemLineHeight = itemLineHeight;
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

                for (int i = 0; i < props.Items.Length; i++)
                {
                    if (i > 0)
                        y += itemSpacing;
                    string item = props.Items[i];
                    SKColor color = i == props.SelectedIndex ? Colors.FluorescentOrange : Colors.White;
                    if (!props.EnabledItems[i])
                        color = Colors.Gray;
                    surface.DrawText_Centered(color, item, props.FontSize, y);
                    y += props.ItemLineHeight;
                }

                frame.Blit(surface, (_frame_Width - width) / 2, (_frame_Height - height) / 2);
            }
        }

        /// <summary>
        /// Draws settings menu.
        /// </summary>
        private void DrawSettings(Surface frame)
        {
            SettingsProperties props = _settingProps;
            if (props == null)
                return;

            double vertSpacing = 26;
            double horizSpacing = 42;
            double itemSpacing = 22;

            if ((props.Width is Double.NaN) || (props.Height is Double.NaN) || (props.CalculatedHeight is Double.NaN))
            {
                double maxWidth = 0, totalHeight = 0;
                double itemLineHeight = 0;
                foreach (SettingsItem item in props.Items)
                {
                    Surface.MeasureText(item.OffCaption, props.FontSize, out double w, out double h);
                    if (w > maxWidth)
                        maxWidth = w;
                    totalHeight += h;
                    itemLineHeight = h;
                }
                props.Width = props.Width is Double.NaN ? (2 + horizSpacing + maxWidth + horizSpacing + 2) : props.Width;
                props.CalculatedHeight = 2 + totalHeight + vertSpacing + (itemSpacing * (props.Items.Length - 1)) + vertSpacing + 2;
                props.Height = Math.Max(props.Height is Double.NaN ? props.CalculatedHeight : props.Height, props.CalculatedHeight);
                props.ItemLineHeight = itemLineHeight;
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

                for (int i = 0; i < props.Items.Length; i++)
                {
                    if (i > 0)
                        y += itemSpacing;
                    string item = props.Items[i].Caption;
                    SKColor color = i == props.SelectedIndex ? Colors.FluorescentOrange : Colors.White;
                    surface.DrawText_Centered(color, item, props.FontSize, y);
                    y += props.ItemLineHeight;
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
                        initials.Blit(char1, ((charWidth - char1.Width) / 2) + (charWidth * 0), (charHeight - char1.Height) / 2);
                    using (Surface char2 = Surface.RenderText(Colors.FluorescentOrange, inits[1].ToString(), 64))
                        initials.Blit(char2, ((charWidth - char2.Width) / 2) + (charWidth * 1), (charHeight - char2.Height) / 2);
                    using (Surface char3 = Surface.RenderText(Colors.FluorescentOrange, inits[2].ToString(), 64))
                        initials.Blit(char3, ((charWidth - char3.Width) / 2) + (charWidth * 2), (charHeight - char3.Height) / 2);
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
            MessageProperties props = _messageProps;
            if (props == null)
                return;

            double horizontalSpacing = 32;
            double verticalSpacing = 26;
            double textWidth = props.Lines
                .Select(l => Surface.MeasureText_Width(l.Text, l.Size))
                .Max();
            double[] textHeights = props.Lines
                .Select(l => Surface.MeasureText_Height(l.Text, l.Size) + l.TopMargin + l.BottomMargin)
                .ToArray();
            double textHeight = textHeights.Sum();
            double betweenSpacing = 24;
            double buttonSize = 28;
            double buttonHeight = Surface.MeasureText_Height("ok", buttonSize);
            double buttonIndent = 48;
            double width = 2 + horizontalSpacing + textWidth + horizontalSpacing + 2;
            if ((width < 380) && (width > 300))
            {
                width = 380;
                horizontalSpacing = (width - textWidth - 4d) / 2d;
            }
            double height = 2 + verticalSpacing + textHeight + (props.Buttons != MessageButtons.None ? betweenSpacing + buttonHeight : 0) + verticalSpacing;
            double y = 2d + verticalSpacing;

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

                using (Surface textSurface = new Surface(textWidth, textHeight))
                {
                    double ty = 0d;
                    for (int i = 0; i < props.Lines.Length; i++)
                    {
                        TextLine line = props.Lines[i];
                        if (line.Alignment == Alignment.Left)
                            textSurface.DrawText_Left(line.Color, line.Text, line.Size, ty);
                        else if (line.Alignment == Alignment.Right)
                            textSurface.DrawText_Right(line.Color, line.Text, line.Size, ty);
                        else if (line.Alignment == Alignment.Center)
                            textSurface.DrawText_Centered(line.Color, line.Text, line.Size, ty);
                        ty += textHeights[i];
                    }
                    surface.Blit(textSurface, horizontalSpacing + 2, y);
                    y += textHeight + betweenSpacing;
                }
                if (props.Buttons != MessageButtons.None)
                {
                    using (Surface buttonSurface = new Surface(width, buttonHeight))
                    {
                        if (props.Buttons == MessageButtons.OK)
                        {
                            buttonSurface.DrawText_Centered(Colors.FluorescentOrange, "ok", buttonSize, 0);
                        }
                        else if (props.Buttons >= MessageButtons.CancelOK)
                        {
                            string button1 = props.Buttons == MessageButtons.CancelOK ? "cancel" : "no";
                            string button2 = props.Buttons == MessageButtons.CancelOK ? "ok" : "yes";
                            SKColor color1 = props.ButtonIndex == 0 ? Colors.FluorescentOrange : Colors.White;
                            SKColor color2 = props.ButtonIndex != 0 ? Colors.FluorescentOrange : Colors.White;
                            buttonSurface.DrawText_Left(color1, button1, buttonSize, 0, buttonIndent);
                            buttonSurface.DrawText_Right(color2, button2, buttonSize, 0, buttonIndent);
                        }
                        surface.Blit(buttonSurface, 0, y);
                    }
                }
                frame.Blit(surface, (frame.Width - surface.Width) / 2, (frame.Height - surface.Height) / 2);
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
            if (!GameConfig.Instance.Debug)
                return;

            List<string> lines = new List<string>();
            lines.Add($"fps:   {(int)_fps.CPS}");
            lines.Add($"game_state:   {gameState}");
            if (communications != null)
            {
                lines.Add($"heartbeats:   s={communications.HeartbeatsSent}");
                lines.Add($"cmd_requests:   s={communications.CommandRequestsSent}, r={communications.CommandRequestsReceived}");
                lines.Add($"cmd_responses:   s={communications.CommandResponsesSent}, r={communications.CommandResponsesReceived}");
                lines.Add($"game_status:   s={communications.DataSent}, r={communications.DataReceived}");
                lines.Add($"com_state:   {communications.ConnectionState}");
            }
            lines.Add($"errors:   {ErrorHandler.ErrorCount}");

            using (Surface surface = Surface.RenderText(Colors.White, lines, 12))
                frame.Blit(surface, 35, 25);
        }

        /// <summary>
        /// Draws the background.
        /// </summary>
        private void DrawBackground(Surface frame, GameStats stats)
        {
            if (!GameConfig.Instance.Background)
                return;

            DateTime now = DateTime.Now;
            foreach (ITile tile in _tiles)
            {
                tile.Move(now, stats.Level);
                using (Surface surface = new Surface(tile.Width, tile.Height, tile.Color))
                {
                    if ((tile is ImageTile t) && (t.Image != null))
                        surface.Blit(t.Image, 0, 0);
                    frame.Blit(surface, tile.X - (tile.Width / 2d), tile.Y - (tile.Height / 2d));
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
                double leftX = space.X;
                double topY = space.Y;
                SKColor color = space.Color;
                SKColor lighter = Colors.GetLighter(color);
                SKColor darker = Colors.GetDarker(color);

                frame.DrawRect(space.Color, leftX, topY, 32, 32);
                frame.DrawLine(lighter, leftX + 0, topY + 0, leftX + 32 - 0, topY + 0, 1);
                frame.DrawLine(lighter, leftX + 1, topY + 1, leftX + 32 - 1, topY + 1, 1);
                frame.DrawLine(lighter, leftX + 2, topY + 2, leftX + 32 - 2, topY + 2, 1);
                frame.DrawLine(lighter, leftX + 32 - 0, topY + 0, leftX + 32 - 0, topY + 32 - 0, 1);
                frame.DrawLine(lighter, leftX + 32 - 1, topY + 1, leftX + 32 - 1, topY + 32 - 1, 1);
                frame.DrawLine(lighter, leftX + 32 - 2, topY + 2, leftX + 32 - 2, topY + 32 - 2, 1);
                frame.DrawLine(darker, leftX + 0, topY + 32 - 0, leftX + 32 - 0, topY + 32 - 0, 1);
                frame.DrawLine(darker, leftX + 1, topY + 32 - 1, leftX + 32 - 1, topY + 32 - 1, 1);
                frame.DrawLine(darker, leftX + 2, topY + 32 - 2, leftX + 32 - 2, topY + 32 - 2, 1);
                frame.DrawLine(darker, leftX + 0, topY + 0, leftX + 0, topY + 32 - 0, 1);
                frame.DrawLine(darker, leftX + 1, topY + 1, leftX + 1, topY + 32 - 1, 1);
                frame.DrawLine(darker, leftX + 2, topY + 2, leftX + 2, topY + 32 - 2, 1);
            }
        }

        /// <summary>
        /// Removes empty rows and columns from specified brick, returning a smaller grid.
        /// </summary>
        private static Space[,] ReduceBrick(Space[,] brick)
        {
            int left = Int32.MaxValue, right = Int32.MinValue, top = Int32.MaxValue, bottom = Int32.MinValue;
            for (int x = 0; x < brick.GetLength(0); x++)
            {
                for (int y = 0; y < brick.GetLength(1); y++)
                {
                    if (brick[x, y].IsSolid())
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
            Space[,] reduced = new Space[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    reduced[x, y] = brick[x + xOffset, y + yOffset];
            return reduced;
        }
    }

    /// <summary>
    /// Represents alignment of text or graphics.
    /// </summary>
    public enum Alignment
    {
        Left,
        Right,
        Center
    }

}

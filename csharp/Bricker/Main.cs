using Bricker.Configuration;
using Bricker.Error;
using Bricker.Game;
using Bricker.Rendering;
using Common.Networking.Game;
using Common.Networking.Game.Discovery;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Bricker
{
    /// <summary>
    /// Contains game entry point, basic logic, and primary game objects.
    /// </summary>
    public class Main
    {
        //private
        private readonly Dispatcher _dispatcher;
        private readonly Queue<Key> _keyQueue;
        private Thread _programLoop;
        private readonly GameCommunications _communications;
        private readonly Renderer _renderer;
        private readonly Matrix _matrix;
        private GameStats _stats;
        private Opponent _opponent;
        private List<ExplodingSpace> _spaces;
        private readonly double[] _levelDropIntervals;
        private Player _pendingOpponent;

        #region Constructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Main(Window window)
        {
            //vars
            _dispatcher = window.Dispatcher;
            _keyQueue = new Queue<Key>();
            _communications = new GameCommunications(Config.GameTitle, 
                Config.GameVersion, Config.LocalIP, Config.LocalPort, 
                Config.Initials, ErrorHandler.Instance);
            _renderer = new Renderer(window);
            _matrix = new Matrix();
            _stats = new GameStats();
            _spaces = null;
            _levelDropIntervals = new double[10];
            _pendingOpponent = null;

            //events
            _communications.OpponentInviteReceived += (o) =>
            {
                if (_pendingOpponent == null)
                    _pendingOpponent = o;
            };

            //run tests (usually does nothing)
            RunTests();

            //calculate level drop intervals
            double interval = 2000;
            for (int i = 0; i < 10; i++)
            {
                interval *= 0.8;
                _levelDropIntervals[i] = interval;
            }
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Logs a user keypress for processing.
        /// </summary>
        public void LogKeyPress(Key key)
        {
            lock (_keyQueue)
            {
                _keyQueue.Enqueue(key);
            }
        }

        /// <summary>
        /// Fired when it's time to draw a new frame.
        /// </summary>
        public void DrawFrame(SKPaintSurfaceEventArgs e)
        {
            _renderer.DrawFrame(e, _matrix, _stats, _spaces);
        }

        /// <summary>
        /// Starts the program loop thread, called on window load.
        /// </summary>
        public void StartProgramLoop()
        {
            _programLoop = new Thread(ProgramLoop)
            {
                IsBackground = true
            };
            _programLoop.Start();
        }

        #endregion

        #region Program Loop

        /// <summary>
        /// Runs main game logic.
        /// </summary>
        private void ProgramLoop()
        {
            //vars
            bool inGame = false;

            //start game communications
            _communications.Start();


            //program loop
            while (true)
            {
                //opponent invite
                if (_pendingOpponent != null)
                    OpponentRespondLoop();

                //main menu loop
                MenuSelection selection = (MenuSelection)MenuLoop(new MenuProperties(
                    options: new string[] { "resume", "new game", "two player", "quit" },
                    enabledOptions: new bool[] { inGame == true, true, true, true },
                    allowEsc: false,
                    allowPlayerInvite: true,
                    width: 400));

                //resume, run game loop
                if (selection == MenuSelection.Resume)
                {
                    inGame = GameLoop();
                }

                //start new game, run game loop
                else if (selection == MenuSelection.New)
                {
                    NewSinglePlayerGame();
                    inGame = GameLoop();
                }

                //two player mode
                else if (selection == MenuSelection.TwoPlayer)
                {
                    //get player initials
                    if (String.IsNullOrWhiteSpace(Config.Initials))
                    {
                        string initials = InitialsLoop(new string[] { "enter your initials", "" });
                        if (!String.IsNullOrWhiteSpace(Config.Initials))
                        {
                            Config.SaveInitials(initials);
                            _communications.ChangePlayerName(initials);
                        }
                    }
                    
                    //select discovered player from lobby
                    Player player = PlayerLobbyLoop();
                    if (player == null)
                        continue;

                    //request match, get response
                    CommandResult result = OpponentInviteLoop(player, out Opponent opponent);


                    //todo: do stuff

                    inGame = GameLoop();
                    _opponent = null;
                }

                //quit program
                else if (selection == MenuSelection.Quit)
                {
                    ExplodeSpaces();
                    break;
                }
            }

            //end program
            _dispatcher.Invoke(() => Application.Current.Shutdown());
        }

        #endregion

        #region Game Loop

        /// <summary>
        /// The main game loop.  Returns true if still in game (menu opened).
        /// </summary>
        private bool GameLoop()
        {
            //vars
            bool gameOver = false;
            bool hit;

            //event loop
            while (!gameOver)
            {
                //reset hit flag
                hit = false;

                //sleep
                Thread.Sleep(15);

                //get next key press
                Key key = Key.None;
                lock (_keyQueue)
                {
                    if (_keyQueue.Count > 0)
                        key = _keyQueue.Dequeue();
                }

                //have key?
                if (key != Key.None)
                {
                    //left
                    if (key == Key.Left)
                        MoveBrickLeft();

                    //right
                    else if (key == Key.Right)
                        MoveBrickRight();

                    //down
                    else if (key == Key.Down)
                        MoveBrickDown();

                    //rotate
                    else if (key == Key.Up)
                        RotateBrick();

                    //drop
                    else if (key == Key.Space)
                    {
                        DropBrickToBottom();
                        hit = true;
                    }

                    //menu
                    else if ((key == Key.Escape) || (key == Key.Q))
                        return true;

                    //level up
                    else if ((key == Key.PageUp) && (Config.Debug))
                        _stats.SetLevel(_stats.Level + 1);

                    //level down
                    else if ((key == Key.PageDown) && (Config.Debug))
                        _stats.SetLevel(_stats.Level - 1);

                    //debug toggle
                    else if (key == Key.D)
                        Config.Debug = !Config.Debug;
                }

                //drop brick timer?
                if (IsDropTime())
                    hit = MoveBrickDown();

                //brick hit bottom?
                if (hit)
                    gameOver = BrickHit();
            }

            //game over
            ExplodeSpaces();
            if (_stats.IsHighScore())
            {
                string initials = InitialsLoop(new string[] { "new high score", "enter your initials" });
                if (!String.IsNullOrWhiteSpace(initials))
                    _stats.AddHighScore(initials);

            }
            return false;
        }

        /// <summary>
        /// Resets state and starts a new game.
        /// </summary>
        private void NewSinglePlayerGame()
        {
            _stats = new GameStats();
            _matrix.NewGame();
        }

        #endregion

        #region Input Loops

        /// <summary>
        /// Enters a generic menu loop defined by the specified properties object.
        /// Returns 0-based index of selected option, or -1 for Esc (if allowed), or -2 for opponent invite.
        /// </summary>
        private int MenuLoop(MenuProperties props)
        {
            try
            {
                //push properties to renderer
                _renderer.MenuProps = props;

                //event loop
                while (true)
                { 
                    //return if opponent invite
                    if (_pendingOpponent != null)
                        return -2;

                    //get next key press
                    Key key = Key.None;
                    lock (_keyQueue)
                    {
                        if (_keyQueue.Count > 0)
                            key = _keyQueue.Dequeue();
                    }

                    //no key?
                    if (key == Key.None)
                    {
                        Thread.Sleep(15);
                        continue;
                    }

                    //up
                    else if ((key == Key.Left) || (key == Key.Up))
                    {
                        props.DecrementSelection();
                    }

                    //down
                    else if ((key == Key.Right) || (key == Key.Down))
                    {
                        props.IncrementSelection();
                    }

                    //enter
                    else if (key == Key.Enter)
                    {
                        return props.SelectionIndex;
                    }

                    //esc
                    else if ((props.AllowEsc) && (key == Key.Escape))
                    {
                        return -1;
                    }
                }
            }
            finally
            {
                //clear properties from renderer
                _renderer.MenuProps = null;
            }            
        }

        /// <summary>
        /// The initials-entry loop.
        /// </summary>
        private string InitialsLoop(string[] header)
        {
            try
            {
                //vars
                char[] chars = new char[] { ' ', ' ', ' ' };
                int position = 0;
                InitialsEntryProperties props = new InitialsEntryProperties(Config.Initials, header);

                //push properties to renderer
                _renderer.InitialProps = props;

                //event loop
                while (true)
                {
                    //get next key press, or continue
                    Key key = Key.None;
                    lock (_keyQueue)
                    {
                        if (_keyQueue.Count > 0)
                            key = _keyQueue.Dequeue();
                    }

                    //no key?
                    if (key == Key.None)
                    {
                        Thread.Sleep(15);
                        continue;
                    }

                    //enter
                    else if (key == Key.Enter)
                    {
                        Config.SaveInitials(props.Initials);
                        _communications.ChangePlayerName(props.Initials);
                        return props.Initials;
                    }

                    //esc
                    else if (key == Key.Escape)
                    {
                        return null;
                    }

                    //backspace
                    else if (key == Key.Back)
                    {
                        position--;
                        if (position < 0)
                            position = 0;
                        chars[position] = ' ';
                    }

                    //other key
                    else
                    {
                        //allow only number or letter
                        string keyStr = key.ToString();
                        char c;
                        if ((keyStr.Length == 1) && Char.IsLetterOrDigit(keyStr[0]))
                            c = keyStr[0];
                        else if ((keyStr.Length == 2) && (keyStr[0] == 'D') && Char.IsDigit(keyStr[1]))
                            c = keyStr[1];
                        else
                            continue;

                        //add char
                        if ((position > 2) || (position < 0))
                            continue;
                        chars[position] = c;
                        position++;
                        if (position > 3)
                            position = 3;
                    }

                    //join chars
                    props.SetInitials(String.Join("", chars));
                }
            }
            finally
            {
                //clear properties from renderer
                _renderer.InitialProps = null;
            }
        }

        /// <summary>
        /// The message box loop.
        /// </summary>
        private bool MessageBoxLoop(string message, MessageButtons buttons)
        {
            try
            {
                //vars
                double size = 24;
                double width = 500;
                MessageProperties props = new MessageProperties(
                    WrapText(message, size, width), 
                    size, 
                    buttons, 
                    buttons >= MessageButtons.CancelOK ? 1 : 0);

                //push properties to renderer
                _renderer.MessageProps = props;

                //event loop
                while (true)
                {
                    //get next key press, or continue
                    Key key = Key.None;
                    lock (_keyQueue)
                    {
                        if (_keyQueue.Count > 0)
                            key = _keyQueue.Dequeue();
                    }

                    //no key?
                    if (key == Key.None)
                    {
                        Thread.Sleep(15);
                        continue;
                    }

                    //enter
                    else if (key == Key.Enter)
                    {
                        return buttons <= MessageButtons.OK ? true : props.ButtonIndex > 0;
                    }

                    //esc
                    else if (key == Key.Escape)
                    {
                        return false;
                    }

                    //left
                    else if ((key == Key.Left) || (key == Key.Up))
                    {
                        props.DecrementsIndex();
                    }

                    //right
                    else if ((key == Key.Right) || (key == Key.Down))
                    {
                        props.IncrementIndex();
                    }
                }
            }
            finally
            {
                //clear properties from renderer
                _renderer.MessageProps = null;
            }
        }

        #endregion

        #region Two-Player Invite

        /// <summary>
        /// Discovered player selection loop.
        /// </summary>
        private Player PlayerLobbyLoop()
        {
            try
            {
                //vars
                LobbyProperties props = new LobbyProperties();

                //push properties to renderer
                _renderer.LobbyProps = props;

                //event loop
                while (true)
                {
                    //get discovered players
                    IReadOnlyList<Player> players = _communications.GetDiscoveredPlayers(top: 5);
                    props.UpdatePlayers(players);

                    //get next key press, or continue
                    Key key = Key.None;
                    lock (_keyQueue)
                    {
                        if (_keyQueue.Count > 0)
                            key = _keyQueue.Dequeue();
                    }

                    //no key?
                    if (key == Key.None)
                    {
                        Thread.Sleep(15);
                        continue;
                    }

                    //enter
                    else if (key == Key.Enter)
                    {
                        if ((props.ButtonIndex == 1) && (props.PlayerIndex >= 0) && (props.PlayerIndex <= (players.Count - 1)))
                            return players[props.PlayerIndex];
                        return null;
                    }

                    //esc
                    else if (key == Key.Escape)
                    {
                        return null;
                    }

                    //left
                    else if (key == Key.Left)
                    {
                        props.DecrementButtonIndex();
                    }

                    //right
                    else if (key == Key.Right)
                    {
                        props.IncrementButtonIndex();
                    }

                    //up
                    else if (key == Key.Up)
                    {
                        props.DecrementPlayerIndex();
                    }

                    //down
                    else if (key == Key.Down)
                    {
                        props.IncrementPlayerIndex();
                    }
                }
            }
            finally
            {
                //clear properties from renderer
                _renderer.LobbyProps = null;
            }
        }

        /// <summary>
        /// Invite opponent loop.  Connects, asks question, waits for response.
        /// </summary>
        private CommandResult OpponentInviteLoop(Player player, out Opponent opponent)
        {
            opponent = null;

            bool success = _communications.SetOpponentAndConnect(player);
            if (!success)
                return CommandResult.Error;

            CommandResult result = _communications.InviteOpponent();
            if (result == CommandResult.Accept)
                opponent = new Opponent(Config.CleanInitials(player.Name), player.IP.ToString());

            return result;
        }

        #endregion

        #region Two-Player Respond

        #endregion



        private void OpponentRespondLoop()
        {
            Player opponent = _pendingOpponent;
            if (opponent == null)
                return;

            bool accept = MessageBoxLoop($"Opponent '{opponent.Name}' is challenging you to a two-player match!  Do you accept?", MessageButtons.NoYes);

            Thread.Sleep(3500);

            _communications.AcceptInvite(opponent);


        }

        /// <summary>
        /// Splits and wraps the text for display in a message box.
        /// </summary>
        private string[] WrapText(string message, double size, double width)
        {
            string[] lines = message.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            List<string> text = new List<string>();
            foreach (string line in lines)
            {
                string[] split = line.Split(' ');
                List<string> words = new List<string>();
                foreach (string word in split)
                {
                    string test = String.Join(" ", words);
                    test += (test.Length > 0 ? " " : "") + word;
                    Surface.MeasureText(test, size, out double w, out double _);
                    if (w > width)
                    {
                        text.Add(String.Join(" ", words));
                        words.Clear();
                    }
                    words.Add(word);
                }
                text.Add(String.Join(" ", words));
            }
            return text.ToArray();
        }

     

        #region Brick Logic

        /// <summary>
        /// Moves brick left.
        /// </summary>
        private void MoveBrickLeft()
        {
            _matrix.MoveBrickLeft();
        }

        /// <summary>
        /// Moves brick right.
        /// </summary>
        private void MoveBrickRight()
        {
            _matrix.MoveBrickRight();
        }

        /// <summary>
        /// Moves brick down.  Returns true if brick hits bottom.
        /// </summary>
        private bool MoveBrickDown()
        {
            bool hit = _matrix.MoveBrickDown();
            if (hit)
                _stats.IncrementScore(1);
            return hit;
        }

        /// <summary>
        /// Rotates brick.
        /// </summary>
        private void RotateBrick()
        {
            _matrix.RotateBrick();
        }

        /// <summary>
        /// Animates a brick dropping to bottom of screen.
        /// </summary>
        private void DropBrickToBottom()
        {
            bool hit = false;
            DateTime start = DateTime.Now;
            double dropsPerSecond = 100;
            int dropCount = 0;
            while (!hit)
            {
                Thread.Sleep(5);
                TimeSpan elapsed = DateTime.Now - start;
                int expectedDrops = (int)Math.Round(dropsPerSecond * elapsed.TotalSeconds);
                while (dropCount < expectedDrops)
                {
                    hit = MoveBrickDown();
                    dropCount++;
                    if (hit)
                        break;
                }
            }
            _stats.IncrementScore(2);
        }

        /// <summary>
        /// Returns true if it's time for brick to drop.
        /// </summary>
        private bool IsDropTime()
        {
            if (_matrix.Brick != null)
            {
                double dropIntervalMs = _levelDropIntervals[_stats.Level - 1];
                return _matrix.Brick.IsDropTime(dropIntervalMs);
            }
            return false;
        }

        /// <summary>
        /// Executed when brick hits bottom and comes to rest.  
        /// Spawns new brick.  Returns true on new brick collision (game over).
        /// </summary>
        private bool BrickHit()
        {
            _matrix.AddBrickToMatrix();
            List<int> rowsToErase = _matrix.IdentifySolidRows();
            int rows = rowsToErase.Count;
            if (rows > 0)
            {
                _stats.IncrementLines(rows);
                int points = 40;
                if (rows == 2)
                    points = 100;
                else if (rows == 3)
                    points = 300;
                else if (rows == 4)
                    points = 1200;
                _stats.IncrementScore(points);
                EraseFilledRows(rowsToErase);
                DropGrid();
            }
            bool collision = _matrix.SpawnBrick();
            return collision;
        }

        /// <summary>
        /// Animates erasure of filled rows.
        /// </summary>
        private void EraseFilledRows(List<int> rowsToErase)
        {
            DateTime start = DateTime.Now;
            double xPerSecond = 50;
            int x = 0;
            while (x < 10)
            {
                Thread.Sleep(5);
                TimeSpan elapsed = DateTime.Now - start;
                int expectedX = (int)Math.Round(xPerSecond * elapsed.TotalSeconds);
                while (x < expectedX)
                {
                    x++;
                    if ((x < 1) || (x > 10))
                        break;
                    foreach (int y in rowsToErase)
                        _matrix.Grid[x, y] = 0;
                }
            }
        }

        /// <summary>
        /// Drops hanging pieces to resting place.
        /// </summary>
        private void DropGrid()
        {
            Thread.Sleep(15);
            while (DropGridOnce())
                continue;
        }

        /// <summary>
        /// Drops hanging pieces, bottom-most row.
        /// </summary>
        private bool DropGridOnce()
        {
            int topFilledRow = 0;
            for (int row = 1; row <= 20; row++)
            {
                bool empty = true;
                for (int x = 1; x <= 10; x++)
                {
                    if (_matrix.Grid[x, row] > 0)
                    {
                        empty = false;
                        break;
                    }
                }
                if (!empty)
                {
                    topFilledRow = row;
                    break;
                }
            }
            if (topFilledRow == 0)
                return false;
            int bottomEmptyRow = 0;
            for (int row = 20; row > (topFilledRow - 1); row--)
            {
                bool empty = true;
                for (int x = 1; x <= 10; x++)
                {
                    if (_matrix.Grid[x, row] > 0)
                    {
                        empty = false;
                        break;
                    }
                }
                if (empty)
                {
                    bottomEmptyRow = row;
                    break;
                }
            }
            if (bottomEmptyRow == 0)
                return false;
            for (int y = bottomEmptyRow; y > 1; y--)
                for (int x = 1; x <= 10; x++)
                    _matrix.Grid[x, y] = _matrix.Grid[x, y - 1];
            for (int x = 1; x <= 10; x++)
                _matrix.Grid[x, 1] = 0;
            return true;
        }

        #endregion

        #region Exploding Spaces

        /// <summary>
        /// Explodes matrix spaces outwards on game over.
        /// </summary>
        private void ExplodeSpaces()
        {
            try
            {
                List<ExplodingSpace> spaces = new List<ExplodingSpace>();
                _matrix.AddBrickToMatrix();
                for (int x = 1; x <= 10; x++)
                {
                    for (int y = 1; y <= 20; y++)
                    {
                        if (_matrix.Grid[x, y] > 0)
                        {
                            double spaceX = (((x - 1) * 33) + 2) + ((_renderer.FrameWidth - 333) / 2) - 1;
                            double spaceY = (((y - 1) * 33) + 2) + ((_renderer.FrameHeight - 663) / 2) - 1;
                            spaces.Add(new ExplodingSpace(spaceX, spaceY, Brick.BrickToColor(_matrix.Grid[x, y])));
                            _matrix.Grid[x, y] = 0;
                        }
                    }
                }
                _spaces = spaces;

                DateTime start = DateTime.Now;
                bool haveSpaces = true;
                while (haveSpaces)
                {
                    TimeSpan elapsed = DateTime.Now - start;
                    haveSpaces = false;
                    foreach (ExplodingSpace space in _spaces)
                    {
                        space.X += space.XVelocity * elapsed.TotalSeconds;
                        space.Y += space.YVelocity * elapsed.TotalSeconds;
                        if ((space.X > 0) && (space.X < 1000) && (space.Y > 0) && (space.Y < 700))
                            haveSpaces = true;
                    }
                    Thread.Sleep(5);
                }
            }
            finally
            {
                _spaces = null;
            }
        }

        #endregion

        #region Testing & Misc

        private void RunTests()
        {
            //byte[,] matrix = new byte[10, 20];
            //Packet p1 = new Packet("10.0.1.220", "10.0.1.222", "JLH", matrix, 1, 10, 100, false, true, false, 10, 5);
            //byte[] bytes = p1.ToBytes();
            //Packet p2 = Packet.FromBytes(bytes);

            //MessageBoxLoop("Here is some message, like whatever", MessageButtons.OK);
            //int i = MenuLoop(new MenuProperties(
            //    header: new string[] { "header line one goes here" },
            //    options: new string[] { "menu option 1", "menu option 2", "menu option 3", "menu option 4", "menu option 5" }));
            //int j = MenuLoop(new MenuProperties(
            //    options: new string[] { "option one", "option two", "option three" }));
            //int k = MenuLoop(new MenuProperties(
            //    options: new string[] { "resume", "new game", "two player", "quit" },
            //    width: 400));
        }

        #endregion

    }
}

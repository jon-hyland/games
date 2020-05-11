using Bricker.Configuration;
using Bricker.Logging;
using Bricker.Rendering;
using Bricker.Rendering.Properties;
using Common.Standard.Error;
using Common.Standard.Game;
using Common.Standard.Logging;
using Common.Standard.Networking;
using Common.Standard.Networking.Packets;
using Common.Windows.Networking.Game;
using Common.Windows.Rendering;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Bricker.Game
{
    /// <summary>
    /// Contains game entry point, basic logic, and primary game objects.
    /// </summary>
    public class Main
    {
        //private
        private readonly MainWindow _window;
        private readonly Dispatcher _dispatcher;
        private readonly Queue<Key> _keyQueue;
        private Thread _programLoop;
        private readonly Config _config;
        private readonly Logger _logger;
        private readonly GameCommunications _communications;
        private readonly Renderer _renderer;
        private readonly Matrix _matrix;
        private readonly Random _random;
        private GameStats _stats;
        private List<ExplodingSpace> _spaces;
        private readonly double[] _levelDropIntervals;
        private Player _pendingOpponent;
        private Opponent _opponent;
        private GameState _gameState;

        //public
        public Config Config => _config;

        #region Constructor

        /// <summary>
        /// Class constructor.
        /// </summary>
        public Main(MainWindow window)
        {
            //vars
            _window = window;
            _dispatcher = window.Dispatcher;
            _keyQueue = new Queue<Key>();
            _config = new Config();
            _logger = new Logger(_config.LogFile);
            _communications = new GameCommunications(_config, _config.Initials);
            _renderer = new Renderer(window, _config);
            _matrix = new Matrix();
            _random = new Random();
            _stats = new GameStats(_config);
            _spaces = null;
            _levelDropIntervals = new double[10];
            _pendingOpponent = null;
            _opponent = null;
            _gameState = GameState.NotPlaying;

            //initialize
            RenderProps.Initialize(_config);
            Log.Initiallize(_logger);
            ErrorHandler.Initialize(_logger);
            _window.Title = $"Bricker v{_config.DisplayVersion}";

            //events
            _communications.OpponentInviteReceived += (o) =>
            {
                if (_pendingOpponent == null)
                    _pendingOpponent = o;
            };
            _communications.DataPacketReceived += (p) => ReceivedGameStatus(p);
            _communications.CommandRequestPacketReceived += (p) => ReceivedCommandPacket(p);

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

        /// <summary>
        /// Fired when main window is closing.
        /// </summary>
        public void WindowClosing()
        {
            _logger?.Dispose();
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
            _renderer.DrawFrame(e, _matrix, _stats, _spaces, _communications, _opponent, _gameState);
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
            //set flag
            _gameState = GameState.NotPlaying;

            //start game communications
            _communications.Start();

            ////todo: remove!!
            //MessageBoxLoop(new MessageProperties(new string[] { "Message with one line", "and another line", "plus another third line" }, MessageButtons.OK));
            //MessageBoxLoop(new MessageProperties("This is a test.", MessageButtons.OK));
            //MessageBoxLoop(new MessageProperties("This is a test with much more words and different buttons.", MessageButtons.CancelOK));
            //MessageBoxLoop(new MessageProperties("Message with no buttons", MessageButtons.None));
            //MessageBoxLoop(new MessageProperties(new string[] { "Message with one line", "and another line" }, MessageButtons.OK));
            //MessageBoxLoop(new MessageProperties(new string[] { "Message with one line", "and another line", "plus another third line" }, MessageButtons.OK));
            //MessageBoxLoop(new MessageProperties(new TextLine[] { new TextLine("Message with text line one"), new TextLine("and also text line two") }, MessageButtons.NoYes));
            //MessageBoxLoop(new MessageProperties(new TextLine[] {
            //    new TextLine(
            //        text:"Message with text line one plus bottom margin",
            //        color: Colors.Coquelicot,
            //        size: 32,
            //        bottomMargin: 20),
            //    new TextLine(
            //        text:"Message with text line one plus top margin",
            //        color: Colors.ForestGreen,
            //        size: 18,
            //        topMargin: 10,
            //        alignment: Alignment.Right) }));

            //get player initials
            if (String.IsNullOrWhiteSpace(_config.Initials))
            {
                string initials = InitialsLoop(new string[] { "who are you?", "enter your initials" });
                if (!String.IsNullOrWhiteSpace(_config.Initials))
                {
                    _config.SaveInitials(initials);
                    _communications.ChangePlayerName(initials);
                }
            }

            //program loop
            while (true)
            {
                //opponent invite
                if (_pendingOpponent != null)
                    OpponentRespondLoop();

                //vars
                MenuSelection selection = MenuSelection.None;

                //auto-restart game?
                if ((_gameState == GameState.GameOver) || (_gameState == GameState.GameOver_OpponentLoss))
                    selection = MenuSelection.New;

                //main menu loop
                if (selection == MenuSelection.None)
                    selection = (MenuSelection)MenuLoop(new MenuProperties(
                        options: new string[] { "resume", "new game", "two player", "quit" },
                        enabledOptions: new bool[] { _gameState == GameState.GamePaused, true, true, true },
                        allowEsc: false,
                        allowPlayerInvite: true,
                        width: 400,
                        height: 380));

                //resume, run game loop
                if (selection == MenuSelection.Resume)
                {
                    GameLoop(newGame: false);
                }

                //start new game, run game loop
                else if (selection == MenuSelection.New)
                {
                    GameLoop(newGame: true);
                }

                //two player mode
                else if (selection == MenuSelection.TwoPlayer)
                {
                    //select discovered player from lobby
                    Player player = PlayerLobbyLoop();
                    if (player == null)
                        continue;

                    //request match, get response
                    CommandResult result = OpponentInviteLoop(player, out Opponent opponent);

                    //new game?
                    if ((result == CommandResult.Accept) && (opponent != null))
                    {
                        _opponent = opponent;
                        GameLoop(newGame: true);
                    }
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
        private void GameLoop(bool newGame)
        {
            //vars
            Opponent opponent;

            //new game?
            if (newGame)
            {
                _stats = new GameStats(_config);
                _matrix.NewGame();
                _opponent?.Reset();
            }

            //set flag
            _gameState = GameState.GameLive;

            //event loop
            while (true)
            {
                //get opponent reference (thread safety)
                opponent = _opponent;

                //return if opponent invite
                if (_pendingOpponent != null)
                {
                    _gameState = GameState.GamePaused;
                    return;
                }

                //vars
                bool hit = false;
                bool collision = false;

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

                    //hold
                    else if (key == Key.C)
                        collision = HoldBrick();

                    //menu
                    else if ((key == Key.Escape) || (key == Key.Q))
                    {
                        _gameState = GameState.GamePaused;
                        return;
                    }

                    //level up
                    else if ((key == Key.PageUp) && (RenderProps.Debug))
                        _stats.SetLevel(_stats.Level + 1);

                    //level down
                    else if ((key == Key.PageDown) && (RenderProps.Debug))
                        _stats.SetLevel(_stats.Level - 1);

                    //debug toggle
                    else if (key == Key.D)
                        RenderProps.Debug = !RenderProps.Debug;
                }

                //drop brick timer?
                if (IsDropTime())
                    hit = MoveBrickDown();

                //brick hit bottom?
                if (hit)
                {
                    bool gameOver = BrickHit();
                    if (gameOver)
                    {
                        _stats.SetGameOver();
                        _gameState = GameState.GameOver;
                        break;
                    }
                }

                //hold-swap collision?
                if (collision)
                {
                    _stats.SetGameOver();
                    _gameState = GameState.GameOver;
                    break;
                }

                //two-player mode?
                if (opponent != null)
                {
                    //disconnected?
                    if (_communications.ConnectionState == ConnectionState.NotConnected)
                    {
                        _opponent = null;
                        _pendingOpponent = null;
                        //MessageBoxLoop("Player has disconnected.", MessageButtons.OK);
                        MessageBoxLoop(new MessageProperties("Player has disconnected."));
                        continue;
                    }

                    //send game status
                    SendGameStatus();

                    //have new sent lines?
                    if (opponent.LinesSent > opponent.LastLinesSent)
                    {
                        int newLines = opponent.LinesSent - opponent.LastLinesSent;
                        opponent.SetLastLinesSent(opponent.LinesSent);
                        bool gameOver = AddSentLines(newLines);
                        if (gameOver)
                        {
                            _stats.SetGameOver();
                            _gameState = GameState.GameOver;
                            break;
                        }
                    }

                    //opponent game over logic
                    if (opponent.GameOver)
                    {
                        _gameState = GameState.GameOver_OpponentLoss;
                        break;
                    }
                }
            }

            //game over
            ExplodeSpaces();

            //two-player mode?
            if (opponent != null)
            {
                //send game status (one last time)
                SendGameStatus();

                ////TODO: PASS GAME OVER VIA DATA PARAMETER
                ////send game-over command, if local player finished
                //if (_gameState == GameState.GameOver)
                //    _communications.SendCommandRequest(
                //        type: CommandType.Passthrough,
                //        data: null,
                //        timeout: TimeSpan.FromSeconds(2));

                //message
                MessageBoxLoop(new MessageProperties(
                    new TextLine[]
                    {
                        new TextLine(text: "Game Over!", size: 32, bottomMargin: 36),
                        new TextLine(text: $"Your score:   {_stats.Score.ToString("N0")}", size: 24),
                        new TextLine(text: $"Opponent score:   {opponent.Score.ToString("N0")}", size: 24)
                    }));
            }

            //high score?
            if (_stats.IsHighScore())
            {
                string initials = InitialsLoop(new string[] { "new high score", "enter your initials" });
                if (!String.IsNullOrWhiteSpace(initials))
                    _stats.AddHighScore(initials);

            }
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

                    //debug toggle
                    else if (key == Key.D)
                        RenderProps.Debug = !RenderProps.Debug;
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
                InitialsEntryProperties props = new InitialsEntryProperties(_config.Initials, header);

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
                        _config.SaveInitials(props.Initials);
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
        private bool MessageBoxLoop(MessageProperties props)
        {
            try
            {
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
                        return props.Buttons <= MessageButtons.OK ? true : props.ButtonIndex > 0;

                    //esc
                    else if (key == Key.Escape)
                        return false;

                    //left
                    else if ((key == Key.Left) || (key == Key.Up))
                        props.DecrementsIndex();

                    //right
                    else if ((key == Key.Right) || (key == Key.Down))
                        props.IncrementIndex();

                    //debug toggle
                    else if (key == Key.D)
                        RenderProps.Debug = !RenderProps.Debug;
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
                IReadOnlyList<Player> players = new List<Player>();

                //push properties to renderer
                _renderer.LobbyProps = props;

                //event loop
                while (true)
                {
                    //get discovered players
                    if (props.TimeSinceLastPlayerUpdate.TotalMilliseconds > 1000)
                    {
                        players = _communications.GetDiscoveredPlayers(top: 5);
                        props.UpdatePlayers(players);
                    }

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
            CommandResult result = CommandResult.Unspecified;
            opponent = null;
            try
            {
                //show message
                _renderer.MessageProps = new MessageProperties(
                    line: "Connecting to opponent..",
                    size: 24,
                    buttons: MessageButtons.None);

                //connect to opponent
                Thread.Sleep(750);
                bool success = _communications.SetOpponentAndConnect(player);
                if (!success)
                    return result = CommandResult.Error;

                //show message
                _renderer.MessageProps = new MessageProperties(
                    line: "Invite sent.  Waiting for response..",
                    size: 24,
                    buttons: MessageButtons.None);

                //send invite and wait
                result = _communications.InviteOpponent();
                if (result == CommandResult.Accept)
                    opponent = new Opponent(player);
            }
            finally
            {
                //clear message
                _renderer.MessageProps = null;

                //show message if no acceptance
                if (result == CommandResult.Error)
                    MessageBoxLoop(new MessageProperties("Unable to connect, an error occurred."));
                else if (result == CommandResult.Reject)
                    MessageBoxLoop(new MessageProperties("Player has declined your invite."));
                else if (result == CommandResult.Timeout)
                    MessageBoxLoop(new MessageProperties("Request timeout, or no player response."));
                else if (result == CommandResult.Unspecified)
                    MessageBoxLoop(new MessageProperties("Unable to connect, an unspecified error occurred."));
            }

            //return
            return result;
        }

        #endregion

        #region Two-Player Respond

        /// <summary>
        /// Prompts user with opponent request, and starts two-player game loop (or
        /// returns to caller).
        /// </summary>
        private void OpponentRespondLoop()
        {
            //return if no pending oppponent
            Player pendingOpponent = _pendingOpponent;
            if (pendingOpponent == null)
                return;

            //prompt user to accept
            bool accept = MessageBoxLoop(new MessageProperties(
                lines: new string[]
                {
                    $"{pendingOpponent.Name} has challenged you to a match!",
                    $"Do you accept?"
                },
                buttons: MessageButtons.NoYes));

            //decline?
            if (!accept)
            {
                //connect, send rejection, disconnect
                _communications.RejectInvite(pendingOpponent);
                _opponent = null;
                _pendingOpponent = null;
                return;
            }

            //connect and send acceptance
            bool success = _communications.AcceptInviteAndConnect(pendingOpponent);
            if (!success)
            {
                _opponent = null;
                _pendingOpponent = null;
                return;
            }

            //set opponent
            _opponent = new Opponent(pendingOpponent);
            _pendingOpponent = null;

            //run new game loop
            GameLoop(newGame: true);
        }

        #endregion

        #region Communications

        /// <summary>
        /// Sends a data packet to opponent containing game status.
        /// </summary>
        private void SendGameStatus()
        {
            try
            {
                //return if no opponent
                if (_opponent == null)
                    return;

                //copy matrix, add live brick
                byte[,] matrix = (byte[,])_matrix.Grid.Clone();
                Brick brick = _matrix.Brick;
                if (brick != null)
                {
                    for (int x = 0; x < brick.Width; x++)
                        for (int y = 0; y < brick.Height; y++)
                            if (brick.Grid[x, y] > 0)
                                matrix[x + brick.X, y + brick.Y] = brick.Grid[x, y];
                }

                //serialize data                
                PacketBuilder builder = new PacketBuilder();
                builder.AddBytes2D(matrix);
                builder.AddUInt16((ushort)_stats.Level);
                builder.AddUInt16((ushort)_stats.Lines);
                builder.AddUInt16((ushort)_stats.Score);
                builder.AddUInt16((ushort)_stats.LinesSent);
                builder.AddBoolean(_stats.GameOver);
                byte[] bytes = builder.ToBytes();

                //send data packet
                _communications.SendData(bytes);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Fired when game status packet received.
        /// </summary>
        private void ReceivedGameStatus(DataPacket packet)
        {
            try
            {
                //return if no opponent
                Opponent opponent = _opponent;
                if (opponent == null)
                    return;

                //deserialize data
                PacketParser parser = new PacketParser(packet.Data);
                byte[,] matrix = parser.GetBytes2D();
                int level = parser.GetUInt16();
                int lines = parser.GetUInt16();
                int score = parser.GetUInt16();
                int linesSent = parser.GetUInt16();
                bool gameOver = parser.GetBoolean();

                //update opponent
                opponent.UpdateOpponent(matrix, level, lines, score, linesSent, gameOver);
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        /// <summary>
        /// Fired when command packet received.
        /// </summary>
        private void ReceivedCommandPacket(CommandRequestPacket packet)
        {
            try
            {
                //switch ((CommandType)packet.CommandType)
                //{
                //    //TODO: DETERMINE GAVE-OVER FROM DATA
                //    case CommandType.Passthrough:
                //        _opponent?.SetGameOver();
                //        _communications.SendCommandResponse(packet.CommandType, packet.Sequence, CommandResult.Accept, null);
                //        break;
                //}
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex);
            }
        }

        #endregion

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
        /// Swaps hold brick.  Tries to avoid a collision, but returns true (game over) if it can't.
        /// </summary>
        private bool HoldBrick()
        {
            return _matrix.HoldBrick();
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
            //add brick to matrix
            _matrix.AddBrickToMatrix();

            //identify any solid rows
            List<int> rows = _matrix.IdentifySolidRows();

            //have rows?
            if (rows.Count > 0)
            {
                //increment lines
                _stats.IncrementLines(rows.Count);

                //calculate base score + bonus
                int points, linesToSend = 0;
                switch (rows.Count)
                {
                    case 1:
                        points = 40;
                        break;
                    case 2:
                        points = 100 + (_opponent != null ? 40 : 0);
                        linesToSend = _opponent != null ? 1 : 0;
                        break;
                    case 3:
                        points = 300 + (_opponent != null ? 100 : 0);
                        linesToSend = _opponent != null ? 2 : 0;
                        break;
                    case 4:
                        points = 1200 + (_opponent != null ? 1200 : 0);
                        linesToSend = _opponent != null ? 4 : 0;
                        break;
                    default:
                        points = 0;
                        linesToSend = 0;
                        break;
                }

                //increment score
                _stats.IncrementScore(points);

                //animation + matrix change
                //todo: add send lines animations
                EraseFilledRows(rows);
                DropGrid();

                //increment sent lines
                _stats.IncrementLinesSent(linesToSend);
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

        /// <summary>
        /// Adds lines sent by opponent.
        /// </summary>
        private bool AddSentLines(int newLines)
        {
            //vars
            int gapIndex = _random.Next(10) + 1;

            //move lines up
            bool outBounds = false;
            for (int y = 1; y <= 20; y++)
            {
                for (int x = 1; x <= 10; x++)
                {
                    if (_matrix.Grid[x, y] != 0)
                    {
                        int newY = y - newLines;
                        if (newY > 0)
                            _matrix.Grid[x, newY] = _matrix.Grid[x, y];
                        else
                            outBounds = true;
                        _matrix.Grid[x, y] = 0;
                    }
                }
            }

            //add lines (animated)
            DateTime start = DateTime.Now;
            double xPerSecond = 50;
            int xx = 0;
            while (xx < 10)
            {
                Thread.Sleep(5);
                TimeSpan elapsed = DateTime.Now - start;
                int expectedX = (int)Math.Round(xPerSecond * elapsed.TotalSeconds);
                while (xx < expectedX)
                {
                    xx++;
                    if ((xx < 1) || (xx > 10))
                        break;
                    for (int y = 20; y > 20 - newLines; y--)
                        _matrix.Grid[xx, y] = (byte)(xx != gapIndex ? 9 : 0);
                }
            }

            //return
            return outBounds;
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

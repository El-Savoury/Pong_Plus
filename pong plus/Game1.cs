using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace pong_plus
{
    public class Game1 : Game
    {
        #region Variables
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Render surface vars
        private RenderTarget2D renderSurface; // Initialise render surface to draw game to
        private Rectangle renderRectangle; // Area within window where the game is rendered to 
        private Texture2D pixel;
        private GameScreen gameScreen;

        // Keyboard vars
        KeyboardState kbd, prevKbd;

        // Timer vars
        private float countDuration = 2f;
        private float currentTime = 0f;

        // Define game states
        public enum GameState { Idle, ModeSelect, Start, Play, ShowScore, CheckEnd };
        private GameState gameState; // Var to store current state
        private bool mode; // Var to choose 1P or 2P mode

        // Ball vars
        private PongBall ball; // Defines position, size and bounding box of ball
        private readonly Random rand; // Inits random object to allow random ball direction

        // Paddle vars
        private Paddle[] paddle;
        private bool lastHit = true; // Which side hit/scored last (left = true right = false)

        // Powerup vars
        private PowerUp powerUp;
        private bool powerUpExists = false;
        private float countDown; // Countdown until powerup spawns

        // Scoring vars
        private const int winScore = 5; // Score needed to win
        private int[] score; // Current scores
        private bool win; // True if a player has won

        // Font
        private SpriteFont font;

        // Colours
        private Color startColour, leftColour, rightColour, white, blue, red, green, yellow;

        // Sounds
        private SoundEffect bounceSound, hitSound, scoreSound, selectSound, startSound;
        #endregion

        #region Methods
        // Scale render surface to allow window resizing
        private void OnWindowResize(object sender, EventArgs e)
        {
            int width = Window.ClientBounds.Width;
            int height = Window.ClientBounds.Height;

            if (height < width / (float)renderSurface.Width * renderSurface.Height)
            {
                width = (int)(height / (float)renderSurface.Height * renderSurface.Width);
            }
            else { height = (int)(width / (float)renderSurface.Width * renderSurface.Height); }

            int x = (Window.ClientBounds.Width - width) / 2;
            int y = (Window.ClientBounds.Height - height) / 2;
            renderRectangle = new Rectangle(x, y, width, height);
        }

        // Move paddles method
        private void MovePaddles()
        {
            // 1P mode
            if (mode)
            {
                paddle[0].playerMove(true, mode);
                paddle[1].PaddleAi(ball);
            }
            // 2P mode
            else
            {
                paddle[0].playerMove(true, mode);
                paddle[1].playerMove(false, mode);
            }
        }

        // Spawn powerup method
        private void SpawnPowerUp(GameTime gameTime, float countDown)
        {
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (currentTime >= countDown)
            {
                powerUp = new PowerUp(rand, lastHit, 16, 1, 3);
                powerUpExists = true;
                currentTime = 0f;
            }
        }
        #endregion

        #region Constructor
        public Game1() // Game1 class constructor
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.TargetElapsedTime = new TimeSpan(333333); // Set 30fps
            Window.AllowUserResizing = true;

            gameState = GameState.Idle; // Start game in idle state

            rand = new Random(); // Init random number generator object

            paddle = new Paddle[2]; // Init array of 2 paddles
        }
        #endregion

        #region Initialise
        protected override void Initialize() // TODO: Add your initialization logic here
        {
            renderSurface = new RenderTarget2D(GraphicsDevice, 800, 600); // Set render surface resolution
            gameScreen = new GameScreen();

            // Set window size
            int windowWidth = 1280;
            int windowHeight = 720;

            graphics.PreferredBackBufferWidth = windowWidth;
            graphics.PreferredBackBufferHeight = windowHeight;

            graphics.IsFullScreen = false;

            graphics.ApplyChanges();

            Window.ClientSizeChanged += OnWindowResize;
            OnWindowResize(null, null);

            ball = new PongBall(rand, lastHit); // Instantiate ball class - Create ball in centre of screen

            base.Initialize();
        }
        #endregion

        #region LoadContent
        protected override void LoadContent() // TODO: use this.Content to load your game content here
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create 1x1 white pixel texture
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new Color[] { Color.White });

            // Load font
            font = Content.Load<SpriteFont>("start");

            // Load sounds
            bounceSound = Content.Load<SoundEffect>("PongSounds/bounce");
            hitSound = Content.Load<SoundEffect>("PongSounds/bump2");
            scoreSound = Content.Load<SoundEffect>("PongSounds/padbump");
            selectSound = Content.Load<SoundEffect>("PongSounds/echo");
            startSound = Content.Load<SoundEffect>("PongSounds/bump");

            // Define custom RGB colours
            white = new Color(199, 198, 198);
            blue = new Color(5, 46, 112);
            red = new Color(173, 43, 34);
            green = new Color(39, 150, 43);
            yellow = new Color(219, 219, 26);
            startColour = green;
            leftColour = green;
            rightColour = blue;
        }
        #endregion

        #region Update
        protected override void Update(GameTime gameTime) // TODO: Add your update logic here
        {
            kbd = Keyboard.GetState();

            // Close window
            if (kbd.IsKeyDown(Keys.Escape)) { Exit(); }

            switch (gameState)
            {
                case GameState.Idle:
                    (_, bool idleBounced) = ball.MoveBall(bounce: true);

                    if (idleBounced)
                    {
                        bounceSound.Play(0.4f, 0, 0);
                        startColour = startColour == green ? yellow : green; // Change text colour on ball bounce
                    }

                    if (kbd.IsKeyDown(Keys.Space))
                    {
                        startSound.Play(0.5f, 0, 0);
                        gameState = GameState.ModeSelect;
                    }

                    // Reset default mode and choice colours
                    mode = true;
                    leftColour = green;
                    rightColour = blue;
                    break;

                case GameState.ModeSelect:

                    (_, bool modeBounced) = ball.MoveBall(bounce: true);

                    if (modeBounced) { bounceSound.Play(0.4f, 0, 0); }

                    // Select game mode (true = 1P, false = 2P)
                    if (kbd.IsKeyDown(Keys.Left))
                    {
                        leftColour = green;
                        rightColour = blue;
                        mode = true;
                    }

                    if (kbd.IsKeyDown(Keys.Right))
                    {
                        leftColour = blue;
                        rightColour = yellow;
                        mode = false;
                    }

                    // Start game
                    if (kbd.IsKeyDown(Keys.Space) && prevKbd.IsKeyUp(Keys.Space))
                    {
                        selectSound.Play(0.5f, 0, 0);
                        gameState = GameState.Start;
                    }
                    break;

                case GameState.Start:

                    ball = new PongBall(rand, lastHit); // Create new ball object at start of each point.
                    paddle[0] = new Paddle(false); // Create left paddle
                    paddle[1] = new Paddle(true);  // Create right paddle

                    // Reset scores & powerup timer
                    win = false;
                    score = new int[2]; 
                    countDown = rand.Next(3, 15);

                    gameState = GameState.Play;
                    break;

                case GameState.Play:
                    (int scored, bool bounced) = ball.MoveBall(bounce: false); // Store returned 1 or -1 value to check which side scored 

                    if (bounced) { bounceSound.Play(0.4f, 0, 0); }

                    MovePaddles();

                    // Spawn and move powerup
                    if (powerUpExists) { powerUp.MoveBall(true); }
                    else if (!powerUpExists) { SpawnPowerUp(gameTime, countDown); }

                    // Collision checks
                    bool hit = paddle[0].CollisionCheck(ball);
                    hit |= paddle[1].CollisionCheck(ball);

                    if (hit)
                    {
                        hitSound.Play(1f, 0, 0);
                        return;
                    }

                    if (scored == 0) return;

                    if (scored == 1) // Left side scored
                    {
                        scoreSound.Play(0.25f, 0, 0);
                        lastHit = true;
                        score[0]++;
                        gameState = GameState.ShowScore;
                    }

                    if (scored == -1) // Right side scored
                    {
                        scoreSound.Play(0.25f, 0, 0);
                        lastHit = false;
                        score[1]++;
                        gameState = GameState.ShowScore;
                    }
                    break;

                case GameState.ShowScore:

                    // Despawn powerup & reset timer
                    powerUpExists = false;
                    powerUp = null;
                    countDown = rand.Next(3, 15);

                    MovePaddles();

                    // Start timer to show score for 2 seconds
                    currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (currentTime >= countDuration)
                    {
                        gameState = GameState.CheckEnd;
                        currentTime = 0f;
                    }

                    ball = new PongBall(rand, lastHit); // Create new ball object at start of each point.
                    break;

                case GameState.CheckEnd:

                    // Check for win by 2 clear points
                    if ((score[0] >= winScore && (score[0] - score[1]) > 1) || (score[0] >= winScore && score[1] == 0)) // Left side
                    {
                        win = true;
                        gameState = GameState.Idle;
                    }
                    else if ((score[1] >= winScore && (score[1] - score[0]) > 1) || (score[1] >= winScore && score[0] == 0)) // Right side
                    {
                        win = true;
                        gameState = GameState.Idle;
                    }
                    else { gameState = GameState.Play; }
                    break;

                default:
                    gameState = GameState.Idle;
                    break;
            }
            prevKbd = kbd;
            base.Update(gameTime);
        }
        #endregion

        #region Draw
        protected override void Draw(GameTime gameTime) // TODO: Add your drawing code here
        {
            GraphicsDevice.SetRenderTarget(renderSurface); // Tell graphics device to draw to render surface
            GraphicsDevice.Clear(Color.Black); // Draw black background

            spriteBatch.Begin();
            gameScreen.drawBorder(spriteBatch, pixel, blue); // Draw sides of court
            gameScreen.drawNet(spriteBatch, pixel, blue);    // Draw centre line

            switch (gameState)
            {
                case GameState.Idle:

                    spriteBatch.DrawString(font, "PONG    PLUS", new Vector2(190, GameScreen.border.Y), startColour);
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    break;

                case GameState.ModeSelect:

                    spriteBatch.DrawString(font, "1P", new Vector2(200, GameScreen.border.Y + 8), leftColour);
                    spriteBatch.DrawString(font, "2P", new Vector2(GameScreen.border.Right - 200, GameScreen.border.Y + 8), rightColour);
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    break;

                case GameState.Start:
                    break;

                case GameState.Play:
                    // Draw score font in blue
                    spriteBatch.DrawString(font, score[0].ToString("D2"), new Vector2(200, GameScreen.border.Y + 8), blue);
                    spriteBatch.DrawString(font, score[1].ToString("D2"), new Vector2(GameScreen.border.Right - 200, GameScreen.border.Y + 8), blue);
                    spriteBatch.DrawString(font, countDown.ToString(), new Vector2(GameScreen.border.Right - 100, GameScreen.border.Y + 8), red);

                    // Draw ball and paddles
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    spriteBatch.Draw(pixel, paddle[0].padBounds, green);
                    spriteBatch.Draw(pixel, paddle[1].padBounds, yellow);

                    // Draw powerup
                    if (powerUpExists) { spriteBatch.Draw(pixel, powerUp.BallBounds, red); }
                    break;

                case GameState.ShowScore:

                    // Check for left side win
                    if ((score[0] >= winScore && (score[0] - score[1]) > 1) || (score[0] >= winScore && score[1] == 0))
                    {
                        spriteBatch.DrawString(font, "WIN", new Vector2(190, GameScreen.border.Y + 8), green);
                    }
                    // Check for right side win
                    else if ((score[1] >= winScore && (score[1] - score[0]) > 1) || (score[1] >= winScore && score[0] == 0))
                    {
                        spriteBatch.DrawString(font, "WIN", new Vector2(GameScreen.border.Right - 210, GameScreen.border.Y + 8), yellow);
                    }
                    // Draw score font in scoring colour
                    else
                    {
                        spriteBatch.DrawString(font, score[0].ToString("D2"), new Vector2(200, GameScreen.border.Y + 8), lastHit ? green : blue);
                        spriteBatch.DrawString(font, score[1].ToString("D2"), new Vector2(GameScreen.border.Right - 200, GameScreen.border.Y + 8), lastHit ? blue : yellow);
                    }

                    // Draw paddles
                    spriteBatch.Draw(pixel, paddle[0].padBounds, green);
                    spriteBatch.Draw(pixel, paddle[1].padBounds, yellow);
                    break;

                case GameState.CheckEnd:

                    // Draw score font
                    if (!win)
                    {
                        spriteBatch.DrawString(font, score[0].ToString("D2"), new Vector2(200, GameScreen.border.Y + 8), blue);
                        spriteBatch.DrawString(font, score[1].ToString("D2"), new Vector2(GameScreen.border.Right - 200, GameScreen.border.Y + 8), blue);
                    }

                    // Draw ball and paddles
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    spriteBatch.Draw(pixel, paddle[0].padBounds, green);
                    spriteBatch.Draw(pixel, paddle[1].padBounds, yellow);
                    break;
            }
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null); // Stop drawing to render surface to allow it to be drawn to the screen

            spriteBatch.Begin();
            spriteBatch.Draw(renderSurface, renderRectangle, Color.White); // Draw render surface to screen
            spriteBatch.End();

            base.Draw(gameTime);
        }
        #endregion
    }
}

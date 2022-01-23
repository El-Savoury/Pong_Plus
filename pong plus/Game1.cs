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
        public float currentTime = 0f;

        // Define game states
        public enum GameState { Idle, ModeSelect, Start, Play, ShowScore, CheckEnd };
        private GameState gameState; // Var to store current state
        private bool mode; // Var to choose 1P or 2P mode

        // Ball vars
        private PongBall ball; // Defines position, size and bounding box of ball
        private readonly Random rand; // Inits random object to allow random ball direction

        // Paddle vars
        private Paddle[] paddle;
        private bool lastHit = true; // Which side hit/scored last (left = true, right = false)

        // Powerup vars
        private PongBall powerUp;
        private float countDown; // Countdown until powerup spawns
        private float powerTimer = 0f; // Times how long powerup is active
        private bool pickup; // Which side picked up powerup
        private bool powerUpExists, losingSide, powerUpGet, powerUpReady, powerUpActive, soundPlayed = false;

        // Powerup icon vars
        private Texture2D[] powerUpTextures;
        private Texture2D laserpaddle, controlBall, sidewaysPaddle, multiLaser, bigPaddle;
        private int iconIndex;
        private float alpha;

        // Scoring vars
        private const int winScore = 5; // Score needed to win
        private int[] score; // Current scores
        private bool win; // True if a player has won

        // Font
        private SpriteFont font;

        // Colours
        public static Color startColour, leftColour, rightColour, white, blue, red, darkRed, green, yellow;

        // Sounds
        public static SoundEffect bounceSound, hitSound, scoreSound, selectSound, startSound, deepSound, powerBounceSound, powerPickupSound, powerActivateSound;
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
            if (mode) // 1P mode
            {
                paddle[0].playerMove(true, mode, paddle[0]);
                paddle[1].PaddleAi(ball);
            }
            else // 2P mode
            {
                paddle[0].playerMove(true, mode, paddle[0]);
                paddle[1].playerMove(false, mode, paddle[1]);
            }
        }

        private void ResetPaddles()
        {
            int leftPadY = paddle[0].PadBounds.Y;
            int rightPadY = paddle[1].PadBounds.Y;

            paddle[0] = new Paddle(false) { PadBounds = new Rectangle(GameScreen.border.Left + 16, leftPadY, 8, 32) };
            paddle[1] = new Paddle(true) { PadBounds = new Rectangle(GameScreen.border.Right - 20, rightPadY, 8, 32) };
        }

        // Spawn powerup method
        private void SpawnPowerUp(GameTime gameTime, float countDown)
        {
            currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (currentTime >= countDown)
            {
                powerUp = new PongBall(rand, losingSide, 24, 1, 3);
                powerUpExists = true;
                powerActivateSound.Play(0.1f, 0, 0);
                currentTime = 0f;
            }
        }

        private void ResetPowerUp()
        {
            // Despawn powerup & reset vars
            powerUp = null;
            powerUpExists = false;
            powerUpGet = false;
            powerUpReady = false;
            powerUpActive = false;
            soundPlayed = false;

            // Reset timers
            powerTimer = 0f;
            countDown = rand.Next(1, 2);
        }

        private void DrawPowerupTrail()
        {

        }

        // Flash powerup icon
        public void FlashIcon(GameTime Time, float a)
        {
            float timer = 0.8f;
            currentTime += (float)Time.ElapsedGameTime.TotalSeconds;

            if (currentTime <= timer) { a = 0f; }
            else if (currentTime > timer && currentTime <= timer * 2) { a = 1f; }
            else { currentTime = 0f; }

            alpha = a;
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

            // Load Sprites
            controlBall = Content.Load<Texture2D>("PongSprites/vert arrows");
            sidewaysPaddle = Content.Load<Texture2D>("PongSprites/side arrows");
            bigPaddle = Content.Load<Texture2D>("PongSprites/big paddle");
            laserpaddle = Content.Load<Texture2D>("PongSprites/laser");
            multiLaser = Content.Load<Texture2D>("PongSprites/multi laser");

            powerUpTextures = new Texture2D[] { controlBall, sidewaysPaddle, bigPaddle, laserpaddle, multiLaser };

            // Load font
            font = Content.Load<SpriteFont>("start");

            // Load sounds
            bounceSound = Content.Load<SoundEffect>("PongSounds/bounce");
            hitSound = Content.Load<SoundEffect>("PongSounds/bump2");
            scoreSound = Content.Load<SoundEffect>("PongSounds/padbump");
            selectSound = Content.Load<SoundEffect>("PongSounds/echo");
            startSound = Content.Load<SoundEffect>("PongSounds/bump");
            deepSound = Content.Load<SoundEffect>("PongSounds/DeepSound");
            powerBounceSound = Content.Load<SoundEffect>("PongSounds/powerBounce");
            powerPickupSound = Content.Load<SoundEffect>("PongSounds/Powerup4");
            powerActivateSound = Content.Load<SoundEffect>("PongSounds/WeirdBounce");

            // Define custom RGB colours
            white = new Color(199, 198, 198);
            blue = new Color(5, 46, 112);
            red = new Color(173, 43, 34);
            darkRed = new Color(127, 24, 24);
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
                    countDown = rand.Next(1, 2);

                    gameState = GameState.Play;
                    break;

                case GameState.Play:
                    (int scored, bool bounced) = ball.MoveBall(bounce: false); // Store returned 1 or -1 value to check which side scored 

                    if (bounced) { bounceSound.Play(0.4f, 0, 0); }

                    MovePaddles();

                    // Spawn and move powerup
                    if (powerUpExists)
                    {
                        (_, bool powerUpBounce) = powerUp.MoveBall(true);
                        if (powerUpBounce) { powerBounceSound.Play(0.6f, 0, 0); }

                        // Hit powerup with paddle
                        (powerUpGet, powerUpExists, pickup) = PowerUp.PickupPowerUp(paddle, powerUp);

                        // Hit powerup with ball
                        if (PowerUp.PowerUpCollision(powerUp, ball))
                        {
                            (powerUpGet, powerUpExists) = PowerUp.DespawnPowerUp(powerUp);

                            pickup = Math.Sign(ball.Velocity.X) == 1; // Set pickup side based on which way ball is moving
                        }
                    }
                    else if (!powerUpExists && !powerUpGet)
                    {
                        // Get losing side
                        if (score[0] > score[1]) { losingSide = true; }
                        else { losingSide = false; }

                        SpawnPowerUp(gameTime, countDown);
                    }

                    // Use powerup
                    if (powerUpGet && !powerUpReady)
                    {
                        iconIndex = rand.Next(0, 3);
                        alpha = 1f;
                        powerUpReady = true;
                    }
                    else if (powerUpGet && powerUpReady)
                    {
                        Keys powerKey = pickup ? Keys.E : Keys.RightControl;
                        bool pressPowerKey = kbd.IsKeyDown(powerKey) && prevKbd.IsKeyUp(powerKey);

                        if (pressPowerKey)
                        {
                            powerUpActive = true;
                            if (!soundPlayed)
                            {
                                powerActivateSound.Play(0.1f, 0, 0);
                                soundPlayed = true;
                            }
                        }
                        if (powerUpActive)
                        {
                            powerTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                            if (powerTimer <= 8f)
                            {
                                PowerUp.RandomisePowerUp(iconIndex, ball, paddle[pickup ? 0 : 1], pickup);
                                FlashIcon(gameTime, alpha);
                            }
                            else
                            {
                                ResetPaddles();
                                deepSound.Play(0.12f, 0, 0);
                                ball = new PongBall(rand, lastHit) { BallBounds = ball.BallBounds, Velocity = ball.Velocity };
                                ResetPowerUp();
                            }
                        }
                    }

                    // Ball collision checks
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

                    ResetPaddles();
                    ResetPowerUp();
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
            gameScreen.DrawBorder(spriteBatch, pixel, blue); // Draw sides of court
            gameScreen.DrawNet(spriteBatch, pixel, blue);    // Draw centre line

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

                    // Draw powerup
                    if (powerUpExists) { spriteBatch.Draw(pixel, powerUp.BallBounds, red); }
                    spriteBatch.DrawString(font, powerTimer.ToString("0.0"), new Vector2(0, -10), red);

                    // Draw laser
                    //spriteBatch.Draw(pixel, new Rectangle(laser.BallBounds.X, laser.BallBounds.Y, 24, 8), red);

                    // Draw powerup icon
                    if (powerUpGet && pickup) { spriteBatch.Draw(powerUpTextures[iconIndex], new Vector2(290, GameScreen.border.Y + 27), red * alpha); }
                    else if (powerUpGet && !pickup) { spriteBatch.Draw(powerUpTextures[iconIndex], new Vector2(453, GameScreen.border.Y + 27), null, red * alpha, 0f, new Vector2(0, 0), new Vector2(1, 1), SpriteEffects.FlipHorizontally, 0f); }

                    // Draw ball and paddles
                    spriteBatch.Draw(pixel, ball.BallBounds, ball.BallColour);
                    spriteBatch.Draw(pixel, paddle[0].PadBounds, paddle[0].PadColour);
                    spriteBatch.Draw(pixel, paddle[1].PadBounds, paddle[1].PadColour);
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
                    spriteBatch.Draw(pixel, paddle[0].PadBounds, green);
                    spriteBatch.Draw(pixel, paddle[1].PadBounds, yellow);
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
                    spriteBatch.Draw(pixel, paddle[0].PadBounds, green);
                    spriteBatch.Draw(pixel, paddle[1].PadBounds, yellow);
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

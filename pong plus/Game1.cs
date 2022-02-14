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
        private KeyboardState kbd, prevKbd;

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
        private readonly Paddle[] paddle;
        private bool lastHit = true; // Which side hit/scored last (left = true, right = false)

        // Powerup vars
        private PongBall powerUp;
        private PongBall[] bullets;
        private int powerSize;
        private float countDown; // Countdown until powerup spawns
        private float powerTimer = 0f; // Times how long powerup is active
        private bool pickup; // Which side picked up powerup
        private bool powerUpExists, losingSide, powerUpGet, powerUpReady, powerUpActive, soundPlayed, bulletFired = false;
        private bool shotgunFired, laserFired;

        // Powerup trail vars
        private Rectangle powerTrail, powerTrail2;
        private bool drawTrail, drawTrail2 = false;
        private float trailTime = 0f;

        // Powerup icon vars
        private Texture2D[] powerUpTextures;
        private Texture2D laserpaddle, controlBall, sidewaysPaddle, shotgunBlast, bigPaddle;
        private int iconIndex;

        // Scoring vars
        private const int winScore = 5; // Score needed to win
        private int[] score; // Current scores
        private bool win; // True if a player has won
        private float scoreTime = 0f;

        // Font
        private SpriteFont font;

        // Colours
        public static Color startColour, leftColour, rightColour, white, blue, red, purple, green, yellow;

        // Sounds
        private SoundEffect bounceSound, hitSound, scoreSound, selectSound, startSound, deepSound; // Base sounds
        public static SoundEffect powerBounceSound, powerPickupSound, powerActivateSound, shotgunSound; // Powerup sounds
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
                paddle[1].PaddleAi(ball, paddle[1]);
            }
            else // 2P mode
            {
                paddle[0].playerMove(true, mode, paddle[0]);
                paddle[1].playerMove(false, mode, paddle[1]);
            }
        }

        // Set paddles back to default xpos maintaining current ypos
        private void ResetPaddles()
        {
            int leftPadY = paddle[0].PadBounds.Y;
            int rightPadY = paddle[1].PadBounds.Y;

            paddle[0] = new Paddle(false) { PadBounds = new Rectangle(GameScreen.border.Left + 16, leftPadY, 8, 32) };
            paddle[0].PadSpeed = 8;

            paddle[1] = new Paddle(true) { PadBounds = new Rectangle(GameScreen.border.Right - 20, rightPadY, 8, 32) };
            paddle[1].PadSpeed = 8;
            paddle[1].AiSpeed = 8;
        }

        private float spawnTime = 0f;
        // Spawn powerup on random timer
        private int SpawnPowerUp(GameTime gameTime, float countDown, Random rand)
        {
            powerSize = rand.Next(22, 37);

            spawnTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Get losing side
            int i;

            if (score[0] > score[1]) { i = 0; }
            else if (score[0] < score[1]) { i = 1; }
            else { i = rand.Next(0, 2); }

            if (i == 0) losingSide = true;
            else if (i == 1) losingSide = false;

            if (spawnTime >= countDown)
            {
                powerUp = new PongBall(rand, losingSide, powerSize, 1, 3);
                powerUpExists = true;
                powerActivateSound.Play(0.1f, 0, 0);
                spawnTime = 0f;
            }
            return powerSize;
        }

        // Draw power up trail
        private void DrawTrail(GameTime gameTime, PongBall powerUp)
        {
            int size = powerSize / 2;

            trailTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (trailTime < 0.1f)
            {
                powerTrail = new Rectangle(powerUp.BallBounds.X, powerUp.BallBounds.Y, size, size);
                drawTrail = true;
                drawTrail2 = true;
            }
            else if (trailTime > 0.5 && trailTime < 0.6) { powerTrail2 = new Rectangle(powerUp.BallBounds.X, powerUp.BallBounds.Y, size, size); }
            else if (trailTime > 0.8f) { trailTime = 0f; }
        }

        // Use power up for whichever paddle has activated it
        private void UsePower()
        {
            powerUpActive = true;
            if (!soundPlayed)
            {
                InitProjectiles(pickup ? paddle[0] : paddle[1], pickup);

                if (iconIndex > 2) { shotgunSound.Play(0.3f, 0, 0); }
                else { powerActivateSound.Play(0.1f, 0, 0); }
                soundPlayed = true;
            }
        }

        private float aiTime = 0f;
        // AI uses power on timer and when ball is moving away
        private void AiUsePower(GameTime time, Random rand, PongBall ball)
        {
            aiTime += (float)time.ElapsedGameTime.TotalSeconds;
            if (aiTime > rand.Next(2, 5) && Math.Sign(ball.Velocity.X) == -1)
            {
                UsePower();
                if (iconIndex == 2) { paddle[1].AiSpeed = 0; }
            }
        }

        // Move ball when AI has control ball powerup 
        private void AiControlBall()
        {
            if (Math.Sign(ball.Velocity.X) == 1 && ball.BallBounds.X > paddle[0].PadBounds.Right + 10)
            {
                if (ball.BallBounds.Y > (paddle[1].PadBounds.Y - 8) + 20 && Math.Sign(ball.Velocity.Y) == 1) { ball.ReverseVelocity(y: true); }
                else if (ball.BallBounds.Y < (paddle[1].PadBounds.Y - 8) - 20 && Math.Sign(ball.Velocity.Y) == -1) { ball.ReverseVelocity(y: true); }
            }
            else if (Math.Sign(ball.Velocity.X) == -1)
            {
                if (ball.BallBounds.Y > (paddle[0].PadBounds.Y - 8) + 20 && Math.Sign(ball.Velocity.Y) == 1) { ball.ReverseVelocity(y: true); }
                else if (ball.BallBounds.Y < (paddle[0].PadBounds.Y - 8) - 20 && Math.Sign(ball.Velocity.Y) == -1) { ball.ReverseVelocity(y: true); }
            }
        }

        // Init projectiles
        private void InitProjectiles(Paddle paddle, bool side)
        {
            int xPos = side ? paddle.PadBounds.Right + 2 : paddle.PadBounds.X - 24;
            int xSpeed = side ? rand.Next(15, 17) : -rand.Next(15, 17);

            // Shotgun
            bullets[0] = new PongBall(xPos, paddle.PadBounds.Y - 8, 8, xSpeed, rand.Next(-5, 1));
            bullets[1] = new PongBall(xPos, paddle.PadBounds.Y + (paddle.PadBounds.Height / 2 - 4), 8, xSpeed, rand.Next(-2, 2));
            bullets[2] = new PongBall(xPos, paddle.PadBounds.Bottom, 8, xSpeed, rand.Next(1, 5));

            // Laser
            bullets[3] = new PongBall(0, 0, 8, side ? 45 : -45, 0) { BallBounds = new Rectangle(side ? paddle.PadBounds.Right : paddle.PadBounds.X - 24, paddle.PadBounds.Y + (paddle.PadBounds.Height / 2 - 4), 24, 8) };
        }

        // Projectile Collisons
        private bool ProjectileCollisions(PongBall[] bullets, Paddle paddle)
        {
            if (paddle.CollisionCheck(bullets[0], false) ||
                paddle.CollisionCheck(bullets[1], false) ||
                paddle.CollisionCheck(bullets[2], false) ||
                paddle.CollisionCheck(bullets[3], false))
            {
                return true;
            }
            else
                return false;
        }

        // Shotgun projectiles despawn if hit paddle or go offscreen
        private void BulletHit(PongBall[] bullets, Paddle paddle, bool side)
        {
            if (bulletFired && ProjectileCollisions(bullets, paddle))
            {
                paddle.PadBounds = new Rectangle(0, mode ? (GameScreen.border.Y + GameScreen.border.Height) / 2 : paddle.PadBounds.Y, 0, 0);
                paddle.PadSpeed = 0;
                paddle.AiSpeed = 0;
                deepSound.Play(0.3f, 0, 0);
            }

            // Play bounce sound
            if (bullets[0].BallBounds.X < GameScreen.border.X + 2 || bullets[0].BallBounds.Right > GameScreen.border.Right - 2) { bounceSound.Play(0.1f, 0.5f, 0); } // bullets[0] = null; }
            if (bullets[1].BallBounds.X < GameScreen.border.X + 2 || bullets[1].BallBounds.Right > GameScreen.border.Right - 2) { bounceSound.Play(0.1f, 0.5f, 0); } //bullets[1] = null; }
            if (bullets[2].BallBounds.X < GameScreen.border.X + 2 || bullets[2].BallBounds.Right > GameScreen.border.Right - 2) { bounceSound.Play(0.1f, 0.5f, 0); } // bullets[2] = null; }

            // Destroy laser
            if (bullets[3].BallBounds.X <= GameScreen.border.X - 1 || bullets[3].BallBounds.Right == GameScreen.border.Right + 20) { bullets[3].BallBounds = new Rectangle(0, 0, 0, 0); }
        }

        private float shotTimer = 0f;
        private readonly float laserTime = 1.5f;
        private bool lasered = false;
        // Laser hit
        private void LaserShot(GameTime time, Paddle paddle, bool side)
        {
            shotTimer += (float)time.ElapsedGameTime.TotalSeconds;

            if (shotTimer < 0.5f && !lasered) { paddle.PadSpeed = 0; }
            else if (shotTimer > 0.5f && shotTimer < laserTime && !lasered) { paddle.PadSpeed = 8; }
            else if (shotTimer >= laserTime && shotTimer < laserTime + 0.05f)
            {
                shotgunSound.Play(0.3f, 0, 0);
                InitProjectiles(paddle, side);
                lasered = true;
            }
            else if (shotTimer > laserTime + 0.05f && shotTimer < laserTime + 0.5f) { paddle.PadSpeed = 0; }
            else if (shotTimer > laserTime + 0.5f)
            {
                paddle.PadSpeed = 8;
                shotTimer = 0f;
            }
        }

        // Despawn powerup & reset vars
        private void ResetPowerUp()
        {
            powerUp = null;
            powerUpExists = false;
            powerUpGet = false;
            powerUpReady = false;
            powerUpActive = false;
            soundPlayed = false;
            bulletFired = false;
            lasered = false;
            drawTrail = false;
            pitch = 0f;
            shotTimer = 0f;
            powerTimer = 0f;
            iconTimer = 0.8f;
            trailTime = 0f;
            aiTime = 0f;
            countDown = rand.Next(1, 2);
            powerTrail = new Rectangle(0, 0, 0, 0);
            powerTrail2 = new Rectangle(0, 0, 0, 0);
        }

        // Flash powerup icon and play timer sound with increasing pitch
        private float alpha;
        private float iconTimer = 0.82f;
        private float pitch = 0f;
        public void FlashIcon(GameTime time, float opacity)
        {
            currentTime += (float)time.ElapsedGameTime.TotalSeconds;

            if (currentTime <= iconTimer) { opacity = 0f; }
            else if (currentTime > iconTimer && currentTime <= iconTimer * 2) { opacity = 1f; }
            else
            {
                currentTime = 0f;
                powerActivateSound.Play(0.1f, pitch, 0);
                pitch += 0.08f;
                iconTimer -= 0.09f;
            }
            alpha = opacity;
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
            bullets = new PongBall[4];

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
            shotgunBlast = Content.Load<Texture2D>("PongSprites/multi laser");

            powerUpTextures = new Texture2D[] { sidewaysPaddle, bigPaddle, controlBall, shotgunBlast, laserpaddle, };

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
            shotgunSound = Content.Load<SoundEffect>("PongSounds/laser");

            // Define custom RGB colours
            white = new Color(199, 198, 198);
            blue = new Color(5, 46, 112);
            red = new Color(173, 43, 34);
            purple = new Color(49, 0, 112);
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

                    // Init ball and paddles
                    ball = new PongBall(rand, lastHit);
                    paddle[0] = new Paddle(false);
                    paddle[1] = new Paddle(true);

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

                        DrawTrail(gameTime, powerUp);

                        // Hit powerup with paddle
                        (powerUpGet, powerUpExists, pickup) = PowerUp.PickupPowerUp(paddle, powerUp);

                        // Hit powerup with ball
                        if (PowerUp.PowerUpCollision(powerUp, ball))
                        {
                            (powerUpGet, powerUpExists) = PowerUp.DespawnPowerUp(powerUp);

                            pickup = Math.Sign(ball.Velocity.X) == 1; // Set pickup side based on which way ball is moving
                        }
                    }
                    else if (!powerUpExists && !powerUpGet) { SpawnPowerUp(gameTime, countDown, rand); }

                    // Use powerup
                    if (powerUpGet && !powerUpReady)
                    {
                        if (mode && !pickup) { iconIndex = rand.Next(1, 5); }
                        else { iconIndex = rand.Next(0, 5); }

                        alpha = 1f;
                        powerUpReady = true;
                    }
                    else if (powerUpGet && powerUpReady)
                    {
                        // Set correct key press to activate powerup based on which mode and pickup side
                        Keys powerKey;
                        if (mode && pickup) { powerKey = Keys.Space; }
                        else { powerKey = pickup ? Keys.E : Keys.RightControl; }

                        bool pressPowerKey = kbd.IsKeyDown(powerKey);

                        // Player use powerup
                        if (pressPowerKey)
                        {
                            UsePower();
                        }
                        // AI use powerup
                        else if (mode && !pickup)
                        {
                            AiUsePower(gameTime, rand, ball);
                        }

                        if (powerUpActive)
                        {
                            powerTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                            if (powerTimer <= 8f)
                            {
                                (shotgunFired, laserFired) = PowerUp.RandomisePowerUp(iconIndex, bullets, ball, paddle[pickup ? 0 : 1], pickup, mode);

                                if (shotgunFired || laserFired) { bulletFired = true; }

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

                    // AI uses move ball power
                    if (mode && !pickup && powerUpActive && iconIndex == 2)
                    {
                        AiControlBall();
                    }

                    // Projectiles fired
                    if (shotgunFired && bulletFired)
                    {
                        BulletHit(bullets, paddle[0], pickup);
                        BulletHit(bullets, paddle[1], pickup);
                    }
                    if (laserFired && bulletFired)
                    {
                        if (pickup)
                        {
                            LaserShot(gameTime, paddle[0], pickup);
                            BulletHit(bullets, paddle[0], pickup);
                            BulletHit(bullets, paddle[1], pickup);
                        }
                        else
                        {
                            LaserShot(gameTime, paddle[1], pickup);
                            BulletHit(bullets, paddle[0], pickup);
                            BulletHit(bullets, paddle[1], pickup);
                        }
                    }

                    // Ball collision checks
                    bool hit = paddle[0].CollisionCheck(ball, bounce: true);
                    hit |= paddle[1].CollisionCheck(ball, bounce: true);

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

                    // Check for a winner by 2 clear points
                    if ((score[0] >= winScore && (score[0] - score[1]) > 1) ||
                        (score[0] >= winScore && score[1] == 0) ||
                        (score[1] >= winScore && (score[1] - score[0]) > 1) ||
                        (score[1] >= winScore && score[0] == 0))
                    { win = true; }

                    // Start timer to show score for 2 seconds
                    scoreTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

                    if (win && scoreTime < 0.05f) { selectSound.Play(0.5f, 0, 0); }
                    else if (scoreTime >= countDuration)
                    {
                        gameState = GameState.CheckEnd;
                        scoreTime = 0f;
                    }

                    ball = new PongBall(rand, lastHit); // Create new ball object at start of each point.
                    break;

                case GameState.CheckEnd:

                    //// Check for win by 2 clear points
                    //if ((score[0] >= winScore && (score[0] - score[1]) > 1) || (score[0] >= winScore && score[1] == 0)) // Left side
                    //{
                    //    win = true;
                    //    gameState = GameState.Idle;
                    //}
                    //else if ((score[1] >= winScore && (score[1] - score[0]) > 1) || (score[1] >= winScore && score[0] == 0)) // Right side
                    //{
                    //    win = true;
                    //    gameState = GameState.Idle;
                    //}
                    if (win) { gameState = GameState.Idle; }
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

                    // Draw powerup icon
                    if (powerUpGet && pickup) { spriteBatch.Draw(powerUpTextures[iconIndex], new Vector2(290, GameScreen.border.Y + 27), purple * alpha); }
                    else if (powerUpGet && !pickup) { spriteBatch.Draw(powerUpTextures[iconIndex], new Vector2(453, GameScreen.border.Y + 27), null, purple * alpha, 0f, new Vector2(0, 0), new Vector2(1, 1), SpriteEffects.FlipHorizontally, 0f); }

                    // Draw powerup
                    if (powerUpExists && drawTrail) { spriteBatch.Draw(pixel, powerTrail, purple /*trail1Col*/); }
                    if (powerUpExists && drawTrail2) { spriteBatch.Draw(pixel, powerTrail2, purple/*trail2Col*/); }
                    if (powerUpExists) { spriteBatch.Draw(pixel, powerUp.BallBounds, purple); }

                    // Draw shotgun blast
                    if (iconIndex == 3 && powerUpActive)
                    {
                        if (bullets[0].BallBounds.X > GameScreen.border.X && bullets[0].BallBounds.Right < GameScreen.border.Right) { spriteBatch.Draw(pixel, bullets[0].BallBounds, red); }
                        if (bullets[1].BallBounds.X > GameScreen.border.X && bullets[1].BallBounds.Right < GameScreen.border.Right) { spriteBatch.Draw(pixel, bullets[1].BallBounds, red); }
                        if (bullets[2].BallBounds.X > GameScreen.border.X && bullets[2].BallBounds.Right < GameScreen.border.Right) { spriteBatch.Draw(pixel, bullets[2].BallBounds, red); }
                    }

                    // Draw laser
                    if (iconIndex == 4 && powerUpActive)
                    {
                        if (bullets[3].BallBounds.X > GameScreen.border.X - 100 && bullets[3].BallBounds.X < GameScreen.border.Right - 20) { spriteBatch.Draw(pixel, bullets[3].BallBounds, red); }
                    }

                    // Draw ball and paddles
                    spriteBatch.Draw(pixel, ball.BallBounds, ball.BallColour);
                    spriteBatch.Draw(pixel, paddle[0].PadBounds, paddle[0].PadColour);
                    spriteBatch.Draw(pixel, paddle[1].PadBounds, paddle[1].PadColour);
                    break;

                case GameState.ShowScore:
                    if ((score[0] >= winScore && (score[0] - score[1]) > 1) || (score[0] >= winScore && score[1] == 0)) { spriteBatch.DrawString(font, "WIN", new Vector2(190, GameScreen.border.Y + 8), green); } // Left side win
                    else if ((score[1] >= winScore && (score[1] - score[0]) > 1) || (score[1] >= winScore && score[0] == 0)) { spriteBatch.DrawString(font, "WIN", new Vector2(GameScreen.border.Right - 210, GameScreen.border.Y + 8), yellow); } // Right side win
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

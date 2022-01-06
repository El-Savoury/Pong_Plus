﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;

namespace pong_plus
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Render surface vars
        private RenderTarget2D renderSurface; // Initialise render surface to draw game to
        private Rectangle renderRectangle; // Area within window where the game is rendered to 
        private Texture2D pixel;
        private gamescreen gameScreen;

        // Keyboard vars
        KeyboardState kbd, prevKbd;

        // Define game states
        public enum GameState { Idle, Mode, Start, Play, CheckEnd };
        private GameState gameState; // Var to store current state
        private bool mode; // Var to choose 1P or 2P mode

        // Ball vars
        private pongball ball; // Defines position, size and bounding box of ball
        private readonly Random rand; // Inits random object to allow random ball direction

        // Paddle vars
        private paddle[] paddles;
        private bool lastHit = true; // Which side hit/scored last (left = true right = false)

        // Scoring vars
        private int[] score;
        private SpriteFont font;

        // RGB colours
        private Color startColour, leftColour, rightColour, white, blue, red, green, yellow;

        // Sounds
        private SoundEffect bounceSound, hitSound, scoreSound, startSound;

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

        public Game1() // Game1 class constructor
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.TargetElapsedTime = new TimeSpan(333333); // Set 30fps
            Window.AllowUserResizing = true;

            gameState = GameState.Idle; // Start game in idle state

            rand = new Random(); // Init random number generator object

            paddles = new paddle[2]; // Init array of 2 paddles
        }

        protected override void Initialize() // TODO: Add your initialization logic here
        {
            renderSurface = new RenderTarget2D(GraphicsDevice, 800, 600); // Set render surface resolution
            gameScreen = new gamescreen();

            // Set window size
            int windowWidth = 1280;
            int windowHeight = 720;

            graphics.PreferredBackBufferWidth = windowWidth;
            graphics.PreferredBackBufferHeight = windowHeight;

            graphics.IsFullScreen = false;

            graphics.ApplyChanges();

            Window.ClientSizeChanged += OnWindowResize;
            OnWindowResize(null, null);

            ball = new pongball(rand, lastHit); // Instantiate ball class - Create ball in centre of screen

            base.Initialize();
        }

        protected override void LoadContent() // TODO: use this.Content to load your game content here
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create 1x1 white pixel texture
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new Color[] { Color.White });

            // Load font
            font = Content.Load<SpriteFont>("start");

            // Load sounds
            bounceSound = Content.Load<SoundEffect>("bounce");
            hitSound = Content.Load<SoundEffect>("bump");
            scoreSound = Content.Load<SoundEffect>("padbump");
            startSound = Content.Load<SoundEffect>("echo");

            // Define custom colours
            white = new Color(199, 198, 198);
            blue = new Color(5, 46, 112);
            red = new Color(173, 43, 34);
            green = new Color(39, 150, 43);
            yellow = new Color(219, 219, 26);
            startColour = green;
            leftColour = yellow;
            rightColour = blue;
        }

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
                        hitSound.Play(0.5f, 0, 0);
                        gameState = GameState.Mode;
                    }

                    mode = true;
                    leftColour = yellow;
                    break;

                case GameState.Mode:

                    (_, bool modeBounced) = ball.MoveBall(bounce: true);

                    if (modeBounced) { bounceSound.Play(0.4f, 0, 0); }

                    // Select game mode (true = 1P, false = 2P)
                    if (kbd.IsKeyDown(Keys.Left))
                    {
                        leftColour = yellow;
                        rightColour = blue;
                        mode = true;
                    }

                    if (kbd.IsKeyDown(Keys.Right))
                    {
                        leftColour = blue;
                        rightColour = green;
                        mode = false;
                    }

                    // Start game
                    if (kbd.IsKeyDown(Keys.Space) && prevKbd.IsKeyUp(Keys.Space))
                    {
                        startSound.Play(0.5f, 0, 0);
                        gameState = GameState.Start;
                    }
                    break;

                case GameState.Start:
                    ball = new pongball(rand, lastHit); // Create new ball object at start of each point.
                    paddles[0] = new paddle(false);     // Create left paddle
                    paddles[1] = new paddle(true);      // Create right paddle

                    score = new int[2]; // Reset scores

                    gameState = GameState.Play;
                    break;

                case GameState.Play:
                    (int scored, bool bounced) = ball.MoveBall(bounce: false); // Store returned 1 or -1 value to check which side scored 

                    if (bounced) { bounceSound.Play(0.4f, 0, 0); }

                    // 1P mode
                    if (mode)
                    {
                        paddles[0].playerMove(true, mode);
                        paddles[1].PaddleAi(ball);
                    }
                    // 2P mode
                    else
                    {
                        paddles[0].playerMove(true, mode);
                        paddles[1].playerMove(false, mode);
                    }

                    // Collision checks
                    bool hit = paddles[0].CollisionCheck(ball);
                    hit |= paddles[1].CollisionCheck(ball);

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
                        gameState = GameState.CheckEnd;
                    }

                    if (scored == -1) // Right side scored
                    {
                        scoreSound.Play(0.25f, 0, 0);
                        lastHit = false;
                        score[1]++;
                        gameState = GameState.CheckEnd;
                    }
                    break;

                case GameState.CheckEnd:

                    leftColour = blue;
                    rightColour = blue;

                    if (score[0] > 10 || score[1] > 10) gameState = GameState.Idle;
                    else gameState = GameState.Play;

                    ball = new pongball(rand, lastHit); // Create new ball object at start of each point.
                    break;

                default:
                    gameState = GameState.Idle;
                    break;
            }

            prevKbd = kbd;
            base.Update(gameTime);
        }

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

                    spriteBatch.DrawString(font, "PONG    PLUS", new Vector2(190, gamescreen.border.Y), startColour);
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    break;

                case GameState.Mode:

                    spriteBatch.DrawString(font, "1P", new Vector2(200, gamescreen.border.Y + 8), leftColour);
                    spriteBatch.DrawString(font, "2P", new Vector2(gamescreen.border.Right - 200, gamescreen.border.Y + 8), rightColour);
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    break;

                case GameState.Start:
                    break;

                case GameState.Play:
                    spriteBatch.DrawString(font, score[0].ToString("D2"), new Vector2(200, gamescreen.border.Y + 8), blue);
                    spriteBatch.DrawString(font, score[1].ToString("D2"), new Vector2(gamescreen.border.Right - 200, gamescreen.border.Y + 8), blue);

                    // Draw ball and paddles
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    spriteBatch.Draw(pixel, paddles[0].padBounds, green);
                    spriteBatch.Draw(pixel, paddles[1].padBounds, yellow);
                    break;

                case GameState.CheckEnd:

                    // Draw score font
                    //spriteBatch.DrawString(font, score[0].ToString("D2"), new Vector2(200, gamescreen.border.Y + 8), leftColour);
                    //spriteBatch.DrawString(font, score[1].ToString("D2"), new Vector2(gamescreen.border.Right - 200, gamescreen.border.Y + 8), rightColour);

                    // Draw ball and paddles
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    spriteBatch.Draw(pixel, paddles[0].padBounds, green);
                    spriteBatch.Draw(pixel, paddles[1].padBounds, yellow);
                    break;
            }
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null); // Stop drawing to render surface to allow it to be drawn to the screen

            spriteBatch.Begin();
            spriteBatch.Draw(renderSurface, renderRectangle, Color.White); // Draw render surface to screen
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

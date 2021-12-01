using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace pong
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private RenderTarget2D doubleBuffer; //initialise render surface to draw game to
        private Rectangle renderRectangle; //area within window where the game is rendered to 
        private Texture2D texture; //used to draw white pixel

        //ball variables
        private Rectangle ball; //defines position, size and bounding box of ball
        private Point ballSpeed; //holds x and y int values to define ball velocity 
        private bool lastHit = true; //which side hit/scored last (left = true right = false)
        private readonly Random rand; //inits random object to allow random ball direction


        public enum GameState { Idle, Start, Play, CheckEnd } //intialise enumeration type to define states 
        private GameState gameState; //variable to store current state

        //reset ball method
        //draws ball to centre of screen and serves at random angle
        private void ResetBall()
        {
            ball = new Rectangle(doubleBuffer.Width / 2 - 6, doubleBuffer.Height / 2 - 6, 12, 12); //set ball size and positions balls origin in center of screen 
            ballSpeed = new Point(lastHit ? rand.Next(2, 7) : -rand.Next(2, 7), //set balls x value based off lastHit bool and random number
                                   rand.Next() > int.MaxValue / 2 ? rand.Next(2, 7) : -rand.Next(2, 7)); //set balls y value by 50/50 chance and random number  
        }

        //move ball method
        //adds direction/speed to balls x + y, checks if ball bounces off screen edge or returns a score.
        private int MoveBall(bool bounceOffSides)
        {
            ball.X += ballSpeed.X;
            ball.Y += ballSpeed.Y;

            //collision top
            if (ball.Y < 0)
            {
                ball.Y = -ball.Y; //ball position
                ballSpeed.Y = -ballSpeed.Y; //ball speed
            }

            //collision bottom
            if (ball.Y + ball.Height > doubleBuffer.Height)
            {
                ball.Y = doubleBuffer.Height - ball.Height - (ball.Y + ball.Height - doubleBuffer.Height);
                ballSpeed.Y = -ballSpeed.Y;
            }

            //collision left
            if (ball.X < 0)
            {
                if (bounceOffSides)
                {
                    ball.X = 0;
                    ballSpeed.X = -ballSpeed.X;
                }
                else return -1; //score point for right
            }

            //collison right
            if (ball.X + ball.Width > doubleBuffer.Width)
            {
                if (bounceOffSides)
                {
                    ball.X = doubleBuffer.Width - ball.Width;
                    ballSpeed.X = -ballSpeed.X;
                }
                else return 1; //score point for left
            }
            return 0;
        }

        #region Game1 Constructor
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.TargetElapsedTime = new TimeSpan(333333); //set 30fps
            Window.AllowUserResizing = false;

            gameState = GameState.Idle; //begin game in idle state

            rand = new Random(); // create new Random object
        }
        #endregion

        #region Initialize method
        protected override void Initialize() // TODO: Add your initialization logic here
        {
            doubleBuffer = new RenderTarget2D(GraphicsDevice, 640, 480); //set render surface resolution

            //set window size
            int windowWidth = 960;
            int windowHeight = 720;

            graphics.PreferredBackBufferWidth = windowWidth;
            graphics.PreferredBackBufferHeight = windowHeight;

            renderRectangle = new Rectangle(0, 0, windowWidth, windowHeight); //set area to display render surface within window

            graphics.IsFullScreen = false;

            graphics.ApplyChanges();

            ResetBall();

            base.Initialize();
        }
        #endregion

        #region LoadContent method
        protected override void LoadContent() // TODO: use this.Content to load your game content here
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            texture = new Texture2D(GraphicsDevice, 1, 1); //create 1x1 texture object 
            Color[] data = new Color[1];
            data[0] = Color.White;
            texture.SetData(data);
        }
        #endregion

        #region Update method
        protected override void Update(GameTime gameTime) // TODO: Add your update logic here
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            switch (gameState)
            {
                case GameState.Idle:
                    MoveBall(bounceOffSides: true);
                    //gameState = GameState.Start;
                    break;

                case GameState.Start:
                    ResetBall();
                    gameState = GameState.Play;
                    break;

                case GameState.Play:
                    int scored = MoveBall(bounceOffSides: false); //store returned value to check who scored 

                    if (scored == 1) //left side scored
                    {
                        lastHit = true;
                        gameState = GameState.CheckEnd;
                    }

                    if (scored == -1) //right side scored
                    {
                        lastHit = false;
                        gameState = GameState.CheckEnd;
                    }
                    break;

                case GameState.CheckEnd:
                    ResetBall();
                    gameState = GameState.Play;
                    break;

                default:
                    gameState = GameState.Idle;
                    break;
            }

            base.Update(gameTime);
        }
        #endregion

        #region Draw method
        protected override void Draw(GameTime gameTime) // TODO: Add   draw logic here
        {
            GraphicsDevice.SetRenderTarget(doubleBuffer); //tell graphics device to draw to render surface
            GraphicsDevice.Clear(Color.Black); //draw black background

            //draw game objects relevant to current state
            spriteBatch.Begin();
            switch (gameState)
            {
                case GameState.Idle:
                    spriteBatch.Draw(texture, ball, Color.White); //draw ball using white pixel texture
                    break;

                case GameState.Start:
                    break;

                case GameState.Play:
                case GameState.CheckEnd:
                    spriteBatch.Draw(texture, ball, Color.White); //draw ball using white pixel texture
                    break;
            }
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null); //stop drawing to render surface to allow it to be drawn to the screen

            spriteBatch.Begin();
            spriteBatch.Draw(doubleBuffer, renderRectangle, Color.White); //draw render surface to screen
            spriteBatch.End();

            base.Draw(gameTime);
        }
        #endregion
    }
}

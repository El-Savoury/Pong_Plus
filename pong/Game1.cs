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
        private RenderTarget2D renderSurface; //initialise render surface to draw game to
        private Rectangle renderRectangle; //area within window where the game is rendered to 
        private Texture2D pixel;

        //ball variables
        private Texture2D ballTexture; //used to draw ball
        private Rectangle ball; //defines position, size and bounding box of ball
        private Point ballSpeed; //holds x and y int values to define ball velocity 
        private readonly Random rand; //inits random object to allow random ball direction

        private Vector2 ballLeft, ballRight;

        //paddle variables
        private Texture2D paddleTexture; //used to draw paddle
        private Rectangle[] paddle; //creates array to hold info for both paddles
        private bool lastHit = true; //which side hit/scored last (left = true right = false)

        private Vector2 paddleLeft, paddleRight;

        public enum GameState { Idle, Start, Play, CheckEnd } //intialise enumeration type to define states 
        private GameState gameState; //variable to store current state

        //reset ball method
        //draws ball to centre of screen and serves at random angle
        private void ResetBall()
        {
            ball = new Rectangle(renderSurface.Width / 2 - 4, renderSurface.Height / 2 - 4, 8, 8); //set ball size and positions balls origin in center of screen 
            ballSpeed = new Point(lastHit ? rand.Next(2, 5) : -rand.Next(2, 5), //set balls x value based off lastHit bool and random number
                                   rand.Next() > int.MaxValue / 2 ? rand.Next(2, 5) : -rand.Next(2, 5)); //set balls y value by 50/50 chance and random number  
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
            if (ball.Y + ball.Height > renderSurface.Height)
            {
                ball.Y = renderSurface.Height - ball.Height - (ball.Y + ball.Height - renderSurface.Height);
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
            if (ball.X + ball.Width > renderSurface.Width)
            {
                if (bounceOffSides)
                {
                    ball.X = renderSurface.Width - ball.Width;
                    ballSpeed.X = -ballSpeed.X;
                }
                else return 1; //score point for left
            }
            return 0;
        }

        //paddle ai method
        //index defines which paddle to move
        private void PaddleAi(int index)
        {
            int ballCentre = ball.Y + (ball.Height / 2); //current y position of balls centre
            int paddleCentre = paddle[index].Y + (paddle[index].Height / 2); //current y position of paddles centre

            int yPosDistance = ballCentre - paddleCentre; //how far along y axis centre of paddle is from centre of ball 
            paddle[index].Y += yPosDistance; //add difference in distance to paddles postion to track ball
        }

        //paddle collision method
        //returns true if any axis of ball alligns/overlaps with paddle axis
        //takes anticipated x and y values of ball as arguments
        private bool PaddleCollision(int index, int ballX, int ballY)
        {
            return ballX <= paddle[index].X + paddle[index].Width &&    //balls left side is overlapping paddles right side
                   ballX + ball.Width >= paddle[index].X &&             //balls right side is overlapping left side
                   ballY <= paddle[index].Y + paddle[index].Height &&   //balls top is overlapping paddles bottom edge
                   ballY + ball.Height >= paddle[index].Y;              //balls bottom is overlapping paddles top edge
        }

        //ball collision line method
        //check if balls current position is behind paddle the check its previous positon to see if ball should have collided
        private bool BallCollisionLine()
        {
            float xPosDistance = 0; //distance between ball and paddle on x axis
            int paddleHit = 0; //which paddle is hit

            if (ballSpeed.X > 0 && (ball.X + ball.Width) > paddle[1].X) //if ball is moving right and balls right xpos is greater than right paddles left edge xpos
            {
                xPosDistance = (ball.X + ball.Width) - paddle[1].X; //distance = ball right edge Xpos - paddle left edge Xpos
                if (xPosDistance > (ballSpeed.X + ball.Width)) { return false; } //if ball is greater distance behind paddle than balls width and speed then it has already passed paddle in earlier frame so has not collided  
                else { paddleHit = 1; } //else right paddle may have been hit
            }
            else if (ballSpeed.X < 0 && ball.X < (paddle[0].X + paddle[0].Width)) //if ball is moving left and balls left xpos is greater than left paddles right edge
            {
                xPosDistance = ball.X - (paddle[0].X + paddle[0].Width); //distance = ball left edge Xpos - paddle right edge Xpos
                if (xPosDistance < ballSpeed.X) { return false; } //if distance in next frame is already greater than the amount the ball will move then no collision occured 
                else { paddleHit = 0; } //else left paddle may have been hit
            }
            else return false;

            //get balls previous position at the time it might have collided
            float deltaTime = xPosDistance / ballSpeed.X; //calculate % of velocity that ball has moved past paddle
            int collisionX = Convert.ToInt32(ball.X - (ballSpeed.X * deltaTime)); //remove percentage from ball speed and subtract from currrent pos to give collison pos
            int collisionY = Convert.ToInt32(ball.Y - (ballSpeed.Y * deltaTime));

            //check for collision
            if (PaddleCollision(xPosDistance < 0 ? 0 : 1, collisionX, collisionY))
            {
                ball.X = collisionX;
                ball.Y = collisionY;

                //reverse ball velocity and increase by 1
                ballSpeed.X = -(ballSpeed.X + Math.Sign(ballSpeed.X)); //sign returns 1 if ballspeed is positive and - 1 if negative so increasing speed by 1 in balls new X direction

                //determine balls new Y direction based on where it hit paddle
                int diffY = (collisionY + ball.Height / 2 - (paddle[paddleHit].Y - paddle[paddleHit].Height / 2)); //distance between balls centre and paddles centre at time of collison

                diffY /= paddle[paddleHit].Height / 8; //divide distance by 1/8 paddles height to get value between +4 and -4

                diffY -= Math.Sign(diffY); //minus 1 in whichever new direction to lower rebound angle 

                ballSpeed.Y += diffY;
                return true;
            }
            return false;
        }

        #region Game1 constructor
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.TargetElapsedTime = new TimeSpan(333333); //set 30fps
            Window.AllowUserResizing = false;

            gameState = GameState.Idle; //begin game in idle state

            rand = new Random(); //create new Random object
        }
        #endregion

        #region Initialize method
        protected override void Initialize() // TODO: Add your initialization logic here
        {
            renderSurface = new RenderTarget2D(GraphicsDevice, 640, 480); //set render surface resolution

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

            //create 1x1 pixel texture
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData<Color>(new Color[] { Color.White }); //fill array with white pixels

            ballTexture = Content.Load<Texture2D>("pongball"); //loads sprite from content folder
            paddleTexture = Content.Load<Texture2D>("pongpaddle");
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
                    gameState = GameState.Start;
                    break;

                case GameState.Start:
                    ResetBall();
                    paddle = new Rectangle[] { new Rectangle(32, renderSurface.Height/2 -16, 8, 32),
                                               new Rectangle(renderSurface.Width - 40, renderSurface.Height/2 -16, 8, 32) };

                    //init collision vectors
                    paddleLeft = new Vector2(); //left side of right paddle
                    paddleRight = new Vector2();//right side of left paddle

                    gameState = GameState.Play;
                    break;

                case GameState.Play:
                    int scored = MoveBall(bounceOffSides: false); //store returned value to check who scored 
                    
                    //add both paddles and check for collisions
                    PaddleAi(0);
                    PaddleAi(1);
                    BallCollisionLine();

                    //update collision lines
                    paddleLeft.X = paddle[1].X;
                    paddleLeft.Y = paddle[1].Y;

                    paddleRight.X = paddle[0].X + paddle[0].Width;
                    paddleRight.Y = paddle[0].Y; 

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
            GraphicsDevice.SetRenderTarget(renderSurface); //tell graphics device to draw to render surface
            GraphicsDevice.Clear(Color.Black); //draw black background

            //draw game objects relevant to current state
            spriteBatch.Begin();
            switch (gameState)
            {
                case GameState.Idle:
                    spriteBatch.Draw(ballTexture, ball, Color.White); //draw ball in idle state
                    break;

                case GameState.Start:
                    break;

                case GameState.Play:
                case GameState.CheckEnd:
                    spriteBatch.Draw(ballTexture, ball, Color.White); //draw ball
                    spriteBatch.Draw(paddleTexture, paddle[0], Color.White); //draw left paddle
                    spriteBatch.Draw(paddleTexture, paddle[1], Color.White); //draw right paddle
                    spriteBatch.Draw(pixel, paddleLeft, Color.Red); //draw collision vector on right paddle left edge
                    spriteBatch.Draw(pixel, paddleRight, Color.Red); //draw collision vector on left paddle right edge
                    break;
            }
            spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null); //stop drawing to render surface to allow it to be drawn to the screen

            spriteBatch.Begin();
            spriteBatch.Draw(renderSurface, renderRectangle, Color.White); //draw render surface to screen
            spriteBatch.End();

            base.Draw(gameTime);
        }
        #endregion
    }
}

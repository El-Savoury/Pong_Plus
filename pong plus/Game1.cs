using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace pong_plus
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Render surface vars
        private RenderTarget2D renderSurface;    // Initialise render surface to draw game to
        private Rectangle renderRectangle;       // Area within window where the game is rendered to 
        private Texture2D pixel;
        private Rectangle courtBorder;
        private gamescreen gameScreen;

        // Define game states
        public enum GameState { Idle, Start, Play, CheckEnd };
        private GameState gameState; // Var to store current state

        // Ball vars
        private pongball ball;         // Defines position, size and bounding box of ball
        private Point ballVelocity;     // Holds x and y int values to define ball velocity 
        private readonly Random rand;   // Inits random object to allow random ball direction

        // Paddle vars
        private Rectangle[] paddle;     // Creates array to hold info for both paddles
        private bool lastHit = true;    // Which side hit/scored last (left = true right = false)

        // RGB colours
        private Color white;
        private Color blue;
        private Color red;
        private Color green;

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

        //// Reset Ball - Draws ball to centre of screen and serves at random angle
        //private void ResetBall()
        //{
        //    ball = new Rectangle(renderSurface.Width / 2 - 4, renderSurface.Height / 2 - 4, 8, 8);          // Set ball size and positions balls origin in centre of screen 
        //    ballVelocity = new Point(lastHit ? rand.Next(2, 7) : -rand.Next(2, 7),                          // Set balls x value based off lastHit bool and random number
        //                           rand.Next() > int.MaxValue / 2 ? rand.Next(2, 7) : -rand.Next(2, 7));    // Set balls y value by 50/50 chance and random number between 2 & 7 
        //}

        //// Move ball / ball collisions
        //private int MoveBall(bool bounce)
        //{
        //    ball.X += ballVelocity.X;
        //    ball.Y += ballVelocity.Y;

        //    // Collision top
        //    if (ball.Y < courtBorder.Y)
        //    {
        //        ball.Y = courtBorder.Y + Math.Sign(ball.Y); // Ball position
        //        ballVelocity.Y = -ballVelocity.Y; // Ball speed
        //    }

        //    // Collision bottom
        //    if (ball.Y + ball.Height > courtBorder.Y + courtBorder.Height)
        //    {
        //        ball.Y = (courtBorder.Y + courtBorder.Height) - ball.Height - (ball.Y + ball.Height - (courtBorder.Y + courtBorder.Height));
        //        ballVelocity.Y = -ballVelocity.Y;
        //    }

        //    // Collision left
        //    if (ball.X < courtBorder.X)
        //    {
        //        if (bounce)
        //        {
        //            ball.X = courtBorder.X;
        //            ballVelocity.X = -ballVelocity.X;
        //        }
        //        else return -1; // Score point for right
        //    }

        //    // Collison right
        //    if (ball.X + ball.Width > courtBorder.X + courtBorder.Width)
        //    {
        //        if (bounce)
        //        {
        //            ball.X = (courtBorder.X + courtBorder.Width) - ball.Width;
        //            ballVelocity.X = -ballVelocity.X;
        //        }
        //        else return 1; // Score point for left
        //    }
        //    return 0;
        //}

        //// Paddle AI
        //private void PaddleAi(int index)
        //{
        //    int ballCentre = ball.Y + (ball.Height / 2); // Current y position of balls centre
        //    int paddleCentre = paddle[index].Y + (paddle[index].Height / 2); // Current y position of paddles centre

        //    int yPosDiff = ballCentre - paddleCentre; // How far along y axis centre of paddle is from centre of ball 
        //    paddle[index].Y += yPosDiff; // Add difference in distance to paddles postion to track ball

        //    // Keep paddles within border
        //    if (paddle[index].Y < courtBorder.Y) { paddle[index].Y = courtBorder.Y; }
        //    if (paddle[index].Y + paddle[index].Height > courtBorder.Y + courtBorder.Height) { paddle[index].Y = courtBorder.Y + courtBorder.Height - paddle[index].Height; }
        //}

        //// Paddle & ball AABB collison check
        //private bool PaddleCollision(int index, int ballX, int ballY)
        //{
        //    return ballX <= paddle[index].X + paddle[index].Width &&    //ball left overlapping paddle right 
        //           ballX + ball.Width >= paddle[index].X &&             //ball right overlapping paddle left
        //           ballY <= paddle[index].Y + paddle[index].Height &&   //ball top overlapping paddle bottom
        //           ballY + ball.Height >= paddle[index].Y;              //ball bottom overlapping paddle top
        //}

        //// 
        //private bool BallCollisionLine()
        //{
        //    float xPosDist = 0;  // Distance between ball and paddle on x axis
        //    int padHit = 0;      // Which paddle is hit

        //    // Check balls current xpos is further behind paddle than balls movement amount in previous frame 
        //    if (ballVelocity.X > 0 && ball.X + ball.Width > paddle[1].X) // Ball right edge is beyond paddle left edge
        //    {
        //        xPosDist = ball.X + ball.Width - paddle[1].X;
        //        if (xPosDist > ballVelocity.X + ball.Width) { return false; } // Ball is not alligned with paddle on x axis in previous frame
        //        else { padHit = 1; } // Right paddle may be hit
        //    }
        //    else if (ballVelocity.X < 0 && ball.X < paddle[0].X + paddle[0].Width) // Ball left edge is beyond paddle right edge
        //    {
        //        xPosDist = ball.X - (paddle[0].X + paddle[0].Width);
        //        if (xPosDist < ballVelocity.X) { return false; }
        //        else { padHit = 0; } // Left paddle may be hit
        //    }
        //    else return false;

        //    // Get balls previous position at the time it might have collided
        //    float deltaTime = xPosDist / ballVelocity.X; // Calculate % of velocity that ball has moved past paddle
        //    int collisionX = (int)(ball.X - (ballVelocity.X * deltaTime)); // Remove percentage from ball speed and subtract from currrent pos to give collison pos
        //    int collisionY = (int)(ball.Y - (ballVelocity.Y * deltaTime));

        //    // Check for collision
        //    if (PaddleCollision(xPosDist < 0 ? 0 : 1, collisionX, collisionY))
        //    {
        //        ball.X = collisionX;
        //        ball.Y = collisionY;

        //        ballVelocity.X = -(ballVelocity.X + Math.Sign(ballVelocity.X)); // Invert ball x velocity and increase by 1

        //        // Determine balls new Y direction based on where it hit paddle
        //        int diffY = (collisionY + ball.Height / 2) - (paddle[padHit].Y + paddle[padHit].Height / 2); // Get distance of ball collision from centre of paddle

        //        diffY /= paddle[padHit].Height / 8; // Divide distance by 1/8 paddle height gives value between +4 and -4
        //        diffY -= Math.Sign(diffY); // Minus 1 in whichever new direction to lower rebound angle

        //        ballVelocity.Y += diffY;
        //        return true;
        //    }
        //    return false;
        //}

        public Game1() // Game1 class constructor
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.TargetElapsedTime = new TimeSpan(333333); // Set 30fps
            Window.AllowUserResizing = true;

            gameState = GameState.Idle; // Start game in idle state

            rand = new Random(); // Init random number generator object
        }

        protected override void Initialize() // TODO: Add your initialization logic here
        {
            renderSurface = new RenderTarget2D(GraphicsDevice, 800, 600); // Set render surface resolution
            gameScreen = new gamescreen();
            courtBorder = gamescreen.border;

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
            //ResetBall(); // Start ball in middle of screen

            base.Initialize();
        }

        protected override void LoadContent() // TODO: use this.Content to load your game content here
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Create 1x1 white pixel texture
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new Color[] { Color.White });

            // Define custom colours
            white = new Color(199, 198, 198);
            blue = new Color(5, 46, 112);
            red = new Color(173, 43, 34);
            green = new Color(39, 150, 43);
        }

        protected override void Update(GameTime gameTime) // TODO: Add your update logic here
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            switch (gameState)
            {
                case GameState.Idle:
                    ball.MoveBall(bounce: true);
                    //gameState = GameState.Start;
                    break;

                case GameState.Start:
                    ball = new pongball(rand, lastHit); // Create new ball object at start of each point.
                    //ResetBall();

                    // Init paddles
                    paddle = new Rectangle[] { new Rectangle(courtBorder.X + 32, ((courtBorder.Y + courtBorder.Height)/2) -16, 8, 32),
                                               new Rectangle(courtBorder.X + courtBorder.Width - 40, renderSurface.Height/2 -16, 8, 32) };

                    gameState = GameState.Play;
                    break;

                case GameState.Play:
                    int sideScored = ball.MoveBall(bounce: false); // Store returned 1 or -1 value to check which side scored 

                    // Add both paddles and check for collisions
                    //PaddleAi(0);
                    //PaddleAi(1);
                    //BallCollisionLine();

                    if (sideScored == 1) // Left side scored
                    {
                        lastHit = true;
                        gameState = GameState.CheckEnd;
                    }

                    if (sideScored == -1) // Right side scored
                    {
                        lastHit = false;
                        gameState = GameState.CheckEnd;
                    }
                    break;

                case GameState.CheckEnd:
                    //ResetBall();
                    gameState = GameState.Play;
                    break;

                default:
                    gameState = GameState.Idle;
                    break;
            }

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
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    break;

                case GameState.Start:
                    break;

                case GameState.Play:

                case GameState.CheckEnd:
                    spriteBatch.Draw(pixel, ball.BallBounds, white);
                    spriteBatch.Draw(pixel, paddle[0], red);
                    spriteBatch.Draw(pixel, paddle[1], green);
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

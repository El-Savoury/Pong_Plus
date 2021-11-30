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

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            this.TargetElapsedTime = new TimeSpan(333333); //set to 30fps
            Window.AllowUserResizing = false;
        }

        protected override void Initialize()
        {
            doubleBuffer = new RenderTarget2D(GraphicsDevice, 640, 480); //set render surface resolution

            //set window size
            int windowWidth = 960;
            int windowHeight = 720;

            graphics.PreferredBackBufferWidth = windowWidth;
            graphics.PreferredBackBufferHeight = windowHeight;

            renderRectangle = new Rectangle(0, 0, windowWidth, windowHeight);

            graphics.IsFullScreen = false;

            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(doubleBuffer); //tell graphics device to draw to render surface
            GraphicsDevice.Clear(Color.Black); //black background
            GraphicsDevice.SetRenderTarget(null); //stop drawing to render surface to allow it to be drawn to the screen

            spriteBatch.Begin();
            spriteBatch.Draw(doubleBuffer, renderRectangle, Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

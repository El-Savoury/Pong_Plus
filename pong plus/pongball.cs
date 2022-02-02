using System;
using Microsoft.Xna.Framework;

namespace pong_plus
{
    public class PongBall
    {
        public int size;
        public int minSpd;
        public int maxSpd;

        // Ball Colour
        public Color BallColour { get; set; }

        // Bounding box
        public Rectangle BallBounds { get; set; }

        // Ball velocity
        public Point Velocity { get; set; }

        // Ball Constructor
        public PongBall(Random rand, bool direction)
        {
            size = 8;
            minSpd = 2;
            maxSpd = 5;
            BallColour = Game1.white;

            InitBall(rand, direction, 400, 300);
        }

        // Powerup constructor
        public PongBall(Random rand, bool direction, int s, int minspeed, int maxspeed)
        {
            size = s;
            minSpd = minspeed;
            maxSpd = maxspeed;

            InitBall(rand, direction, rand.Next(GameScreen.border.X + 200, GameScreen.border.Right - 200),
                                      rand.Next(GameScreen.border.Y + 100, GameScreen.border.Bottom - 100));
        }

        // Projectile constructor
        public PongBall(int x, int y, int size, int velX, int velY)
        {
            BallBounds = new Rectangle(x, y, size, size);
            Velocity = new Point(velX, velY);
        }

        // Init ball
        private void InitBall(Random rand, bool direction, int spawnX, int spawnY)
        {
            BallBounds = new Rectangle(spawnX, spawnY, size, size);
            Velocity = new Point(direction ? rand.Next(minSpd, maxSpd) : -rand.Next(minSpd, maxSpd),              // Sets balls x velocity based on which side scored last
                       rand.Next() > int.MaxValue / 2 ? rand.Next(minSpd, maxSpd) : -rand.Next(minSpd, maxSpd));  // Sets y velocity by 50:50 chance 
        }

        // Reposition ball
        public void RepositionBall(Point point)
        {
            BallBounds = new Rectangle(point, BallBounds.Size); // Allows balls position to be updated for correct collisions
        }

        // Increase velocity
        const int maxVelocity = 64;
        public void IncreaseVelocity(int x, int y)
        {
            Point vel = Velocity;

            // Add speed
            vel.X += x;
            vel.Y += y;

            //Cap speed
            vel.X = Math.Clamp(vel.X, -maxVelocity, maxVelocity);
            vel.Y = Math.Clamp(vel.Y, -maxVelocity, maxVelocity);

            Velocity = vel;
        }

        // Reverse Velocity
        public void ReverseVelocity(bool x = false, bool y = false) // Call either param as true to invert its velocity
        {
            Point vel = Velocity;

            if (x) vel.X = -vel.X;
            if (y) vel.Y = -vel.Y;

            Velocity = vel;
        }

        // Move ball / ball collisions
        public (int, bool) MoveBall(bool bounce)
        {
            int scored = 0;
            bool bounced = false; // Returned when ball bounces for scoring and sound

            Rectangle border = GameScreen.border;
            Point ballPos = BallBounds.Location;

            ballPos.X += Velocity.X;
            ballPos.Y += Velocity.Y;

            // Collision top
            if (ballPos.Y < border.Y)
            {
                bounced = true;
                ballPos.Y = border.Y + Math.Sign(ballPos.Y); // Ball position
                ReverseVelocity(y: true);
            }

            // Collision bottom
            if (ballPos.Y + BallBounds.Height > border.Bottom)
            {
                bounced = true;
                ballPos.Y = (border.Bottom - BallBounds.Height) - (ballPos.Y + BallBounds.Height - border.Bottom);
                ReverseVelocity(y: true);
            }

            // Collision left
            if (ballPos.X < border.X)
            {
                if (bounce) // Idle state
                {
                    bounced = true;
                    ballPos.X = border.X;
                    ReverseVelocity(x: true);
                }
                else scored = -1; // Score point for right
            }

            // Collison right
            if (ballPos.X + BallBounds.Width > border.Right)
            {
                if (bounce) // Idle state
                {
                    bounced = true;
                    ballPos.X = border.Right - BallBounds.Width;
                    ReverseVelocity(x: true);
                }
                else scored = 1; // Score point for left
            }

            RepositionBall(ballPos);
            return (scored, bounced); // Return which side scored or if the ball bounced
        }
    }
}
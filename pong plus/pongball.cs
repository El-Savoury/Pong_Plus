using System;
using Microsoft.Xna.Framework;

namespace pong_plus
{
    public class pongball
    {
        // Bounding box
        public Rectangle BallBounds
        {
            get; private set;
        }

        // Ball velocity
        public Point Velocity
        {
            get; private set;
        }

        // Constructor
        public pongball(Random rand, bool direction)
        {
            BallBounds = new Rectangle(800 / 2 - 4, 600 / 2 - 4, 8, 8);
            Velocity = new Point(direction ? 3 : -3, rand.Next() > int.MaxValue / 2 ? 3 : -3); // Sets balls x & y speed based on which side scored last 
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

            if (Math.Abs(Velocity.X) < maxVelocity) { vel.X += x; }
            if (Math.Abs(Velocity.Y) < maxVelocity) { vel.Y += y; }

            Velocity = vel;
        }

        // Invert Velocity
        public void InvertVelocity(bool x = false, bool y = false) // Call either param as true to invert its velocity
        {
            var vel = Velocity;

            if (x) vel.X = -vel.X;
            if (y) vel.Y = -vel.Y;

            Velocity = vel;
        }

        // Move ball / ball collisions
        public int MoveBall(bool bounce)
        {
            Rectangle border = gamescreen.border;
            Point ballPos = BallBounds.Location;

            ballPos.X += Velocity.X;
            ballPos.Y += Velocity.Y;

            // Collision top
            if (ballPos.Y < border.Y)
            {
                ballPos.Y = border.Y + Math.Sign(ballPos.Y); // Ball position
                InvertVelocity(y: true);
            }

            // Collision bottom
            if (ballPos.Y + BallBounds.Height > border.Bottom)
            {
                ballPos.Y = (border.Bottom - BallBounds.Height) - (ballPos.Y + BallBounds.Height - border.Bottom);
                InvertVelocity(y: true);
            }

            // Collision left
            if (ballPos.X < border.X)
            {
                if (bounce) // Idle state
                {
                    ballPos.X = border.X;
                    InvertVelocity(x: true);
                }
                else return -1; // Score point for right
            }

            // Collison right
            if (ballPos.X + BallBounds.Width > border.Right)
            {
                if (bounce) // Idle state
                {
                    ballPos.X = border.Right - BallBounds.Width;
                    InvertVelocity(x: true);
                }
                else return 1; // Score point for left
            }

            RepositionBall(ballPos);
            return 0;
        }

    }
}

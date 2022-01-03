using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace pong_plus
{
    public class gamescreen
    {
        // Game screen size
        public static Rectangle border = new Rectangle(80, 60, 640, 480);

        // Constructor
        public gamescreen()
        {

        }

        // Draw pong court borders
        public void drawBorder(SpriteBatch spritebatch, Texture2D texture, Color colour)
        {
            int space = 8;
            int width = 8;
            int height;

            // Draw top & bottom line
            spritebatch.Draw(texture, new Rectangle(border.X - 8, border.Y - 8, border.Width + 16, width), colour);
            spritebatch.Draw(texture, new Rectangle(border.X - 8, border.Bottom, border.Width + 16, width), colour);

            // Draw dotted sides
            for (int i = 0; i < 22; i++)
            {
                height = 8;
                spritebatch.Draw(texture, new Rectangle(border.X - 8, (border.Y + 72) + (i * (height + space)), width, height), colour);
                spritebatch.Draw(texture, new Rectangle(border.Right, (border.Y + 64) + (i * (height + space)), width, height), colour);
            }

            // Draw equal rectangles
            for (int j = 0; j < 2; j++)
            {
                height = 24;
                spritebatch.Draw(texture, new Rectangle(border.X - 8, (border.Y + 8) + (j * (height + space)), width, height), colour);
                spritebatch.Draw(texture, new Rectangle(border.Right, (border.Bottom - 64) + (j * (height + space)), width, 24), colour);
            }

            // Draw unequal rectangles
            for (int k = 0; k < 2; k++)
            {
                height = k < 1 ? 24 : 16;
                spritebatch.Draw(texture, new Rectangle(border.Right, (border.Y + 8) + k * (height + 16) , width, height), colour);

                height = k > 0 ? 24 : 16;
                spritebatch.Draw(texture, new Rectangle(border.X - 8, (border.Bottom - 64) + (k * height + space), width, height), colour);
            }
        }

        // Draw dotted centre line
        public void drawNet(SpriteBatch spritebatch, Texture2D texture, Color colour)
        {
            int width = 8;
            int height = 36;
            int indent = border.Y + 24;
            int space = 20;

            for (int i = 0; i < 8; i++)
            {
                spritebatch.Draw(texture, new Rectangle(border.X + border.Width / 2, indent + (i * (height + space)), width, height), colour);
            }
        }
    }
}

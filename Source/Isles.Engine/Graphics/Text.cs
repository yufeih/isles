using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    /// <summary>
    /// Provide sprite text drawing functionality
    /// </summary>
    public static class Text
    {
        struct StringValue
        {
            public string Text;
            public float Size;
            public Vector2 Position;
            public Color Color;
        }

        static BaseGame game;
        static SpriteFont font;
        static SpriteBatch sprite;
        static BasicEffect basicEffect;
        static List<StringValue> values = new List<StringValue>();

        /// <summary>
        /// Get sprite font
        /// </summary>
        public static SpriteFont Font
        {
            get { return font; }
        }

        /// <summary>
        /// Gets or Sets sprite batch used to drawing the text
        /// </summary>
        public static SpriteBatch Sprite
        {
            get { return sprite; }
        }

        /// <summary>
        /// Initialize text system
        /// </summary>
        /// <param name="game"></param>
        public static void Initialize(BaseGame setGame)
        {
            game = setGame;
            font = game.Content.Load<SpriteFont>(Settings.DefaultFontFile);
            sprite = new SpriteBatch(game.GraphicsDevice);
            basicEffect = new BasicEffect(game.GraphicsDevice, null);

            Log.Write("Text Initialized...");
        }

        /// <summary>
        /// Default text drawing function
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        public static void DrawString(string text, float size, Vector3 position, Color color)
        {
            Vector3 v = game.GraphicsDevice.Viewport.Project(
                position, game.Projection, game.View, Matrix.Identity);

            DrawString(text, size, new Vector2(v.X, v.Y), color);
        }

        /// <summary>
        /// Default text drawing function
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        public static void DrawString(string text, float size, Vector2 position, Color color)
        {
            StringValue value = new StringValue();
            value.Text = text;
            value.Size = size;
            value.Position = position;
            value.Color = color;

            values.Add(value);
        }

        /// <summary>
        /// Draw a line strip
        /// </summary>
        public static void DrawLineStrip(Vector3[] vertices, Vector3 color)
        {
            basicEffect.View = game.View;
            basicEffect.Projection = game.Projection;
            basicEffect.DiffuseColor = color;
            basicEffect.Begin();
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                game.GraphicsDevice.DrawUserPrimitives<Vector3>(
                    PrimitiveType.LineStrip, vertices, 0, vertices.Length - 1);

                pass.End();
            }
            basicEffect.End();
        }

        /// <summary>
        /// Draw a line list
        /// </summary>
        public static void DrawLineList(Vector3[] vertices, Vector3 color)
        {
            basicEffect.View = game.View;
            basicEffect.Projection = game.Projection;
            basicEffect.DiffuseColor = color;
            basicEffect.Begin();
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Begin();

                game.GraphicsDevice.DrawUserPrimitives<Vector3>(
                    PrimitiveType.LineList, vertices, 0, vertices.Length / 2);

                pass.End();
            }
            basicEffect.End();
        }

        /// <summary>
        /// Call this at the end of the frame to draw all strings
        /// </summary>
        public static void Present()
        {
            sprite.Begin();
            foreach (StringValue value in values)
                sprite.DrawString(
                    font, value.Text, value.Position, value.Color, 0,
                    Vector2.Zero, value.Size / font.LineSpacing, SpriteEffects.None, 0);
            sprite.End();

            // Clear all string in this frame
            values.Clear();
        }
    }
}

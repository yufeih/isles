//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    /// <summary>
    /// Provide 2D graphics drawing functionalities
    /// </summary>
    public class Graphics2D
    {
        /// <summary>
        /// Limit line count since we're using a fixed vertex buffer.
        /// </summary>
        private const int MaxLineCount = 512;
        private const int MaxPrimitiveVertexCount = 1024;
        private const int MaxPrimitiveIndexCount = 2048;

        /// <summary>
        /// Entry type for string drawing
        /// </summary>
        private struct StringValue
        {
            public string Text;
            public float Size;
            public Vector2 Position;
            public Color Color;
        }

        private readonly BaseGame game;
        private readonly Effect effect;
        private readonly SpriteFont font;
        private readonly SpriteBatch sprite;
        private readonly BasicEffect basicEffect;
        private readonly List<StringValue> strings = new();
        private readonly VertexPositionColor[] lines = new VertexPositionColor[MaxLineCount];
        private readonly VertexPositionColor[] primitives = new VertexPositionColor[MaxPrimitiveVertexCount];
        private readonly ushort[] primitiveIndices = new ushort[MaxPrimitiveIndexCount];
        private int primitiveIndexCount = 0;
        private int primitiveVertexCount = 0;
        private int lineCount = 0;
        private readonly DynamicVertexBuffer vertices;
        private readonly DynamicIndexBuffer indices;

        /// <summary>
        /// Gets graphics 2D effect
        /// </summary>
        public Effect Effect => effect;

        /// <summary>
        /// Get sprite font
        /// </summary>
        public SpriteFont Font => font;

        /// <summary>
        /// Gets or Sets sprite batch used to drawing the text
        /// </summary>
        public SpriteBatch Sprite => sprite;

        /// <summary>
        /// Initialize text system
        /// </summary>
        /// <param name="game"></param>
        public Graphics2D(BaseGame setGame)
        {
            game = setGame;
            font = game.ZipContent.Load<SpriteFont>(game.Settings.DefaultFont);
            effect = game.ZipContent.Load<Effect>(game.Settings.Graphics2DEffect);
            sprite = new SpriteBatch(game.GraphicsDevice);
            basicEffect = new BasicEffect(game.GraphicsDevice, null);
            vertices = new DynamicVertexBuffer(
                game.GraphicsDevice, typeof(VertexPositionColor), MaxPrimitiveVertexCount, BufferUsage.WriteOnly);
            indices = new DynamicIndexBuffer(
                game.GraphicsDevice, typeof(ushort), MaxPrimitiveIndexCount, BufferUsage.WriteOnly);
        }

        /// <summary>
        /// Default text drawing function
        /// </summary>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        public void DrawString(string text, float size, Vector3 position, Color color)
        {
            Point v = game.Project(position);

            DrawString(text, size, new Vector2(v.X, v.Y), color);
        }

        /// <summary>
        /// Default text drawing function
        /// </summary>
        public void DrawString(string text, float size, Vector2 position, Color color)
        {
            StringValue value;
            value.Text = text;
            value.Size = size;
            value.Position = position;
            value.Color = color;

            strings.Add(value);
        }

        /// <summary>
        /// Draws a shadowed string
        /// </summary>
        public void DrawShadowedString(string text, float size, Vector2 position, Color textColor, Color shadow)
        {
            StringValue value;
            value.Text = text;
            value.Size = size;
            value.Position = position + Vector2.One;
            value.Color = shadow;

            // Background
            strings.Add(value);

            // Foreground
            value.Position = position;
            value.Color = textColor;

            strings.Add(value);
        }

        /// <summary>
        /// Draw a 2D line onto the screen
        /// </summary>
        public void DrawLine(Point start, Point end, Color color)
        {
            if (lineCount >= MaxLineCount)
            {
                //Log.Write("Warning: Line capacity exceeded...");
                return;
            }
                        
            VertexPositionColor value;

            // Transform from screen space to projection space
            value.Position = ScreenToEffect(start.X, start.Y);
            value.Color = color;

            // Add the first vertex
            lines[lineCount++] = value;

            value.Position = ScreenToEffect(end.X, end.Y);

            // Add the second vertex
            lines[lineCount++] = value;
        }

        /// <summary>
        /// Transform from screen space to graphics2D effect position space
        /// </summary>
        public Vector3 ScreenToEffect(int x, int y)
        {
            Vector3 value;

            value.X = (float)(2 * x - game.ScreenWidth) / game.ScreenWidth;
            value.Y = (float)(game.ScreenHeight - 2 * y) / game.ScreenHeight;
            value.Z = 0;

            return value;
        }

        public void DrawShadowedLine(Point start, Point end, Color color, Color shadow)
        {
            Point shadowStart, shadowEnd;

            shadowStart.X = start.X + 1;
            shadowStart.Y = start.Y + 1;
            shadowEnd.X = end.X + 1;
            shadowEnd.Y = end.Y + 1;

            DrawLine(shadowStart, shadowEnd, shadow);
            DrawLine(start, end, color);
        }

        public void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            DrawLine(game.Project(start), game.Project(end), color);
        }

        /// <summary>
        /// Draws a 2D primitive.
        /// </summary>
        /// <param name="vertices">
        /// Vertex positions are in screen space [0~ScreenWidth, 0~ScreenHeight, 0]
        /// </param>
        /// <param name="indices">
        /// Use triangle list for vertex indexing
        /// </param>
        public void DrawPrimitive(IEnumerable<VertexPositionColor> vertices, IEnumerable<ushort> indices)
        {
            var iCount = 0;
            var indexBias = primitiveVertexCount;
            VertexPositionColor value;

            // Add new vertices
            foreach (VertexPositionColor v in vertices)
            {
                if (primitiveVertexCount >= MaxPrimitiveVertexCount)
                {
                    //Log.Write("Warning: 2D primitive vertex capacity exceeded...");
                    return;
                }
                
                // Transform from screen space to projection space
                value.Position.X = (2 * v.Position.X - game.ScreenWidth) / game.ScreenWidth;
                value.Position.Y = (game.ScreenHeight - 2 * v.Position.Y) / game.ScreenHeight;
                value.Position.Z = 0;
                value.Color = v.Color;

                primitives[primitiveVertexCount++] = value;
            }

            // Add new indices
            foreach (var index in indices)
            {
                if (primitiveIndexCount >= MaxPrimitiveIndexCount)
                {
                    //Log.Write("Warning: 2D primitive index capacity exceeded...");
                    return;
                }

                // Apply index bias
                primitiveIndices[primitiveIndexCount++] = (ushort)(indexBias + index);

                iCount++;
            }

            // Make sure our vertices and indices match triangle list
            if (iCount % 3 != 0)
            {
                throw new ArgumentException("Index count must be a multiple of 3");
            }
        }

        /// <summary>
        /// Draw a rectangle
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        public void DrawRectangle(Rectangle rect, Color color)
        {
            var vertices = new VertexPositionColor[4]
            {
                new VertexPositionColor(new Vector3(rect.Left, rect.Top, 0), color),
                new VertexPositionColor(new Vector3(rect.Right, rect.Top, 0), color),
                new VertexPositionColor(new Vector3(rect.Right, rect.Bottom , 0), color),
                new VertexPositionColor(new Vector3(rect.Left, rect.Bottom, 0), color)
            };

            var indices = new ushort[6] { 0, 1, 2, 0, 2, 3 };

            // Error when drawing up to more than 2 triangles
            DrawPrimitive(vertices, indices);
        }

        /// <summary>
        /// Call this at the end of the frame to draw all strings
        /// </summary>
        public void Present()
        {
            game.GraphicsDevice.RenderState.AlphaBlendEnable = true;
            PresentPrimitives();
            PresentText();
            PresentLine();
            game.GraphicsDevice.RenderState.AlphaBlendEnable = false;
        }

        public void PresentText()
        {
            if (strings.Count <= 0)
            {
                return;
            }

            sprite.Begin();
            foreach (StringValue value in strings)
            {
                sprite.DrawString(
                    font, value.Text, value.Position, value.Color, 0,
                    Vector2.Zero, value.Size, SpriteEffects.None, 0);
            }

            sprite.End();

            // Clear all string in this frame
            strings.Clear();
        }

        public void PresentLine()
        {
            if (lineCount <= 0)
            {
                return;
            }

            game.GraphicsDevice.Vertices[0].SetSource(null, 0, 0);

            // Update line vertices
            vertices.SetData(lines, 0, lineCount);

            // Draw all lines
            game.GraphicsDevice.VertexDeclaration = new VertexDeclaration(
                game.GraphicsDevice, VertexPositionColor.VertexElements);

            game.GraphicsDevice.Vertices[0].SetSource(
                vertices, 0, VertexPositionColor.SizeInBytes);

            effect.CurrentTechnique = effect.Techniques["Graphics2D"];
            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                game.GraphicsDevice.DrawPrimitives(
                    PrimitiveType.LineList, 0, lineCount / 2);
                pass.End();
            }
            effect.End();

            // Clear all lines in this frame
            lineCount = 0;
        }

        public void PresentPrimitives()
        {
            if (primitiveIndexCount < 3)
            {
                return;
            }

            game.GraphicsDevice.Vertices[0].SetSource(null, 0, 0);

            // Update primitive vertices
            vertices.SetData(primitives, 0, primitiveVertexCount);
            indices.SetData(primitiveIndices, 0, primitiveIndexCount);

            // Draw all lines
            game.GraphicsDevice.VertexDeclaration = new VertexDeclaration(
                game.GraphicsDevice, VertexPositionColor.VertexElements);

            game.GraphicsDevice.Vertices[0].SetSource(
                vertices, 0, VertexPositionColor.SizeInBytes);
            game.GraphicsDevice.Indices = indices;

            effect.CurrentTechnique = effect.Techniques["Graphics2D"];
            effect.Begin();
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Begin();
                game.GraphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList, 0, 0, primitiveVertexCount, 0, primitiveIndexCount / 3);
                pass.End();
            }
            effect.End();

            // Clear all primitives after drawing them
            primitiveIndexCount = 0;
            primitiveVertexCount = 0;
        }

        #region String Formatting
        /// <summary>
        /// Each charactor has a different with and height. But sadly SpriteFont
        /// hides all those charactor map and cropping stuff inside, making it
        /// difficult to measure and format text ourself.
        /// Unless you have any better idea, just use a fixed charactor width and
        /// height. This would yield incorrect result.
        /// </summary>
        public const float CharactorWidthPerSizeUnit = 0.7f;
        public const float CharactorHeightPerSizeUnit = 1.3f;

        /// <summary>
        /// Format the given text to make it fit in a rectangle area.
        /// What if we could recognize and split english word?
        /// </summary>
        /// <returns>The formatted text</returns>
        /// <example>
        /// FormatString("ABCD", 20): "AB\nCD"
        /// FormatString("What is your name?", 100): "What is \nyour name?"
        /// </example>
        public static string FormatString(String text, float width, float fontSize, SpriteFont font)
        {
            width /= fontSize;

            //Charactors per line
            float offset = 0;

            //Identify whether the next word is the first word in its line
            var firstWordInLine = true;

            //Iterate words in the text
            var wi = new WordIterator(text);

            //Return value
            var rtvSB = new StringBuilder();

            //Store each word in the text
            var str = wi.NextWord();

            if (str == null)
            {
                return "";
            }

            //Arrange each word in the formatted lines
            while (str != null)
            {
                if (firstWordInLine)
                {
                    rtvSB.Append(str);
                    if (str[str.Length - 1] != '\n')
                    {
                        offset += font.MeasureString(str).X;
                        firstWordInLine = false;
                    }
                    else
                    {
                        offset = 0;
                    }
                }
                else
                {
                    if (offset + font.MeasureString(str).X > width)
                    {
                        firstWordInLine = true;
                        if (rtvSB.Length != 0 && rtvSB[rtvSB.Length - 1] != '\n')
                        {
                            rtvSB.Append('\n');
                        }
                        offset = 0;
                        continue;
                    }
                    else
                    {
                        rtvSB.Append(str);
                        if (str[str.Length - 1] == '\n')
                        {
                            offset = 0;
                        }
                        else
                        {
                            offset += font.MeasureString(str).X;
                        }
                    }
                }
                str = wi.NextWord();
            }
            return rtvSB.ToString();
        }

        /// <summary>
        /// Format the given text to make it fit in a rectangle area.
        /// Clip and append "..." at the end if the text excceed the rectangle.
        /// </summary>
        /// <example>
        /// FormatString("ABCDEFGH", 40, 50)    : "ABCD\nEFGH"
        /// FormatString("ABCDEFGHIJ", 40, 50)  : "ABCD\nE..."
        /// </example>
        public static string FormatString(String text, float width, float height, float fontSize, SpriteFont font)
        {

            width /= fontSize;

            height /= fontSize;

            float offset = 0;

            //Identify whether the next word is the first word in its line
            var firstWordInLine = true;

            //Iterate words in the text
            var wi = new WordIterator(text);

            //Return value
            var rtvSB = new StringBuilder("");

            //Store each word in the text
            var str = wi.NextWord();

            if (str == null)
            {
                return "";
            }

            //Arrange each word in the formatted lines
            while (str != null)
            {
                Vector2 currSize = font.MeasureString(rtvSB.ToString());
                if (firstWordInLine)
                {
                    rtvSB.Append(str);
                    if (str.Length != 0 && str[str.Length - 1] != '\n')
                    {
                        offset += font.MeasureString(str).X;
                        firstWordInLine = false;
                    }
                    else
                    {
                        offset = 0;
                    }
                }
                else
                {
                    if (offset + font.MeasureString(str).X > width)
                    {
                        firstWordInLine = true;
                        if (rtvSB.Length != 0 && rtvSB[rtvSB.Length - 1] != '\n')
                        {
                            rtvSB.Append('\n');
                        }
                        offset = 0;
                        continue;
                    }
                    else
                    {
                        rtvSB.Append(str);
                        if (str[str.Length - 1] == '\n')
                        {
                            offset = 0;
                            firstWordInLine = true;
                        }
                        else
                        {
                            offset += font.MeasureString(str).X;
                        }
                    }
                }
                if (font.MeasureString(rtvSB.ToString()).Y > height)
                {
                    rtvSB.Append("...");
                    break;
                }
                str = wi.NextWord();
            }
            return rtvSB.ToString();
        }

        /// <summary>
        /// Used to Iterate each word in the text. 
        /// This class is designed to help to implement FormatString.
        /// A word here is defined as a sequence of characters without '\n' and ' '
        /// or with only the last being '\n' or ' '.
        /// eg. "word \n " is combination of 3 words: "word ", "\n", " ".
        /// </summary>
        private class WordIterator
        {
            /// <summary>
            /// Hold the text to be processed
            /// </summary>
            private readonly string text;

            /// <summary>
            /// Identify the index of the first char of the next word
            /// </summary>
            private int currentIndex;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="text">initialize the text</param>
            public WordIterator(string text)
            {
                this.text = text;
                currentIndex = 0;
            }

            /// <summary>
            /// Get the next word in the text
            /// </summary>
            /// <returns>next word</returns>
            public string NextWord()
            {
                int i;
                string rtvStr;
                if (currentIndex >= text.Length)
                {
                    return null;
                }
                for (i = currentIndex; i < text.Length; i++)
                {
                    if (text[i] == ' ' || text[i] == '\n')
                    {
                        break;
                    }
                }
                if (i == text.Length)
                {
                    rtvStr = text.Substring(currentIndex);
                }
                else
                {
                    rtvStr = text.Substring(currentIndex, i - currentIndex + 1);
                }
                currentIndex = i + 1;
                return rtvStr;
            }

            /// <summary>
            /// Reset currentIndex to 0
            /// </summary>
            public void Reset()
            {
                currentIndex = 0;
            }

            /// <summary>
            /// Get or set the currentIndex
            /// </summary>
            public int CurrentIndex
            {
                get => currentIndex;
                set => currentIndex = value;
            }

        }
        #endregion
    }
}

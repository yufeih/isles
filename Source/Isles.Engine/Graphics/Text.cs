//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

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
            font = game.Content.Load<SpriteFont>(game.Settings.DefaultFont);
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
        
        /// <summary>
        /// Each charactor has a different with and height. But sadly SpriteFont
        /// hides all those charactor map and cropping stuff inside, making it
        /// difficult to measure and format text ourself.
        /// Unless you have any better idea, just use a fixed charactor width and
        /// height. This would yield incorrect result.
        /// </summary>
        public const int CharactorWidth = 10;
        public const int CharactorHeight = 25;

        /// <summary>
        /// Format the given text to make it fit in a rectangle area.
        /// What if we could recognize and split english word?
        /// </summary>
        /// <returns>The formatted text</returns>
        /// <example>
        /// FormatString("ABCD", 20): "AB\nCD"
        /// FormatString("What is your name?", 100): "What is \nyour name?"
        /// </example>
        public static string FormatString(string text, int width)
        {

            //Charactors per line
            int charNumPerLine = width / CharactorWidth;

            //Identify the line count
            int currentLine = 0;

            //Charactor nums in the currently processed line
            int charNumInCurrentLine = 0;

            //Identify whether the next word is the first word in its line
            bool firstWordInLine = true;

            //Iterate words in the text
            WordIterator wi = new WordIterator(text);

            //Return value
            StringBuilder rtvSB = new StringBuilder();

            //Store each word in the text
            string str = wi.NextWord();

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
                        charNumInCurrentLine += str.Length;
                        firstWordInLine = false;
                    }
                    else
                    {
                        currentLine++;
                        charNumInCurrentLine = 0;
                    }
                }
                else
                {
                    if ((str[str.Length - 1] != ' ' && str[str.Length - 1] != '\n'
                        && charNumInCurrentLine + str.Length > charNumPerLine) ||
                        ((str[str.Length - 1] == ' ' || str[str.Length - 1] == '\n')
                        && charNumInCurrentLine + str.Length - 1 > charNumPerLine))
                    {
                        currentLine++;
                        charNumInCurrentLine = 0;
                        firstWordInLine = true;
                        if (rtvSB[rtvSB.Length - 1] != '\n')
                        {
                            rtvSB.Append('\n');
                        }
                        continue;
                    }
                    else
                    {
                        rtvSB.Append(str);
                        if (str[str.Length - 1] != '\n')
                        {
                            charNumInCurrentLine += str.Length;
                        }
                        else
                        {
                            currentLine++;
                            charNumInCurrentLine = 0;
                            firstWordInLine = true;
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
        public static string FormatString(string text, int width, int height)
        {
            //Mux num of chars that can be held in this rectangle
            int muxLineNum = height / CharactorHeight;
            if (muxLineNum == 0)
            {
                muxLineNum = 1;
            }
            //Charactors per line
            int charNumPerLine = width / CharactorWidth;

            //Identify the line count
            int currentLine = 0;

            //Charactor nums in the currently processed line
            int charNumInCurrentLine = 0;

            //Identify whether the next word is the first word in its line
            bool firstWordInLine = true;

            //Iterate words in the text
            WordIterator wi = new WordIterator(text);

            //Return value
            StringBuilder rtvSB = new StringBuilder();

            //Store each word in the text
            string str = wi.NextWord();

            if (str == null)
            {
                return "";
            }

            //Arrange each word in the formatted lines
            while (str != null)
            {
                if (currentLine == muxLineNum - 1)//Last line
                {
                    //Append "..." to the text ending with ' ' when exceeding
                    if ((str[str.Length - 1] != ' ' && charNumInCurrentLine + str.Length + 3 > charNumPerLine) ||
                        (str[str.Length - 1] == ' ' && charNumInCurrentLine + str.Length + 2 > charNumPerLine))
                    {
                        if (rtvSB[rtvSB.Length - 1] == ' ')
                        {
                            rtvSB[rtvSB.Length - 1] = '.';
                            rtvSB.Append("..");
                        }
                        else
                        {
                            rtvSB.Append("...");
                        }
                        break;
                    }
                    //Append "..." to the text ending with '\n'
                    if (str[str.Length - 1] == '\n')
                    {
                        rtvSB.Append(str);
                        rtvSB[rtvSB.Length - 1] = ' ';
                        rtvSB.Append("...");
                        break;
                    }
                }
                if (firstWordInLine)
                {
                    rtvSB.Append(str);
                    if (str[str.Length - 1] != '\n')
                    {
                        charNumInCurrentLine += str.Length;
                        firstWordInLine = false;
                    }
                    else
                    {
                        currentLine++;
                        charNumInCurrentLine = 0;
                    }
                }
                else
                {
                    if ((str[str.Length - 1] != ' ' && str[str.Length - 1] != '\n'
                         && charNumInCurrentLine + str.Length > charNumPerLine) ||
                         ((str[str.Length - 1] == ' ' || str[str.Length - 1] == '\n')
                         && charNumInCurrentLine + str.Length - 1 > charNumPerLine))
                    {
                        currentLine++;
                        charNumInCurrentLine = 0;
                        firstWordInLine = true;
                        if (rtvSB[rtvSB.Length - 1] != '\n')
                        {
                            rtvSB.Append('\n');
                        }
                        continue;
                    }
                    else
                    {
                        rtvSB.Append(str);
                        if (str[str.Length - 1] != '\n')
                        {
                            charNumInCurrentLine += str.Length;
                        }
                        else
                        {
                            currentLine++;
                            charNumInCurrentLine = 0;
                            firstWordInLine = true;
                        }
                    }
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
        class WordIterator
        {
            /// <summary>
            /// Hold the text to be processed
            /// </summary>
            string text;

            /// <summary>
            /// Identify the index of the first char of the next word
            /// </summary>
            int currentIndex;

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
                get { return currentIndex; }
                set { currentIndex = value; }
            }

        }
    }
}

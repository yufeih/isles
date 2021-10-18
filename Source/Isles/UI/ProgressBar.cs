//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;


namespace Isles.UI
{
    public class ProgressBar : UIElement
    {
        #region Fields

        //About time control

        private double hightLightCycle = 2.5 ;

        public bool HighLightOn = true;

        public double HightLightCycle
        {
            get { return hightLightCycle; }
            set { hightLightCycle = value; }
        }

        private double staringTime;

        public double  StartingTime
        {
            get { return staringTime; }
            set { staringTime = value; }
        }


        double highLightRollingSpeed;


        /// <summary>
        /// Pixels per second
        /// </summary>
        public double HighLightRollingSpeed
        {
            get
            {
                return highLightRollingSpeed;
            }
            set
            {
                highLightRollingSpeed = value;
            }
        }

        // About length

        private int endLength = 14;

        public int EndLength
        {
            get { return endLength; }
            set { endLength = value; }
        }

        private int hightLightLength = 20;

        public int HighLightLength
        {
            get { return hightLightLength; }
            set { hightLightLength = value; }
        }


	    
        // About Textures: 5 in total including the Frame of the bar

        Rectangle sourceRectangleLeftEnd;

        public Rectangle SourceRectangleLeftEnd
        {
            get { return sourceRectangleLeftEnd; }
            set { sourceRectangleLeftEnd = value; }
        }

        Rectangle sourceRectangleRightEnd;

        public Rectangle SourceRectangleRightEnd
        {
            get { return sourceRectangleRightEnd; }
            set { sourceRectangleRightEnd = value; }
        }

        Rectangle sourceRectangleHighLight;

        public Rectangle SourceRectangleHightLight
        {
            get { return sourceRectangleHighLight; }
            set { sourceRectangleHighLight = value; }
        }

        Rectangle sourceRectangleFiller;

        public Rectangle SourceRectangleFiller
        {
            get { return sourceRectangleFiller; }
            set { sourceRectangleFiller = value; }
        }

        int persentage = 0;

        public int Persentage
        {
            get
            {
                return persentage;
            }
            set
            {
                if (value > 100)
                    value = 100;
                if (value < 0)
                    value = 0;
                persentage = value;
                fillingRectangleDirty = true;
            }
        }

        bool fillingRectangleDirty = true;

        Rectangle fillingRectangle;

        public Rectangle FillingRectangle
        {
            get
            {
                if (fillingRectangleDirty)
                {
                    fillingRectangleDirty = false;
                    fillingRectangle = new Rectangle(DestinationRectangle.X, DestinationRectangle.Y,
                          DestinationRectangle.Width * persentage / 100, DestinationRectangle.Height);
                    return fillingRectangle;
                }
                else
                {
                    return fillingRectangle;
                }
            }
        }

        #endregion

        /// <summary>
        /// Set the progress
        /// </summary>
        /// <param name="persentage">integer between 0 - 100</param>
        public void SetProgress(int persentage)
        {
            Persentage = persentage;
        }


        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            // Draw the progress
            if(FillingRectangle.Width > 2 * EndLength)
            {
                sprite.Draw(Texture, new Rectangle(FillingRectangle.X, FillingRectangle.Y, 
                            EndLength, FillingRectangle.Height), this.sourceRectangleLeftEnd, Color.White);
                sprite.Draw(Texture, new Rectangle(FillingRectangle.Right - EndLength, 
                            FillingRectangle.Y, EndLength, FillingRectangle.Height),this.sourceRectangleRightEnd, Color.White);
                sprite.Draw(Texture, new Rectangle(FillingRectangle.X + EndLength, FillingRectangle.Y, 
                            FillingRectangle.Width - 2 * EndLength, FillingRectangle.Height),this.SourceRectangleFiller, Color.White);
            }
            else
            {
                sprite.Draw(Texture, new Rectangle(FillingRectangle.X, FillingRectangle.Y, FillingRectangle.Width / 2, 
                            FillingRectangle.Height), this.sourceRectangleLeftEnd, Color.White);
                sprite.Draw(Texture, new Rectangle(FillingRectangle.X + FillingRectangle.Width / 2, FillingRectangle.Y, 
                            FillingRectangle.Width / 2, FillingRectangle.Height), this.sourceRectangleRightEnd, Color.White);
            }
            /*
            // Draw the rolling highlight
            int highLightFront = (int)((gameTime.TotalGameTime.TotalSeconds - StartingTime) 
                                % HightLightCycle * HighLightRollingSpeed);

            if ( FillingRectangle.Width > HighLightLength)
            {
                if ( highLightFront - HighLightLength > FillingRectangle.Right)
                {
                    // Hight is not in the drawing region 
                    // So, do nothing 
                }
                else if(highLightFront > FillingRectangle.Right)
                {
                    // End part of the highlight should be drow
                    sprite.Draw(this.Texture, new Rectangle(highLightFront - HighLightLength, FillingRectangle.Y,
                                FillingRectangle.Right - (highLightFront - HighLightLength), FillingRectangle.Height),
                                new Rectangle(sourceRectangleHighLight.X, sourceRectangleHighLight.Y,
                                            sourceRectangleHighLight.Width * (FillingRectangle.Right - (highLightFront - HighLightLength)) / HighLightLength,
                                            sourceRectangleHighLight.Height),
                                Color.White);
                }
                else if (highLightFront - HighLightLength > FillingRectangle.X)
                {
                    // Full part of the highlight to be drow
                    sprite.Draw(Texture, new Rectangle(highLightFront - HighLightLength, FillingRectangle.Y,
                                HighLightLength, FillingRectangle.Height), sourceRectangleHighLight, Color.White);
                }
                else
                {
                    int sourceX = sourceRectangleHighLight.X + (FillingRectangle.Left - (highLightFront - HighLightLength))
                                                                / HighLightLength * sourceRectangleHighLight.Width;
                    sprite.Draw(Texture, new Rectangle(FillingRectangle.X, FillingRectangle.Y, highLightFront - FillingRectangle.Right,
                                FillingRectangle.Height),
                                new Rectangle(sourceX, sourceRectangleHighLight.Y,
                                                sourceRectangleHighLight.Right - sourceX, sourceRectangleHighLight.Height)
                                , Color.White);
                    //sprite.Draw(Texture, new Rectangle(highLightFront - HighLightLength, FillingRectangle.Y, highLightFront, FillingRectangle.Height),
                    //            SourceRectangleHightLight, Color.White);
                }
            }
            */

        }

        public override void Update(GameTime gameTime)
        {
            // throw new Exception("The method or operation is not implemented.");
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            return EventResult.Handled;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void OnEnableStateChanged()
        {
            base.OnEnableStateChanged();
        }
    }
}

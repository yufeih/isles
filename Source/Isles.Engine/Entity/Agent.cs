using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
using Isles.AI;


namespace Isles.Engine
{
    /// <summary>
    /// Base class for all game agents
    /// </summary>
    public class BaseAgent : Entity
    {
        #region Entity
        public override bool BeginDrag(Hand hand)
        {
            // Agents can't be dragged
            return false;
        }

        public override bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // Agents can't be dropped
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Draw(GameTime gameTime)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override float? Intersects(Ray ray)
        {
            throw new Exception("The method or operation is not implemented.");
        } 
        #endregion

        public BaseAgent(GameScreen screen)
            : base(screen)
        {
           
        }
    }
}

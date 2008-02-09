//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;

namespace Isles
{
    #region Tree
    /// <summary>
    /// Respresents a tree in the game
    /// </summary>
    public class Tree : Entity
    {
        #region Fields
        #endregion
        
        #region Methods
        /// <summary>
        /// Create a new tree
        /// </summary>
        public Tree(GameWorld world) : base(world)
        {
            XmlElement xml;

            if (GameDefault.Singleton.
                WorldObjectDefaults.TryGetValue(GetType().Name, out xml))
            {
                Deserialize(xml);
            }
        }

        public override void Deserialize(XmlElement xml)
        {
            // Deserialize models after default attributes are assigned
            base.Deserialize(xml);

            // Fall on the ground
            Position = new Vector3(
                Position.X, Position.Y, world.Landscape.GetHeight(Position.X, Position.Y));
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            Model.Update(gameTime);
        }

        /// <summary>
        /// Draw the tree
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            if (!VisibilityTest(BaseGame.Singleton.ViewProjection))
                return;

            Model.Transform = Transform;
            Model.Draw(gameTime, delegate(BasicEffect effect)
            {
                if (Selected)
                    effect.DiffuseColor = Vector3.UnitY;
                else if (Highlighted)
                    effect.DiffuseColor = Vector3.UnitZ;
                else
                    effect.DiffuseColor = Vector3.One;
            });
        }

        /// <summary>
        /// Draw the tree use an effect to show that the current
        /// Position is invalid
        /// </summary>
        /// <param name="gameTime"></param>
        public void DrawInvalid(GameTime gameTime)
        {
            Model.Transform = Transform;
            Model.Draw(gameTime, delegate(BasicEffect effect)
            {
                effect.DiffuseColor = Vector3.UnitX;
            });
        }

        public override bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // If dropped on a wood storage, add to our total wood amount,
            // and we're done with this wood
            Building building = entity as Building;
            if (building != null && building.StoresWood)
            {
                world.GameLogic.Wood += 100; // FIXME: Magic number

                hand.Drop();
                world.Destroy(this);
                return false;
            }

            // Otherwise, place it on the ground
            return base.BeginDrop(hand, entity, leftButton);
        }

        public override void Follow(Hand hand)
        {
            // Highlight buildings that can store wood
            Building building = world.Pick() as Building;
            if (building != null && building.StoresWood)
                world.Highlight(building);
            else
                world.Highlight(null);

            base.Follow(hand);
        }
        #endregion
    }
    #endregion
}

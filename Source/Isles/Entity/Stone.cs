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
    #region Stone
    /// <summary>
    /// Respresents a stone in the game
    /// </summary>
    public class Stone : Entity
    {
        #region Fields
        /// <summary>
        /// Stone speed
        /// </summary>
        //Vector3 speed;

        /// <summary>
        /// Force applied to the stone
        /// </summary>
        //Vector3 force;
        
        /// <summary>
        /// Torque applied to the stone
        /// </summary>
        //Vector3 torque;

        /// <summary>
        /// Radius of the bounding sphere
        /// </summary>
        //float radius;

        /// <summary>
        /// Gets or sets the value of the stone
        /// </summary>
        public int Gold;
        #endregion
        
        #region Methods
        /// <summary>
        /// Create a new stone
        /// </summary>
        public Stone(GameWorld world) : base (world)
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
            int.TryParse(xml.GetAttribute("Gold"), out Gold);

            // Deserialize models after default attributes are assigned
            base.Deserialize(xml);
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
        /// Ray intersection test
        /// </summary>
        /// <param name="ray">Target ray</param>
        /// <returns>
        /// Distance from the intersection point to the ray starting Position,
        /// Null if there's no intersection.
        /// </returns>
        public override Nullable<float> Intersects(Ray ray)
        {
            // Transform ray to object space
            Matrix worldInverse = Matrix.Invert(Model.Transform);
            Vector3 newPosition = Vector3.Transform(ray.Position, worldInverse);
            Vector3 newTarget = Vector3.Transform(ray.Position + ray.Direction, worldInverse);
            Ray newRay = new Ray(newPosition, newTarget - newPosition);

            // Perform a bounding box intersection...
            //
            // HACK HACK!!! We need a mstone accurate algorithm :)
            return newRay.Intersects(Model.BoundingBox);
        }

        public override void Follow(Hand hand)
        {
            // Highlight buildings that can store gold
            Building building = world.Pick() as Building;
            if (building != null && building.StoresGold)
                world.Highlight(building);
            else
                world.Highlight(null);

            base.Follow(hand);
        }
        
        public override bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // If dropped on a gold storage, add to our total gold amount,
            // and we're done with this stone
            Building building = entity as Building;
            if (building != null && building.StoresGold)
            {
                world.GameLogic.Gold += Gold;

                hand.Drop();
                world.Destroy(this);
                return false;
            }

            // Otherwise place the stone.
            // TODO: Can we throw it away?
            return base.BeginDrop(hand, entity, leftButton);
        }

        #endregion
    }
    #endregion
}

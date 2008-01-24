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
            position.Z = world.Landscape.GetHeight(position.X, position.Y);
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            model.Update(gameTime);
        }

        /// <summary>
        /// Draw the tree
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            Matrix transform = Matrix.CreateRotationZ(rotation);
            transform.Translation = position;
            model.Transform = transform;
            model.Draw(gameTime, delegate(BasicEffect effect)
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
        /// position is invalid
        /// </summary>
        /// <param name="gameTime"></param>
        public void DrawInvalid(GameTime gameTime)
        {
            Matrix transform = Matrix.CreateRotationZ(rotation);
            transform.Translation = position;
            model.Transform = transform;
            model.Draw(gameTime, delegate(BasicEffect effect)
            {
                effect.DiffuseColor = Vector3.UnitX;
            });
        }

        /// <summary>
        /// Place the tree at a new location
        /// </summary>
        /// <returns>Success or not</returns>
        public override bool Place(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            position = newPosition;
            rotation = newRotation;

            // Fall on the ground
            position.Z = landscape.GetHeight(position.X, position.Y);

            // A tree only covers one grid :)
            // FIXME: One grid is not large enough for picking...
            //        Find a way to deal with it!!!
            Point grid = landscape.PositionToGrid(position.X, position.Y);

            if (!landscape.IsValidGrid(grid))
                return false;

            if (landscape.Data[grid.X, grid.Y].Owners.Count != 0)
                return false;

            if (landscape.Data[grid.X, grid.Y].Type != LandscapeType.Ground)
                return false;

            landscape.Data[grid.X, grid.Y].Owners.Add(this);
            return true;
        }

        /// <summary>
        /// Remove the game entity from the landscape
        /// </summary>
        /// <returns>Success or not</returns>
        public override bool Pickup(Landscape landscape)
        {
            Point grid = landscape.PositionToGrid(position.X, position.Y);

            System.Diagnostics.Debug.Assert(landscape.Data[grid.X, grid.Y].Owners.Contains(this));

            landscape.Data[grid.X, grid.Y].Owners.Remove(this);
            return true;
        }

        /// <summary>
        /// Ray intersection test
        /// </summary>
        /// <param name="ray">Target ray</param>
        /// <returns>
        /// Distance from the intersection point to the ray starting position,
        /// Null if there's no intersection.
        /// </returns>
        public override Nullable<float> Intersects(Ray ray)
        {
            // Transform ray to object space
            Matrix worldInverse = Matrix.Invert(model.Transform);
            Vector3 newPosition = Vector3.Transform(ray.Position, worldInverse);
            Vector3 newTarget = Vector3.Transform(ray.Position + ray.Direction, worldInverse);
            Ray newRay = new Ray(newPosition, newTarget - newPosition);

            // Perform a bounding box intersection...
            //
            // HACK HACK!!! We need a more accurate algorithm :)
            return newRay.Intersects(model.BoundingBox);
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

        public override bool EndDrop(Hand hand, Entity entity, bool leftButton)
        {
            if (!Place(world.Landscape))
            {
                world.Destroy(this);
            }

            return true;
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

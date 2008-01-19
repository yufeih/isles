using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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

        }

        public override void Deserialize(IDictionary<string, string> attributes)
        {
            base.Deserialize(attributes);

            string value;

            if (attributes.TryGetValue("Gold", out value))
                Gold = int.Parse(value);
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
        /// Draw the stone
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
        /// Draw the stone use an effect to show that the current
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
        /// Place the stone at a new location
        /// </summary>
        /// <returns>Success or not</returns>
        public override bool Place(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            position = newPosition;
            rotation = newRotation;

            // Fall on the ground
            position.Z = landscape.GetHeight(position.X, position.Y);

            // A stone only covers one grid :)
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
            // HACK HACK!!! We need a mstone accurate algorithm :)
            return newRay.Intersects(model.BoundingBox);
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

            // Otherwise, overwrite rotating behavior,
            // we want to throw the stone
            mouseBeginDropRotation = rotation;
            mouseBeginDropPosition = Input.MousePosition;
            mouseBeginDropPosition.Y -= 10;
            return true;
        }

        /// <summary>
        /// Called when the user is dropping the entity
        /// </summary>
        public override void Dropping(Hand hand, Entity entity, bool leftButton)
        {
            rotation = mouseBeginDropRotation + MathHelper.PiOver2 + (float)Math.Atan2(
                -(double)(Input.MousePosition.Y - mouseBeginDropPosition.Y),
                 (double)(Input.MousePosition.X - mouseBeginDropPosition.X));
        }

        /// <summary>
        /// Called when the user decided to drop this entity (button just released)
        /// </summary>
        /// <param name="entity">
        /// The target entity to be drop to (can be null).
        /// </param>
        /// <returns>
        /// Whether the hand should drop this entity
        /// </returns>
        public override bool EndDrop(Hand hand, Entity entity, bool leftButton)
        {
            if (!Place(world.Landscape))
            {
                // Drop failed, removes it
                world.Destroy(this);
            }
            return true;
        }

        #endregion
    }
    #endregion
}

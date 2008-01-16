using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    #region Stone
    /// <summary>
    /// Respresents a stone in the game
    /// </summary>
    public class Stone : Entity
    {
        #region Fields
        /// <summary>
        /// Settings of this stone
        /// </summary>
        StoneSettings settings;

        /// <summary>
        /// Model of this stone
        /// </summary>
        GameModel model;

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
        /// Gets the game model of this stone
        /// </summary>
        public GameModel Model
        {
            get { return model; }
        }

        /// <summary>
        /// Gets or sets the settings of this stone
        /// </summary>
        public StoneSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }
        #endregion
        
        #region Methods
        /// <summary>
        /// Create a new stone
        /// </summary>
        public Stone(GameScreen gameScreen, StoneSettings settings) : base (gameScreen)
        {
            this.settings = settings;

            Name = settings.Name;

            // NOTE: Override existing root transform...
            Model xnaModel = gameScreen.LevelContent.Load<Model>(settings.Model);
            xnaModel.Root.Transform = settings.Transform;
            model = new GameModel(gameScreen.Game, xnaModel);

            // Stone size are fixed?
            size = model.BoundingBox.Max - model.BoundingBox.Min;
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
            Building building = screen.Pick() as Building;
            if (building != null && building.Settings.StoreCrystal)
                screen.EntityManager.Highlighted = building;
            else
                screen.EntityManager.Highlighted = null;

            base.Follow(hand);
        }
        
        public override bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // If dropped on a gold storage, add to our total gold amount,
            // and we're done with this stone
            Building building = entity as Building;
            if (building != null && building.Settings.StoreCrystal)
            {
                screen.Gold += Settings.Gold;

                hand.Drop();
                screen.EntityManager.RemoveStone(this);
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
            if (!Place(screen.Landscape))
            {
                // Drop failed, removes it
                screen.EntityManager.Remove(this);
            }
            return true;
        }

        #endregion
    }
    #endregion

    #region StoneSettings
    /// <summary>
    /// Settings for a single stone
    /// </summary>
    [Serializable()]
    public class StoneSettings
    {
        #region Variables
        /// <summary>
        /// Name of the stone
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Description of the stone
        /// </summary>
        public string Description = "";

        /// <summary>
        /// Asset name of the stone model
        /// </summary>
        public string Model = "";

        /// <summary>
        /// Transform of the model
        /// </summary>
        public Matrix Transform = Matrix.Identity;

        /// <summary>
        /// How much gold it provides
        /// </summary>
        public int Gold;
        #endregion
    }

    /// <summary>
    /// Settings for all stones
    /// </summary>
    [Serializable()]
    public class StoneSettingsCollection: ICollection
    {
        List<StoneSettings> settings = new List<StoneSettings>();

        public StoneSettings this[int index]
        {
            get { return settings[index]; }
        }

        public void CopyTo(Array a, int index)
        {
            settings.CopyTo((StoneSettings[])a, index);
        }

        public int Count
        {
            get { return settings.Count; }
        }

        public object SyncRoot
        {
            get { return this; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public IEnumerator GetEnumerator()
        {
            return settings.GetEnumerator();
        }

        public void Add(StoneSettings newStone)
        {
            settings.Add(newStone);
        }
    }
    #endregion
}

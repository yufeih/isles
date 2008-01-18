using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    #region Building
    /// <summary>
    /// Respresents a building in the game
    /// </summary>
    public class Building : Entity
    {
        #region BuildingState
        /// <summary>
        /// State of the building
        /// </summary>
        public enum BuildingState
        {
            /// <summary>
            /// Normal state
            /// </summary>
            Normal,

            /// <summary>
            /// The user is finding where to place the building
            /// </summary>
            Locating,

            /// <summary>
            /// The building is under construction
            /// </summary>
            Constructing,

            /// <summary>
            /// The construction stops due to lack of resource
            /// </summary>
            Onhold,

            /// <summary>
            /// The building is destroyed, leaving some ashes
            /// </summary>
            Destroyed,
        }
        #endregion

        #region Fields
        /// <summary>
        /// State of the building
        /// </summary>
        protected BuildingState state = BuildingState.Locating;

        /// <summary>
        /// Base height of the building
        /// </summary>
        protected float baseHeight;

        /// <summary>
        /// Settings of this building
        /// </summary>
        protected BuildingSettings settings;

        /// <summary>
        /// Whether the location is valid for this building
        /// </summary>
        protected bool isValidLocation = true;

        /// <summary>
        /// Health of the building
        /// </summary>
        protected float health;

        /// <summary>
        /// Gets or sets the health of the building
        /// </summary>
        public float Health
        {
            get { return health; }

            set
            {
                if (value <= 0)
                {
                    health = 0;
                    state = BuildingState.Destroyed;
                    // TODO: destroy the building
                }
            }
        }

        /// <summary>
        /// Gets the building state
        /// </summary>
        public BuildingState State
        {
            get { return state; }
        }

        /// <summary>
        /// Gets or set the base height of the building
        /// </summary>
        public float BaseHeight
        {
            get { return baseHeight; }
            set { baseHeight = value; }
        }

        /// <summary>
        /// Gets or sets the settings of this building
        /// </summary>
        public BuildingSettings Settings
        {
            get { return settings; }
            set { settings = value; }
        }
        #endregion
        
        #region Methods
        /// <summary>
        /// Create a new building
        /// </summary>
        public Building(GameWorld world, BuildingSettings settings) : base(world)
        {
            this.settings = settings;

            Name = settings.Name;

            health = settings.Health;

            // NOTE: Override existing root transform...
            Model xnaModel = world.LevelContent.Load<Model>(settings.Model);
            xnaModel.Root.Transform = settings.Transform;
            model = new GameModel(xnaModel);

            // Center game model (Only on XY axis)
            model.CenterModel(false);

            size.Z = model.BoundingBox.Max.Z;
            size.X = model.BoundingBox.Max.X - model.BoundingBox.Min.X;
            size.Y = model.BoundingBox.Max.Y - model.BoundingBox.Min.Y;

            baseHeight = -model.BoundingBox.Min.Z * 5;    // FIXME: Time 2 for debug only

            settings.ConstructionTime = 0;  // CHEAT!! CHEAT!!
        }

        /// <summary>
        /// Change game UI when this building is selected
        /// </summary>
        protected override void OnSelectStateChanged()
        {
            if (selected)
            {
                if (settings.Functions.Count > 0)
                {
                    //world.ClearScrollPanelElements();

                    //foreach (string key in settings.Functions)
                    //{
                    //    world.AddScrollPanelElement(
                    //        world.GetFunction(key).UIControl);
                    //}
                }
                else
                {
                    //world.ResetScrollPanelElements();
                }
            }
        }

        /// <summary>
        /// Tests to see if the building is in a valid location
        /// </summary>
        /// <remarks>
        /// You can only rotate the building around Z axis to let this
        /// algorithm work !!!
        /// </remarks>
        /// <returns></returns>
        public bool IsValidLocation(Landscape landscape)
        {
            // Make sure it's on the landscape
            if (position.X < 0 || position.Y < 0 ||
                position.X > landscape.Width || position.Y > landscape.Depth)
            {
                return false;
            }

            // Keep track of entities
            List<Entity> entities = new List<Entity>(2);

            float z = landscape.GetHeight(position.X, position.Y);
            foreach (Point grid in new Landscape.GridEnumerator(
                landscape, model.BoundingBox.Min, model.BoundingBox.Max, position, rotation))
            {
                //if (!IsValidGrid(landscape, grid, z))
                //    return false;

                foreach (Entity entity in landscape.Data[grid.X, grid.Y].Owners)
                {
                    if (!entities.Contains(entity))
                    {
                        // Rectangle intersection test
                        if (Math2D.RectangleIntersects(
                            new Vector2(-size.X / 2, -size.Y / 2),
                            new Vector2(size.X / 2, size.Y / 2),
                            new Vector2(position.X, position.Y), rotation,
                            new Vector2(-entity.Size.X / 2, -entity.Size.Y / 2),
                            new Vector2(entity.Size.X / 2, entity.Size.Y / 2),
                            new Vector2(entity.Position.X, entity.Position.Y),
                            entity.Rotation) != ContainmentType.Disjoint)
                        {
                            return false;
                        }

                        entities.Add(entity);
                    }
                }
            }
            
            return true;
        }

        /// <summary>
        /// Test if a grid is valid for this building
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        bool IsValidGrid(Landscape landscape, Point grid, float z)
        {
            // Should be on the ground and not owned by others
            if (landscape.Data[grid.X, grid.Y].Type != LandscapeType.Ground/* ||
                landscape.Data[grid.X, grid.Y].Owners.Count != 0*/)
                return false;

            // Shouldn't be on somewhere too rough
            if (z - landscape.HeightField[grid.X, grid.Y] > baseHeight)
                return false;
            
            return true;
        }

        /// <summary>
        /// Place the game entity somewhere on the ground and complete it immediately
        /// </summary>
        /// <returns>Success or not</returns>
        public bool PlaceAndCompleteBuild(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            if (!Place(landscape, newPosition, newRotation))
                return false;

            CompleteBuild();
            return true;
        }

        /// <summary>
        /// Place the game entity somewhere on the ground
        /// </summary>
        /// <returns>Success or not</returns>
        public override bool Place(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            // Store new position/rotation
            position = newPosition;
            rotation = newRotation;

            if (!IsValidLocation(landscape))
                return false;

            // Fall on the ground
            position.Z = landscape.GetHeight(position.X, position.Y);

            Landscape.GridEnumerator grids = new Landscape.GridEnumerator(
                landscape, model.BoundingBox.Min, model.BoundingBox.Max, position, rotation);

            // Change grid owner
            foreach (Point grid in grids)
                landscape.Data[grid.X, grid.Y].Owners.Add(this);

            // Set state to constructing
            state = BuildingState.Constructing;
            startTime = BaseGame.Singleton.CurrentGameTime.TotalGameTime.TotalSeconds;

            return true;
        }

        /// <summary>
        /// Remove the game entity from the landscape
        /// </summary>
        /// <returns>Success or not</returns>
        public override bool Pickup(Landscape landscape)
        {
            // Set everything back to null
            Landscape.GridEnumerator grids = new Landscape.GridEnumerator(
                landscape, model.BoundingBox.Min, model.BoundingBox.Max, position, rotation);

            // Change grid owner
            foreach (Point grid in grids)
            {
                System.Diagnostics.Debug.Assert(landscape.Data[grid.X, grid.Y].Owners.Contains(this));

                landscape.Data[grid.X, grid.Y].Owners.Remove(this);
            }
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
        #endregion

        #region Drag & Drop
        public override void EndDrag(Hand hand)
        {
            // Nothing will be dragged, since we can't change
            // a building's position once placed :)
        }

        public override void Follow(Hand hand)
        {
            isValidLocation = IsValidLocation(world.Landscape);
            base.Follow(hand);
        }

        public override bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // Right click will cancel building
            if (!leftButton)
            {
                hand.Drop();
                world.Destroy(this);
                return false;
            }

            return base.BeginDrop(hand, entity, leftButton);
        }

        public override void Dropping(Hand hand, Entity entity, bool leftButton)
        {
            isValidLocation = IsValidLocation(world.Landscape);
            base.Dropping(hand, entity, leftButton);
        }

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

        #region Update & Draw

        /// <summary>
        /// Progress when constructing the building.
        /// </summary>
        double startTime;
        int woodSpend, goldSpend;
        float progress;

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            model.Update(gameTime);

            if (state == BuildingState.Constructing)
            {
                if (settings.ConstructionTime > 0)
                {
                    progress = (float)(
                        (gameTime.TotalGameTime.TotalSeconds - startTime) /
                            settings.ConstructionTime);
                }
                else
                {
                    progress = 1;
                }

                // Check to see if we have enough wood and gold
                int wood = (int)(settings.Wood * progress - woodSpend);
                int gold = (int)(settings.Gold * progress - goldSpend);

                if (wood <= world.GameLogic.Wood &&
                    gold <= world.GameLogic.Gold)
                {
                    if (woodSpend + wood >= settings.Wood &&
                        goldSpend + gold >= settings.Gold)
                    {
                        wood = settings.Wood - woodSpend;
                        gold = settings.Gold - goldSpend;

                        CompleteBuild();
                    }

                    world.GameLogic.Wood -= wood;
                    world.GameLogic.Gold -= gold;

                    woodSpend += wood;
                    goldSpend += gold;
                }
                else
                {
                    // Otherwise our building is on hold
                    state = BuildingState.Onhold;
                }
            }
            else if (state == BuildingState.Onhold)
            {
                startTime += gameTime.ElapsedGameTime.TotalSeconds;

                // Check to see if we have additional wood and gold
                float f = (float)(
                    (gameTime.TotalGameTime.TotalSeconds - startTime) /
                        settings.ConstructionTime);

                // Check to see if we have enough wood and gold
                int wood = (int)(settings.Wood * f - woodSpend);
                int gold = (int)(settings.Gold * f - goldSpend);

                if (wood <= world.GameLogic.Wood && gold <= world.GameLogic.Gold)
                {
                    state = BuildingState.Constructing;
                }
            }
        }

        private void CompleteBuild()
        {
            // Construction conplete
            state = BuildingState.Normal;

            // Building finished, update dependencies
            world.GameLogic.Dependencies[settings.Name] = true;
        }

        /// <summary>
        /// Draw the building
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            if (isValidLocation)
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
            else
            {
                DrawInvalid(gameTime);
            }
        }

        /// <summary>
        /// Draw the building use an effect to show that the current
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

        public override Rectangle DrawStatus(Rectangle region)
        {
            Text.DrawString("Name: " + Name + "\nDescription: " + settings.Description,
                15, new Vector2((float)region.X, (float)region.Y), Color.Orange);

            return region;
        }
        #endregion
    }

    #endregion

    #region BuildingSettings
    /// <summary>
    /// Settings for a single building
    /// </summary>
    [Serializable()]
    public class BuildingSettings
    {
        #region Variables
        /// <summary>
        /// Name of the building
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Description of the building
        /// </summary>
        public string Description = "";

        /// <summary>
        /// Asset name of the building model
        /// </summary>
        public string Model = "";

        /// <summary>
        /// Transform of the model
        /// </summary>
        public Matrix Transform = Matrix.Identity;

        /// <summary>
        /// Hot key of the building
        /// </summary>
        public Keys Hotkey;

        /// <summary>
        /// Icon of the building
        /// </summary>
        public int Icon;

        /// <summary>
        /// How much wood it costs
        /// </summary>
        public int Wood;

        /// <summary>
        /// How many gold it costs
        /// </summary>
        public int Gold;

        /// <summary>
        /// How many seconds it takes to construct
        /// </summary>
        public float ConstructionTime;

        /// <summary>
        /// How much damage it can suffer
        /// </summary>
        public float Health;

        /// <summary>
        /// Whether this building stores wood or not
        /// </summary>
        public bool StoreWood;

        /// <summary>
        /// Whether this building stores gold or not
        /// </summary>
        public bool StoreCrystal;

        /// <summary>
        /// Whether this building stores food or not
        /// </summary>
        public bool StoreFood;

        /// <summary>
        /// Whether this building is farmland or not,
        /// farmlands are treated specially
        /// </summary>
        public bool IsFarmland;

        /// <summary>
        /// Dependencies
        /// </summary>
        public List<string> Dependencies = new List<string>();

        /// <summary>
        /// Functions of the building
        /// </summary>
        public List<string> Functions = new List<string>();
        #endregion
    }

    #region BuildingSettingsCollection
    /// <summary>
    /// Settings for all buildings
    /// </summary>
    [Serializable()]
    public class BuildingSettingsCollection: ICollection
    {
        List<BuildingSettings> settings = new List<BuildingSettings>();

        public BuildingSettings this[int index]
        {
            get { return settings[index]; }
        }

        public void CopyTo(Array a, int index)
        {
            settings.CopyTo((BuildingSettings[])a, index);
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

        public void Add(BuildingSettings newBuilding)
        {
            settings.Add(newBuilding);
        }
    }
    #endregion
    #endregion
}

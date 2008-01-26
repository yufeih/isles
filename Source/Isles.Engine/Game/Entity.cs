using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    #region BaseEntity
    /// <summary>
    /// Base world object
    /// </summary>
    public abstract class BaseEntity : IWorldObject, IAudioEmitter
    {
        #region Field

        public static int EntityCount = 0;

        /// <summary>
        /// Game world
        /// </summary>
        protected GameWorld world;

        /// <summary>
        /// Gets or sets the 3D position of the entity.
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
            set { position = value; IsDirty = true; }
        }

        protected Vector3 position;
        

        /// <summary>
        /// Gets or sets which way the entity is facing.
        /// </summary>
        public Vector3 Forward
        {
            get { return forward; }
        }

        protected Vector3 forward;


        /// <summary>
        /// Gets or sets the orientation of this entity.
        /// </summary>
        public Vector3 Up
        {
            get { return Vector3.UnitZ; }
        }


        /// <summary>
        /// Gets or sets how fast this entity is moving.
        /// </summary>
        public Vector3 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        protected Vector3 velocity;


        protected bool isDirty = false;

        public bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        protected BoundingBox boundingBox;

        public BoundingBox BoundingBox
        {
            get { return boundingBox; }
        }

        /// <summary>
        /// Name of our game entity
        /// </summary>
        protected string name;

        /// <summary>
        /// Gets or sets entity name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        protected string classID;

        /// <summary>
        /// Gets or sets the class ID of this world object
        /// </summary>
        public string ClassID
        {
            get { return classID; }
            set { classID = value; }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public BaseEntity(GameWorld world)
        {
            this.world = world;
            this.name = "Entity " + (EntityCount++);
        }

        /// <summary>
        /// Update
        /// </summary>
        /// <param name="gameTime"></param>
        public abstract void Update(GameTime gameTime);

        /// <summary>
        /// Draw the entity
        /// </summary>
        /// <param name="gameTime"></param>
        public abstract void Draw(GameTime gameTime);

        /// <summary>
        /// Write the scene object to an output stream
        /// Serialized attributes: Name, Position, Velocity
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Serialize(XmlElement xml)
        {
            xml.SetAttribute("Name", name);
            xml.SetAttribute("Position", Helper.Vector3Tostring(position));
            xml.SetAttribute("Velocity", Helper.Vector3Tostring(velocity));
        }

        /// <summary>
        /// Read and initialize the scene object from an input stream.
        /// Deserialized attributes: Name, Position, Velocity
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Deserialize(XmlElement xml)
        {
            string value;

            name = xml.GetAttribute("Name");

            if ((value = xml.GetAttribute("Position")) != "")
                position = Helper.StringToVector3(value);

            if ((value = xml.GetAttribute("Velocity")) != "")
                velocity = Helper.StringToVector3(value);
        }
    }
    #endregion

    #region Entity

    /// <summary>
    /// Base class for all pickable game entities
    /// </summary>
    public abstract class Entity : BaseEntity
    {
        /// <summary>
        /// This is the max height for any game entity
        /// </summary>
        public const float MaxHeight = 1000.0f;

        #region Field
        /// <summary>
        /// Gets or sets entity description
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        protected string description;

        /// <summary>
        /// Gets the model of the entity
        /// </summary>
        public GameModel Model
        {
            get { return model; }
        }

        protected GameModel model;
        
        /// <summary>
        /// Entitis can only rotate on the XY plane :(
        /// </summary>
        protected float rotation;

        /// <summary>
        /// Size of the entity in object space
        /// </summary>
        protected Vector3 size;

        /// <summary>
        /// Gets or sets Whether this entity has been selected
        /// </summary>
        protected bool selected;

        /// <summary>
        /// Gets or sets whether this entity has been highlighted. E.g. Mouse over
        /// </summary>
        protected bool highlighted;

        /// <summary>
        /// Forces needed to drag this entity
        /// </summary>
        protected float dragForce = 100;

        /// <summary>
        /// A list of spells owned by this entity
        /// </summary>
        protected List<Spell> spells = new List<Spell>();

        /// <summary>
        /// Gets or sets entity rotation
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        /// <summary>
        /// Gets the size of the entity in object space
        /// </summary>
        public Vector3 Size
        {
            get { return size; }
        }

        /// <summary>
        /// Gets or sets Whether this entity has been selected
        /// </summary>
        public bool Selected
        {
            get { return selected; }
            set { selected = value; if (selected == value) OnSelectStateChanged(); }
        }

        /// <summary>
        /// Gets or sets whether this entity has been highlighted. E.g. Mouse over
        /// </summary>
        public bool Highlighted
        {
            get { return highlighted; }
            set { highlighted = value; if (highlighted == value) OnHighlightStateChanged(); }
        }

        #endregion

        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        public Entity(GameWorld world)
            : base(world)
        {

        }
        
        public Entity(GameWorld world, GameModel model)
            : base(world)
        {
            this.model = model;
        }

        /// <summary>
        /// Serialized attributes: Description, Model, Transform, Scale
        /// </summary>
        /// <param name="xml"></param>
        public override void Serialize(XmlElement xml)
        {
            base.Serialize(xml);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserialized attributes: Description, Model, Transform, Scale, Rotation
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);

            string value = "";

            // Get entity description
            description = xml.GetAttribute("Description");

            // Initialize model
            if ((value = xml.GetAttribute("Model")) != "")
            {
                GameModel newModel = // Treat game model as level content
                    new GameModel(world.LevelContent.Load<Model>(value));

                Matrix transform = Matrix.Identity;

                // Get model transform
                if ((value = xml.GetAttribute("Transform")) != "")
                    transform = Helper.StringToMatrix(value);

                if ((value = xml.GetAttribute("Scale")) != "")
                    transform *= Matrix.CreateScale(Helper.StringToVector3(value));

                if (transform != Matrix.Identity)
                {
                    newModel.Model.Root.Transform = transform;
                    newModel.Refresh();
                }

                SetModel(newModel);
            }

            // Get rotation
            float.TryParse(xml.GetAttribute("Rotation"), out rotation);

            // Get spells
            XmlNodeList spellNodes = xml.SelectNodes("Spell");
            foreach (XmlNode node in spellNodes)
            {
                XmlElement element = node as XmlElement;

                if (element != null && 
                   (value = element.GetAttribute("Class")) != "")
                {
                    spells.Add(Spell.Create(value, world));
                }
            }

            // OLD CODE: Place the entity
            Place(world.Landscape);
        }

        /// <summary>
        /// Get entity spells
        /// </summary>
        public virtual IEnumerable<Spell> Spells
        {
            get { return spells; }
        }

        /// <summary>
        /// Set the model of the entity
        /// </summary>
        public virtual void SetModel(GameModel model)
        {
            this.model = model;

            // Center game model by default
            this.model.CenterModel(false);

            // Compute size based on game model
            if (model != null)
                size = model.BoundingBox.Max - model.BoundingBox.Min;
        }

        /// <summary>
        /// Transform a point from world space to local space
        /// NOTE: This implementation does not consider z value
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vector3 WorldToLocal(Vector3 p)
        {
            Vector2 v, pos;

            v.X = p.X;
            v.Y = p.Y;

            pos.X = position.X;
            pos.Y = position.Y;

            return new Vector3(
                Math2D.WorldToLocal(v, pos, rotation), p.Z);
        }

        /// <summary>
        /// Transform a point from local space to world space
        /// NOTE: This implementation does not consider z value
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Vector3 LocalToWorld(Vector3 p)
        {
            Vector2 v, pos;

            v.X = p.X;
            v.Y = p.Y;

            pos.X = position.X;
            pos.Y = position.Y;

            return new Vector3(
                Math2D.LocalToWorld(v, pos, rotation), p.Z);
        }

        /// <summary>
        /// Test to see if this entity is visible from a given view and projection
        /// </summary>
        /// <returns></returns>
        public virtual bool VisibilityTest(Matrix viewProjection)
        {
            // Transform position to projection space
            //Vector3 p = Vector3.Transform(position, viewProjection);
            BoundingFrustum f = new BoundingFrustum(viewProjection);
            return (f.Contains(position) == ContainmentType.Contains);
        }

        /// <summary>
        /// Gets the spells of the entity
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Spell> GetSpells()
        {
            return null;
        }

        /// <summary>
        /// Place the game entity somewhere on the ground using
        /// existing entity position and rotation
        /// </summary>
        public bool Place(Landscape landscape)
        {
            return Place(landscape, position, rotation);
        }
        
        /// <summary>
        /// Place the game entity somewhere on the ground
        /// </summary>
        /// <returns>Success or not</returns>
        public virtual bool Place(Landscape landscape, Vector3 newPosition, float newRotation)
        {
            return false;
        }

        /// <summary>
        /// Pick up the game entity from the landscape
        /// </summary>
        /// <returns>Success or not</returns>
        public virtual bool Pickup(Landscape landscape)
        {
            return false;
        }

        /// <summary>
        /// Called when the entity highlighted state changed
        /// </summary>
        protected virtual void OnHighlightStateChanged() { }

        /// <summary>
        /// Called when the entity selected state changed
        /// </summary>
        protected virtual void OnSelectStateChanged() { }

        /// <summary>
        /// Draw the entity status when selected
        /// </summary>
        /// <returns>The rectangle drawed</returns>
        public virtual Rectangle DrawStatus(Rectangle region)
        {
            Text.DrawString(Name, 15, new Vector2((float)region.X, (float)region.Y), Color.Orange);
            return region;
        }

        /// <summary>
        /// Tests whether the object occupies the specified point.
        /// </summary>
        /// <param name="point">Point to be tested in world space</param>
        /// <returns></returns>
        public virtual bool Intersects(Vector3 point)
        {
            if (null == model)
                return false;

            // Transform point to object space
            Matrix worldInverse = Matrix.Invert(model.Transform);
            Vector3 newPosition = Vector3.Transform(point, worldInverse);

            // Performs a bounding box test
            return model.BoundingBox.Contains(newPosition) == ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the object intersects the specified ray.
        /// </summary>
        /// <param name="ray">Ray to be tested in world space</param>
        /// <returns></returns>
        public virtual float? Intersects(Ray ray)
        {
            if (null == model)
                return null;

            // Transform ray to object space
            Matrix worldInverse = Matrix.Invert(model.Transform);
            Vector3 newPosition = Vector3.Transform(ray.Position, worldInverse);
            Vector3 newTarget = Vector3.Transform(ray.Position + ray.Direction, worldInverse);
            Ray newRay = new Ray(newPosition, newTarget - newPosition);

            // Perform a bounding box intersection...
            return newRay.Intersects(model.BoundingBox);
        }
        
        #region Drag & Drop
        /// <summary>
        /// Called when the user decided to drag this entity (button just pressed)
        /// </summary>
        /// <returns>
        /// Whether this entity shall be dragged (or receive Dragging/EndDrag events)
        /// </returns>
        public virtual bool BeginDrag(Hand hand) { return true; }

        /// <summary>
        /// Called when the user is dragging the entity
        /// </summary>
        public virtual void Dragging(Hand hand) { }

        /// <summary>
        /// Called when the user decided to drag this entity (button just released)
        /// </summary>
        public virtual void EndDrag(Hand hand)
        {
            // Pick up then entity only when the hand has enough power
            if (hand.Power >= dragForce)
            {
                Pickup(world.Landscape);
                hand.Drag(this);
            }
        }

        /// <summary>
        /// Called when this entity is being carried by a hand
        /// </summary>
        public virtual void Follow(Hand hand)
        {
            position = hand.Position;
        }

        protected Point mouseBeginDropPosition;
        protected float mouseBeginDropRotation;

        /// <summary>
        /// Called when the user decided to drop this entity (button just pressed)
        /// </summary>
        /// <param name="entity">
        /// The target entity to be drop to (can be null).
        /// </param>
        /// <returns>
        /// OLD: Whether the hand should drag the target entity or not.
        /// If the return value is true, the target entity will receive BeginDrap event,
        /// and this entity will not receive Dropping/EndDrop event.
        /// 
        /// NEW: Whether this entity would like to continue receive Dropping/EndDrop events
        /// </returns>
        /// <remarks>
        /// Place the entity on the ground plus rotating it by default
        /// </remarks>
        public virtual bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // Otherwise, place it on the ground
            mouseBeginDropRotation = rotation;
            mouseBeginDropPosition = Input.MousePosition;
            mouseBeginDropPosition.Y -= 10;
            return true; 
        }

        /// <summary>
        /// Called when the user is dropping the entity
        /// </summary>
        public virtual void Dropping(Hand hand, Entity entity, bool leftButton)
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
        public virtual bool EndDrop(Hand hand, Entity entity, bool leftButton)
        {
            return false;
        }
        #endregion
        #endregion
    }

    #endregion
    
    #region BaseAgent
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
        #endregion

        public BaseAgent(GameWorld world)
            : base(world)
        {

        }
    }
    #endregion
}

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
        public virtual Vector3 Position
        {
            get { return position; }

            set
            {
                // Mark bounding box dirty, save the old bounding box
                IsDirty = true;
                position = value;
            }
        }

        Vector3 position;
        

        /// <summary>
        /// Gets or sets which way the entity is facing.
        /// </summary>
        public virtual Vector3 Forward
        {
            get { return Vector3.UnitZ; }
        }


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
        public virtual Vector3 Velocity
        {
            get { return Vector3.Zero; }
        }


        /// <summary>
        /// By marking the IsDirty property of a scene object, the scene
        /// manager will be able to adjust its internal data structure
        /// to adopt to the change of transformation.
        /// </summary>
        public virtual bool IsDirty
        {
            get { return false; }
            set { }
        }


        /// <summary>
        /// Gets or sets whether the dirty bounding box
        /// </summary>
        public virtual BoundingBox DirtyBoundingBox
        {
            get { return new BoundingBox(); }
            set { }
        }


        public virtual BoundingBox BoundingBox
        {
            get { return new BoundingBox(position, position); }
        }


        /// <summary>
        /// Gets or sets entity name
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string name;
        

        /// <summary>
        /// Gets or sets the class ID of this world object
        /// </summary>
        public string ClassID
        {
            get { return classID; }
            set { classID = value; }
        }

        string classID;


        /// <summary>
        /// Gets or sets whether this world object is active
        /// </summary>
        public virtual bool IsActive
        {
            get { return false; }
            set { throw new Exception("Base entity is inactive by default"); }
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
        }

        /// <summary>
        /// Read and initialize the scene object from an input stream.
        /// Deserialized attributes: Name, Position, Velocity
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Deserialize(XmlElement xml)
        {
            string value;

            if (xml.HasAttribute("Name"))
                name = xml.GetAttribute("Name");

            if ((value = xml.GetAttribute("Position")) != "")
                // Note this should be the upper case Position!!!
                Position = Helper.StringToVector3(value);
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

            set
            {
                MarkDirty();
                model = value;
                model.CenterModel(false);
            }
        }

        GameModel model;


        /// <summary>
        /// Gets or sets entity position.
        /// </summary>
        public override Vector3 Position
        {
            set
            {
                MarkDirty();
                base.Position = new Vector3(value.X, value.Y,
                    world.Landscape.GetHeight(value.X, value.Y));
            }
        }


        /// <summary>
        /// Gets or sets entity rotation on the XY plane, in radius.
        /// </summary>
        public float Rotation
        {
            get { return rotation; }
            set { MarkDirty(); rotation = value; }
        }

        float rotation = 0;


        /// <summary>
        /// Gets or sets entity rotation on the X axis
        /// </summary>
        public float RotationX
        {
            get { return rotationX; }
            set { MarkDirty(); rotationX = value; }
        }

        float rotationX = 0;


        /// <summary>
        /// Gets or sets entity rotation on the Y axis
        /// </summary>
        public float RotationY
        {
            get { return rotationY; }
            set { MarkDirty(); rotationY = value; }
        }

        float rotationY = 0;


        /// <summary>
        /// Gets or set entity scale
        /// </summary>
        public Vector3 Scale
        {
            get { return scale; }
            set { MarkDirty(); scale = value; }
        }
        
        Vector3 scale = Vector3.One;


        /// <summary>
        /// Gets or sets Whether this entity has been selected
        /// </summary>
        public bool Selected
        {
            get { return selected; }
            set { selected = value; if (selected == value) OnSelectStateChanged(); }
        }

        bool selected;


        /// <summary>
        /// Gets model transform
        /// </summary>
        public Matrix Transform
        {
            get
            {
                if (isTransformDirty)
                {
                    transform = // Build transform from SRT values
                                // Bug: Something's wrong with Matrix.CreateFromYawPitchRoll
                        Matrix.CreateScale(scale) *
                        //Matrix.CreateFromYawPitchRoll(rotationY, rotationX, rotation) *
                        Matrix.CreateRotationX(rotationX) *
                        Matrix.CreateRotationY(rotationY) *
                        Matrix.CreateRotationZ(rotation) *
                        Matrix.CreateTranslation(Position);

                    isTransformDirty = false;
                }

                return transform;
            }
        }

        Matrix transform;


        /// <summary>
        /// Interface member for IWorldObject
        /// </summary>
        public override bool IsDirty
        {
            get { return isDirty; }
            set { isDirty = value; }
        }

        bool isDirty = true;
        bool isTransformDirty = true;


        /// <summary>
        /// Mark both bounding box and transform
        /// </summary>
        void MarkDirty()
        {
            isDirty = true;
            isTransformDirty = true;
        }


        /// <summary>
        /// Returns the axis aligned bounding box of the game model
        /// </summary>
        public override BoundingBox BoundingBox
        {
            get { return model != null ? model.BoundingBox : new BoundingBox(); }
        }

        /// <summary>
        /// Gets or sets the dirty bounding box
        /// </summary>
        public override BoundingBox DirtyBoundingBox
        {
            get { return dirtyBoundingBox; }
            set { dirtyBoundingBox = value; }
        }

        BoundingBox dirtyBoundingBox = new BoundingBox();


        /// <summary>
        /// Gets the size of the entity
        /// </summary>
        public virtual Vector3 Size
        {
            get { return model.BoundingBox.Max - model.BoundingBox.Min; }
        }


        /// <summary>
        /// Gets or sets whether this entity has been highlighted. E.g. Mouse over
        /// </summary>
        public bool Highlighted
        {
            get { return highlighted; }
            set { highlighted = value; if (highlighted == value) OnHighlightStateChanged(); }
        }


        bool highlighted;


        /// <summary>
        /// Gets entity outline
        /// </summary>
        public Outline Outline
        {
            get { return outline; }
        }

        Outline outline = Outline.Empty;

        
        /// <summary>
        /// Gets or sets whether this world object is active
        /// </summary>
        /// <remarks>
        /// This property is internally used by ISceneManager.
        /// If you want to active or deactive a world object, call
        /// ISceneManager.Active or ISceneManager.Deactive instead.
        /// </remarks>
        public override bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        bool isActive;


        /// <summary>
        /// Forces needed to drag this entity
        /// </summary>
        protected float dragForce = 100;

        /// <summary>
        /// A list of spells owned by this entity
        /// </summary>
        protected List<Spell> spells = new List<Spell>();
        
        #endregion

        #region Methods
        /// <summary>
        /// Constructor
        /// </summary>
        public Entity(GameWorld world)
            : base(world)
        {
            // Entities are active by default
            world.Activate(this);
        }
        
        public Entity(GameWorld world, GameModel model)
            : base(world)
        {
            // Note assign to the upper case Model
            Model = model;

            // Entities are active by default
            world.Activate(this);
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
        /// Deserialized attributes: Description, Model, Position, Scale,
        /// Rotation, RotationX, RotationY
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);

            string value = "";

            // Get entity description
            if (xml.HasAttribute("Description"))
                description = xml.GetAttribute("Description");

            // Treat game model as level content
            if ((value = xml.GetAttribute("Model")) != "")
                Model = new GameModel(world.LevelContent.Load<Model>(value));
            
            // Get entity rotation & scale
            if ((value = xml.GetAttribute("Rotation")) != "")
                Rotation = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("RotationX")) != "")
                RotationX = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("RotationY")) != "")
                RotationY = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("Scale")) != "")
                Scale = Helper.StringToVector3(value);

            // Get entity spells
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
        }


        /// <summary>
        /// Get entity spells
        /// </summary>
        public virtual IEnumerable<Spell> Spells
        {
            get { return spells; }
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

            pos.X = Position.X;
            pos.Y = Position.Y;

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

            pos.X = Position.X;
            pos.Y = Position.Y;

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
            //BoundingFrustum f = new BoundingFrustum(viewProjection);

            //if (f.Contains(position) == ContainmentType.Contains)
            {
                // Distance to the eye
                if (Vector3.Subtract(
                    Position, BaseGame.Singleton.Eye).LengthSquared() <
                    BaseGame.Singleton.Settings.ViewDistanceSquared)
                {
                    return true;
                }
            }

            return false;
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

        public override void Update(GameTime gameTime)
        {
            if (isDirty)
            {
                // Update model transform, this will update the bounding box
                model.Transform = Transform;

                // By default, the outline of an entity is a circle
                outline.SetCircle(
                    new Vector2(Position.X, Position.Y),
                    (Size.X + Size.Y) / 2);
            }

            if (model != null)
                model.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (model != null && VisibilityTest(BaseGame.Singleton.ViewProjection))
            {
                model.Draw(gameTime);

                /*
                int bone = model.GetBone("R_Thumb2");
                
                if (bone >= 0)
                {
                    Matrix mx = model.GetBoneTransform(bone);

                    Vector3 v = BaseGame.Singleton.GraphicsDevice.Viewport.Project(
                        mx.Translation, BaseGame.Singleton.Projection,
                        BaseGame.Singleton.View, Matrix.Identity);

                    Text.DrawString("Hehe, Im here", 16, new Vector2(v.X, v.Y), Color.White);
                }
                 */
            }
        }

        /// <summary>
        /// Tests whether the object occupies the specified point.
        /// </summary>
        /// <param name="point">Point to be tested in world space</param>
        /// <returns></returns>
        public virtual bool Intersects(Vector3 point)
        {
            // Performs an axis aligned bounding box intersection test
            return BoundingBox.Contains(point) == ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the object intersects the specified ray.
        /// </summary>
        /// <param name="ray">Ray to be tested in world space</param>
        /// <returns></returns>
        public virtual float? Intersects(Ray ray)
        {
            // Performs an axis aligned bounding box intersection test
            return ray.Intersects(model.BoundingBox);
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
                hand.Drag(this);
        }

        /// <summary>
        /// Called when this entity is being carried by a hand
        /// </summary>
        public virtual void Follow(Hand hand)
        {
            Position = hand.Position;
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
            mouseBeginDropRotation = Rotation;
            mouseBeginDropPosition = Input.MousePosition;
            mouseBeginDropPosition.Y -= 10;
            return true; 
        }

        /// <summary>
        /// Called when the user is dropping the entity
        /// </summary>
        public virtual void Dropping(Hand hand, Entity entity, bool leftButton)
        {
            Rotation = mouseBeginDropRotation + MathHelper.PiOver2 + (float)Math.Atan2(
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
            //if (!Place(world.Landscape))
            //{
            //    world.Destroy(this);
            //}
            return true;
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
        #region Field

        /// <summary>
        /// Agent state machine
        /// </summary>
        public StateMachine<BaseAgent> StateMachine
        {
            get { return stateMachine; }
        }

        protected StateMachine<BaseAgent> stateMachine;

        /// <summary>
        /// Map from animation type to model clip name
        /// </summary>
        protected IDictionary<string, string> clipMap = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets max health of the agent
        /// </summary>
        public virtual float MaxHealth
        {
            get { return maxHealth; }

            set
            {
                maxHealth = (value > 0 ? value : 0);
                if (health > 0)
                    health = maxHealth;
            }
        }

        float maxHealth = 100;

        /// <summary>
        /// Gets or sets the health of the building
        /// </summary>
        public virtual float Health
        {
            get { return health; }

            set
            {
                if (value < 0)
                    health = 0;
                else if (value > maxHealth)
                    health = maxHealth;
                else
                    health = value;
            }
        }

        float health = 100;

        #endregion

        #region Methods
        
        public BaseAgent(GameWorld world)
            : base(world)
        {
            stateMachine = new StateMachine<BaseAgent>(this);
        }

        /// <summary>
        /// Play the animation of a given type.
        /// </summary>
        /// <param name="type">
        /// Example types: Idle, Walk, Run, Attack...
        /// </param>
        /// <remarks>
        /// This method differs from Model.PlayAnimation in that type is fixed 
        /// and used all over the game.
        /// E.g., a animation of type "Walk" may have the name "Take 001" in the model.
        /// </remarks>
        public void PlayAnimation(string type)
        {
            string clip;

            if (clipMap.TryGetValue(type, out clip))
            {
                Model.PlayAnimation(clip);
            }
        }

        /// <summary>
        /// Deserialized attributes: Health, MaxHealth, ClipWalk, ClipRun
        /// </summary>
        /// <param name="xml"></param>
        public override void Deserialize(XmlElement xml)
        {
            if (xml.HasAttribute("ClipWalk"))
                clipMap.Add("Walk", xml.GetAttribute("ClipWalk"));

            if (xml.HasAttribute("ClipRun"))
                clipMap.Add("Run", xml.GetAttribute("ClipRun"));

            float.TryParse(xml.GetAttribute("Health"), out health);
            float.TryParse(xml.GetAttribute("MaxHealth"), out maxHealth);

            if (health > maxHealth)
            {
                if (maxHealth >= 0)
                    health = maxHealth;
                else
                    maxHealth = health;
            }

            base.Deserialize(xml);
        }

        /// <summary>
        /// Called when the agent is dragged by the hand
        /// </summary>
        public override bool BeginDrag(Hand hand)
        {
            // Agents can't be dragged
            return false;
        }

        /// <summary>
        /// Called when the agent is dropped by the hand
        /// </summary>
        public override bool BeginDrop(Hand hand, Entity entity, bool leftButton)
        {
            // Agents can't be dropped
            return false;
        }
        
        /// <summary>
        /// Move the agent to the specific location
        /// </summary>
        public bool MoveTo(Vector3 location)
        {
            // Change agent to move state
            stateMachine.ChangeState(new StateMove(world, location));
            return true;
        }

        /// <summary>
        /// Move the agent towards the specific entity
        /// </summary>
        public bool MoveTo(Entity target)
        {

            return true;
        }

        /// <summary>
        /// Attack the specific entity
        /// </summary>
        public bool Attack(Entity target)
        {
            // StateSequential<T> may came to help here.
            // For the attack state, it contains two sequential states:
            // E.g., StateMoveToTarget, StateAttackTarget.
            // StateSequential will execute StateAttackTarget immediately
            // StateMoveToTarget has finished (near enough to the target)
            //
            // Example code:
            //
            //      StateSequential<BaseAgent> sequential = new StateSequential<BaseAgent>();
            //      sequential.Add(new StateMoveToTarget());
            //      sequential.Add(new StateAttackTarget());
            //      stateMachine.ChangeState(sequential);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Move and attack the agent the specific location
        /// </summary>
        public bool AttackTo(Vector3 location)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update the agent. If the agent is selected, handle mouse events.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Move the agent
            if (Selected && Input.MouseRightButtonJustPressed)
            {
                Input.SuppressMouseEvent();

                Entity picked = world.Pick();

                if (picked != null)
                {
                    MoveTo(picked);
                }
                else
                {
                    Vector3? location = world.Landscape.Pick();

                    if (location.HasValue)
                        MoveTo(location.Value);
                }
            }

            // Update state machine
            stateMachine.Update(gameTime);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            // Draw state machine
            stateMachine.Draw(gameTime);

            base.Draw(gameTime);
        }
        #endregion
    }
    #endregion
}

//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    #region IState
    public enum StateResult
    {
        Active, Inactive, Completed, Failed,
    }

    public interface IState : IEventListener
    {
        void Terminate();

        /// <summary>
        /// Perform any updates for this state
        /// </summary>
        /// <returns>
        /// True indicates keeping the state alive
        /// </returns>
        StateResult Update(GameTime gameTime);

        /// <summary>
        /// Perform any drawings for this state
        /// </summary>
        void Draw(GameTime gameTime);
    }

    #endregion

    #region General Purpose States
    /// <summary>
    /// Base state
    /// </summary>
    public abstract class BaseState : IState
    {
        protected StateResult State = StateResult.Inactive;

        public abstract void Activate();
        public abstract void Terminate();

        protected void ActivateIfInactive()
        {
            if (State == StateResult.Inactive)
            {
                Activate();
                State = StateResult.Active;
            }
        }

        public virtual void Draw(GameTime gameTime)
        {
        }

        public virtual StateResult Update(GameTime gameTime)
        {
            return StateResult.Completed;
        }

        public virtual EventResult HandleEvent(EventType type, object sender, object tag)
        {
            return EventResult.Unhandled;
        }
    }


    /// <summary>
    /// Represents a composite game state.
    /// All child states will be executed.
    /// </summary>
    public class StateComposite : BaseState
    {
        public override void Activate() { }
        public override void Terminate() { }

        protected LinkedList<IState> SubStates = new LinkedList<IState>();

        /// <remarks>
        /// Should be called before attaching this state to a state machine
        /// </remarks>
        public void Add(IState state)
        {
            SubStates.AddLast(state);
        }

        /// <summary>
        /// Clear all sub states
        /// </summary>
        public void Clear()
        {
            SubStates.Clear();
        }

        /// <summary>
        /// Update the composite state
        /// </summary>
        /// <remarks>
        /// If one of the substates failed, the whole composite state failed.
        /// If all of the substates completed, the whole composite state complete.
        /// </remarks>
        public override StateResult Update(GameTime gameTime)
        {
            ActivateIfInactive();

            LinkedListNode<IState> current = SubStates.First;

            StateResult compositeResult = StateResult.Completed;

            while (current != null)
            {
                StateResult result = current.Value.Update(gameTime);

                if (result == StateResult.Failed)
                {
                    // The whole state fails
                    SubStates.Clear();
                    return StateResult.Failed;
                }

                if (result != StateResult.Completed)
                {
                    // The whole state continues
                    LinkedListNode<IState> next = current.Next;
                    SubStates.Remove(current);
                    current = next;
                    compositeResult = StateResult.Active;
                }

                // Move on to the next state
                current = current.Next;
            }

            return compositeResult;
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (IState state in SubStates)
                state.Draw(gameTime);
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            // Pass to all sub states
            foreach (IState state in SubStates)
                if (state.HandleEvent(type, state, tag) == EventResult.Handled)
                    return EventResult.Handled;

            return EventResult.Unhandled;
        }
    }


    /// <summary>
    /// Represents a sequence of game states.
    /// Swith to the next state once the current state has completed
    /// </summary>
    public class StateSequential : BaseState
    {
        public override void Activate() { }
        public override void Terminate() { }

        protected LinkedList<IState> SubStates = new LinkedList<IState>();

        /// <remarks>
        /// Should be called before attaching this state to a state machine
        /// </remarks>
        public void Add(IState state)
        {
            SubStates.AddLast(state);
        }

        public void Clear()
        {
            SubStates.Clear();
        }

        /// <remarks>
        /// Derived classes must call base.Update first in their update functions.
        /// </remarks>
        public override StateResult Update(GameTime gameTime)
        {
            ActivateIfInactive();

            LinkedListNode<IState> current = SubStates.First;

            if (current != null)
            {
                StateResult result = current.Value.Update(gameTime);

                if (result == StateResult.Failed)
                {
                    // The whole state failed
                    SubStates.Clear();
                    return StateResult.Failed;
                }

                if (result == StateResult.Completed)
                {
                    // Switch to the next state
                    SubStates.Remove(current);
                    return SubStates.Count > 0 ? StateResult.Active :
                                                 StateResult.Completed;
                }

                // The current state is still active
                return StateResult.Active;
            }

            return StateResult.Completed;
        }

        public override void Draw(GameTime gameTime)
        {
            // Only draw the first state
            if (SubStates.First != null)
                SubStates.First.Value.Draw(gameTime);
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            // Only pass to the first state
            if (SubStates.First != null)
                return SubStates.First.Value.HandleEvent(type, sender, tag);
            return EventResult.Unhandled;
        }
    }
    #endregion

    #region BaseEntity
    /// <summary>
    /// Base world object
    /// </summary>
    public abstract class BaseEntity : IWorldObject, IAudioEmitter, IEventListener
    {
        #region Field

        public static int EntityCount = 0;

        /// <summary>
        /// Game world
        /// </summary>
        public GameWorld World
        {
            get { return world; }
        }

        GameWorld world;

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
        /// Used for 3D sound.
        /// </summary>
        public virtual Vector3 Forward
        {
            get { return Vector3.UnitZ; }
        }


        /// <summary>
        /// Gets or sets the orientation of this entity.
        /// Used for 3D sound.
        /// </summary>
        public Vector3 Up
        {
            get { return Vector3.UnitZ; }
        }


        /// <summary>
        /// Gets or sets how fast this entity is moving.
        /// Used for 3D sound.
        /// </summary>
        public virtual Vector3 Velocity
        {
            get { return Vector3.Zero; }
            set { }
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
        /// Gets or sets scene manager data
        /// </summary>
        public object SceneManagerTag
        {
            get { return sceneManagerTag; }
            set { sceneManagerTag = value; }
        }

        object sceneManagerTag;

        public virtual BoundingBox BoundingBox
        {
            get { return new BoundingBox(); }
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
        /// Called when this entity is been add to the world
        /// </summary>
        public virtual void OnCreate() { }

        /// <summary>
        /// Called when this entity is been destroyed
        /// </summary>
        public virtual void OnDestroy() { }

        /// <summary>
        /// Draw the scene object to a shadow map
        /// </summary>
        public virtual void DrawShadowMap(GameTime gameTime, ShadowEffect shadow)
        {
            // Nothing is drawed
        }

        /// <summary>
        /// Draw the scene object to a reflection map
        /// </summary>
        public virtual void DrawReflection(GameTime gameTime, Matrix view, Matrix projection)
        {
            // Nothing is drawed
        }

        /// <summary>
        /// Make the entity fall on the ground
        /// </summary>
        public void Fall()
        {
            Position = new Vector3(Position.X,
                                   Position.Y,
                                   World.Landscape.GetHeight(Position.X, Position.Y));
        }

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

        /// <summary>
        /// Handle events
        /// </summary>
        public virtual EventResult HandleEvent(EventType type, object sender, object tag)
        {
            return EventResult.Unhandled;
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
        /// Gets or sets the state of the agent
        /// </summary>
        public IState State
        {
            get { return state; }

            set
            {
                IState resultState = null;

                if (OnStateChanged(value, ref resultState))
                {
                    if (state != null)
                        state.Terminate();
                    state = resultState;
                }
            }
        }

        IState state;

        protected virtual bool OnStateChanged(IState newState, ref IState resultState) { return true; }

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
            }
        }

        GameModel model;


        /// <summary>
        /// Gets or sets whether this entity is visible
        /// </summary>
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }

        bool visible = true;


        /// <summary>
        /// Gets or sets whether the entity is within the view frustum
        /// </summary>
        public bool WithinViewFrustum
        {
            get { return withinViewFrustum; }
        }

        bool withinViewFrustum;


        /// <summary>
        /// Gets or sets entity position.
        /// </summary>
        public override Vector3 Position
        {
            get { return base.Position; }
            set { MarkDirty(); base.Position = value; }
        }


        /// <summary>
        /// Gets or sets entity rotation
        /// </summary>
        public Quaternion Rotation
        {
            get { return rotation; }
            set { MarkDirty(); rotation = value; }
        }

        Quaternion rotation = Quaternion.Identity;


        /// <summary>
        /// Gets or sets entity scale
        /// </summary>
        public Vector3 Scale
        {
            get { return scale; }
            set { MarkDirty(); scale = value; }
        }

        Vector3 scale = Vector3.One;


        /// <summary>
        /// Gets or sets the bias of model transform
        /// </summary>
        public Matrix TransformBias
        {
            get { return transformBias; }
            set { MarkDirty(); transformBias = value; }
        }

        Matrix transformBias = Matrix.Identity;

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
                        Matrix.CreateScale(scale) *
                        Matrix.CreateFromQuaternion(rotation) *
                        Matrix.CreateTranslation(Position);

                    isTransformDirty = false;
                }

                return transform;
            }
        }

        Matrix transform = Matrix.Identity;


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
            get
            {
                if (model == null)
                    return new BoundingBox();

                if (isDirty)
                    model.Transform = transformBias * Transform;

                return model.BoundingBox;
            }
        }

        /// <summary>
        /// Gets the size of the entity
        /// </summary>
        public virtual Vector3 Size
        {
            get { return BoundingBox.Max - BoundingBox.Min; }
        }
        
        /// <summary>
        /// Gets entity outline
        /// </summary>
        public Outline Outline
        {
            get
            {
                if (isDirty)
                    UpdateOutline(outline);

                return outline; 
            }
        }

        /// <summary>
        /// Used by derived classes to setup their outline.
        /// By default, the outline of an entity is a circle.
        /// </summary>
        protected virtual void UpdateOutline(Outline outline)
        {
            // Radius of the outline should not be changed
            outline.SetCircle(new Vector2(Position.X, Position.Y), outline.Radius);
        }

        Outline outline = new Outline();

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
        /// Gets whether the entity is interactive. you can make an entity
        /// interactive by calling GameWorld.Activate();
        /// </summary>
        public virtual bool IsInteractive
        {
            get { return true; }
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
            // Note assign to the upper case Model
            Model = model;
        }

        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);

            // Make entity fall on the ground
            Fall();

            string value = "";

            // Treat game model as level content
            if ((value = xml.GetAttribute("Model")) != "")
                Model = new GameModel(World.Content.Load<Model>(value));
            
            Vector3 scaleBias = Vector3.One;
            Vector3 translation = Vector3.Zero;
            float rotationX = 0, rotationY = 0, rotationZ = 0;

            // Get entity transform bias
            if ((value = xml.GetAttribute("RotationXBias")) != "")
                rotationX = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("RotationYBias")) != "")
                rotationY = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("RotationZBias")) != "")
                rotationZ = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("ScaleBias")) != "")
                scaleBias = Helper.StringToVector3(value);

            if ((value = xml.GetAttribute("PositionBias")) != "")
                translation = Helper.StringToVector3(value);

            if (scaleBias != Vector3.One || rotationX != 0 || rotationY != 0 || rotationZ != 0 ||
                translation != Vector3.Zero)
            {
                transformBias = Matrix.CreateScale(scaleBias) *
                                Matrix.CreateRotationX(rotationX) *
                                Matrix.CreateRotationY(rotationY) *
                                Matrix.CreateRotationZ(rotationZ) *
                                Matrix.CreateTranslation(translation);

                // Update model transform
                model.Transform = transformBias;

                // Center game model
                Vector3 center = (model.BoundingBox.Max + model.BoundingBox.Min) / 2;
                transformBias.M41 -= center.X;
                transformBias.M42 -= center.Y;
                transformBias.M43 -= model.BoundingBox.Min.Z;
                transformBias.M43 -= 0.2f;  // A little offset under the ground
            }

            // Get entity transform
            rotationX = rotationY = rotationZ = 0;

            if ((value = xml.GetAttribute("RotationX")) != "")
                rotationX = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("RotationY")) != "")
                rotationY = MathHelper.ToRadians(float.Parse(value));

            if ((value = xml.GetAttribute("RotationZ")) != "")
                rotationZ = MathHelper.ToRadians(float.Parse(value));

            if (rotationX != 0 || rotationX != 0 || rotationZ != 0)
            {
                Rotation = Quaternion.CreateFromRotationMatrix(
                                    Matrix.CreateRotationX(rotationX) *
                                    Matrix.CreateRotationY(rotationY) *
                                    Matrix.CreateRotationZ(rotationZ));
            }

            if ((value = xml.GetAttribute("Rotation")) != "")
                Rotation = Helper.StringToQuaternion(value);

            if ((value = xml.GetAttribute("Scale")) != "")
                Scale = Helper.StringToVector3(value);

            // Deserialize is probably always called during initialization,
            // so calculate outline radius at this time.
            outline.SetCircle(Vector2.Zero, (Size.X + Size.Y) / 4);
            UpdateOutline(outline);
        }
        
        public override void Serialize(XmlElement xml)
        {
            base.Serialize(xml);

            if (scale != Vector3.One)
                xml.SetAttribute("Scale", Helper.Vector3Tostring(scale));
            if (rotation != Quaternion.Identity)
                xml.SetAttribute("Rotation", Helper.QuaternionTostring(rotation));
        }

        /// <summary>
        /// Test to see if this entity is visible from a given view and projection
        /// </summary>
        /// <returns></returns>
        public virtual bool IsVisible(Matrix viewProjection)
        {
            // Transform position to projection space
            BoundingFrustum f = new BoundingFrustum(viewProjection);

            if (f.Intersects(BoundingBox))
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

        public override void Update(GameTime gameTime)
        {
            // Update current state
            if (state != null)
            {
                StateResult result = state.Update(gameTime);

                if (result == StateResult.Completed ||
                    result == StateResult.Failed)
                {
                    State = null;
                }
            }

            if (isDirty)
            {
                // Update model transform, this will update the bounding box
                model.Transform = transformBias * Transform;

                // Update outline
                UpdateOutline(outline);
            }

            if (model != null)
                model.Update(gameTime);

            withinViewFrustum = IsVisible(BaseGame.Singleton.ViewProjection);
        }

        public override void Draw(GameTime gameTime)
        {
            if (visible)
            {
                // Draw current state
                if (state != null)
                    state.Draw(gameTime);

                if (model != null && withinViewFrustum)
                {
                    //model.Tint = highlighted ? new Vector4(0, 0, 0, 1) : Vector4.One;
                    model.Draw(gameTime);
                }
            }
        }

        public override void DrawShadowMap(GameTime gameTime, ShadowEffect shadow)
        {
            if (visible && model != null && IsVisible(shadow.ViewProjection))
                model.DrawShadowMap(gameTime, shadow);
        }

        public override void DrawReflection(GameTime gameTime, Matrix view, Matrix projection)
        {
            if (visible && model != null && IsVisible(view * projection))
                model.Draw(gameTime);
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (state != null &&
                state.HandleEvent(type, sender, tag) == EventResult.Handled)
                return EventResult.Handled;

            return base.HandleEvent(type, sender, tag);
        }

        /// <summary>
        /// Tests whether the object occupies the specified point.
        /// </summary>
        /// <param name="point">Point to be tested in world space</param>
        /// <returns></returns>
        public virtual bool Intersects(Vector3 point)
        {
            // Performs an axis aligned bounding box intersection test
            return visible && BoundingBox.Contains(point) == ContainmentType.Contains;
        }

        /// <summary>
        /// Tests whether the object intersects the specified ray.
        /// </summary>
        /// <param name="ray">Ray to be tested in world space</param>
        /// <returns></returns>
        public virtual float? Intersects(Ray ray)
        {
            // Performs an axis aligned bounding box intersection test
            return visible ? model.Intersects(ray) : null;
        }

        /// <summary>
        /// Tests whether the object intersects the specified frustum.
        /// </summary>
        public virtual bool Intersects(BoundingFrustum frustum)
        {
            return visible && frustum.Contains(Position) == ContainmentType.Contains;
        }
        #endregion
    }

    #endregion
}

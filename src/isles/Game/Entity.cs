// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Isles;

public enum StateResult
{
    Active,
    Inactive,
    Completed,
    Failed,
}

public abstract class BaseState : IEventListener
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

    public virtual StateResult Update(GameTime gameTime)
    {
        return StateResult.Completed;
    }

    public virtual EventResult HandleEvent(EventType type, object sender, object tag)
    {
        return EventResult.Unhandled;
    }
}

public abstract class BaseEntity : IAudioEmitter, IEventListener
{
    public static int EntityCount;

    public GameWorld World { get; }

    /// <summary>
    /// Gets or sets the 3D position of the entity.
    /// </summary>
    public virtual Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets which way the entity is facing.
    /// Used for 3D sound.
    /// </summary>
    public virtual Vector3 Forward => Vector3.UnitZ;

    /// <summary>
    /// Gets or sets the orientation of this entity.
    /// Used for 3D sound.
    /// </summary>
    public Vector3 Up => Vector3.UnitZ;

    /// <summary>
    /// Gets or sets how fast this entity is moving.
    /// Used for 3D sound.
    /// </summary>
    public virtual Vector3 Velocity => Vector3.Zero;

    public virtual BoundingBox BoundingBox => new();

    /// <summary>
    /// Gets or sets entity name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the class ID of this world object.
    /// </summary>
    public string ClassID { get; set; }

    public BaseEntity(GameWorld world)
    {
        World = world;
        Name = "Entity " + EntityCount++;
    }

    public virtual void Update(GameTime gameTime) { }

    public virtual void OnCreate() { }

    public virtual void OnDestroy() { }

    /// <summary>
    /// Make the entity fall on the ground.
    /// </summary>
    public void Fall()
    {
        Position = new(Position.X, Position.Y, World.Landscape.GetHeight(Position.X, Position.Y));
    }

    public virtual void Deserialize(XmlElement xml)
    {
        string value;

        if (xml.HasAttribute("Name"))
        {
            Name = xml.GetAttribute("Name");
        }

        if ((value = xml.GetAttribute("Position")) != "")
        {
            // Note this should be the upper case Position!!!
            Position = Helper.StringToVector3(value);
        }
    }

    public virtual EventResult HandleEvent(EventType type, object sender, object tag)
    {
        return EventResult.Unhandled;
    }
}

/// <summary>
/// Base class for all pickable game entities.
/// </summary>
public abstract class Entity : BaseEntity
{
    public BaseState State
    {
        get => state;
        set
        {
            BaseState resultState = null;

            if (OnStateChanged(value, ref resultState))
            {
                if (state != null)
                {
                    state.Terminate();
                }

                state = resultState;
            }
        }
    }

    private BaseState state;

    protected virtual bool OnStateChanged(BaseState newState, ref BaseState resultState) => true;

    public GameModel Model
    {
        get => model;

        set
        {
            MarkDirty();
            model = value;
        }
    }

    private GameModel model;

    /// <summary>
    /// Gets or sets whether this entity is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    public bool WithinViewFrustum { get; private set; }

    public override Vector3 Position
    {
        get => base.Position;
        set
        {
            MarkDirty();
            base.Position = value;
        }
    }

    public Quaternion Rotation
    {
        get => rotation;
        set
        {
            MarkDirty();
            rotation = value;
        }
    }

    private Quaternion rotation = Quaternion.Identity;

    public Vector3 Scale
    {
        get => scale;
        set
        {
            MarkDirty();
            scale = value;
        }
    }

    private Vector3 scale = Vector3.One;
    private Matrix transformBias = Matrix.Identity;

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

    private Matrix transform = Matrix.Identity;

    private bool isDirty = true;
    private bool isTransformDirty = true;

    /// <summary>
    /// Mark both bounding box and transform.
    /// </summary>
    private void MarkDirty()
    {
        isDirty = true;
        isTransformDirty = true;
    }

    /// <summary>
    /// Returns the axis aligned bounding box of the game model.
    /// </summary>
    public override BoundingBox BoundingBox
    {
        get
        {
            if (model == null)
            {
                return new BoundingBox();
            }

            if (isDirty)
            {
                model.Transform = transformBias * Transform;
            }

            return model.BoundingBox;
        }
    }

    /// <summary>
    /// Gets the size of the entity.
    /// </summary>
    public virtual Vector3 Size => BoundingBox.Max - BoundingBox.Min;

    public Outline Outline
    {
        get
        {
            if (isDirty)
            {
                UpdateOutline(outline);
            }

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

    private readonly Outline outline = new();

    public virtual bool IsPickable => Visible;

    public Entity(GameWorld world)
        : base(world)
    {
    }

    public override void Deserialize(XmlElement xml)
    {
        base.Deserialize(xml);

        // Make entity fall on the ground
        Fall();

        string value;

        // Treat game model as level content
        if ((value = xml.GetAttribute("Model")) != "")
        {
            Model = new GameModel(value);
        }

        if ((value = xml.GetAttribute("Alpha")) != "")
        {
            Model.Alpha = float.Parse(value);
        }

        Vector3 scaleBias = Vector3.One;
        Vector3 translation = Vector3.Zero;
        float rotationX = 0, rotationY = 0, rotationZ = 0;

        // Get entity transform bias
        if ((value = xml.GetAttribute("RotationXBias")) != "")
        {
            rotationX = MathHelper.ToRadians(float.Parse(value));
        }

        if ((value = xml.GetAttribute("RotationYBias")) != "")
        {
            rotationY = MathHelper.ToRadians(float.Parse(value));
        }

        if ((value = xml.GetAttribute("RotationZBias")) != "")
        {
            rotationZ = MathHelper.ToRadians(float.Parse(value));
        }

        if ((value = xml.GetAttribute("ScaleBias")) != "")
        {
            scaleBias = Helper.StringToVector3(value);
        }

        if ((value = xml.GetAttribute("PositionBias")) != "")
        {
            translation = Helper.StringToVector3(value);
        }

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
        {
            rotationX = MathHelper.ToRadians(float.Parse(value));
        }

        if ((value = xml.GetAttribute("RotationY")) != "")
        {
            rotationY = MathHelper.ToRadians(float.Parse(value));
        }

        if ((value = xml.GetAttribute("RotationZ")) != "")
        {
            rotationZ = MathHelper.ToRadians(float.Parse(value));
        }

        if (rotationX != 0 || rotationX != 0 || rotationZ != 0)
        {
            Rotation = Quaternion.CreateFromRotationMatrix(
                                Matrix.CreateRotationX(rotationX) *
                                Matrix.CreateRotationY(rotationY) *
                                Matrix.CreateRotationZ(rotationZ));
        }

        if ((value = xml.GetAttribute("Rotation")) != "")
        {
            Rotation = Helper.StringToQuaternion(value);
        }

        if ((value = xml.GetAttribute("Scale")) != "")
        {
            Scale = Helper.StringToVector3(value);
        }

        // Deserialize is probably always called during initialization,
        // so calculate outline radius at this time.
        outline.SetCircle(Vector2.Zero, (Size.X + Size.Y) / 4);
        UpdateOutline(outline);
    }

    /// <summary>
    /// Test to see if this entity is visible from a given view and projection.
    /// </summary>
    public virtual bool IsVisible(Matrix viewProjection)
    {
        // Transform position to projection space
        var f = new BoundingFrustum(viewProjection);

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
        {
            model.Update(gameTime);
        }

        WithinViewFrustum = IsVisible(BaseGame.Singleton.ViewProjection);
    }

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        return state != null &&
            state.HandleEvent(type, sender, tag) == EventResult.Handled
            ? EventResult.Handled
            : base.HandleEvent(type, sender, tag);
    }

    public virtual bool Intersects(BoundingFrustum frustum)
    {
        return Visible && frustum.Contains(Position) == ContainmentType.Contains;
    }
}

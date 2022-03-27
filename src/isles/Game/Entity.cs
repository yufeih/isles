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

    public virtual BoundingBox BoundingBox => new();

    /// <summary>
    /// Gets or sets entity name.
    /// </summary>
    public string Name { get; set; } = "Entity " + EntityCount++;

    /// <summary>
    /// Gets or sets the class ID of this world object.
    /// </summary>
    public string ClassID { get; set; }

    public BaseEntity(GameWorld world)
    {
        World = world;
    }

    public virtual void Update(GameTime gameTime) { }

    public virtual void OnCreate() { }

    public virtual void OnDestroy() { }

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

    public GameModel Model { get; set; }

    public virtual bool Visible => true;

    public bool WithinViewFrustum { get; private set; }

    public Quaternion Rotation { get; set;} = Quaternion.Identity;

    public Vector3 Scale { get; set; } = Vector3.One;

    private Matrix transformBias = Matrix.Identity;

    /// <summary>
    /// Returns the axis aligned bounding box of the game model.
    /// </summary>
    public override BoundingBox BoundingBox
    {
        get
        {
            if (Model == null)
            {
                return new BoundingBox();
            }

            return Model.BoundingBox;
        }
    }

    public Outline Outline
    {
        get
        {
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

    private readonly Outline outline = new();

    public virtual bool IsPickable => Visible;

    public Entity(GameWorld world, GameModel model)
        : base(world)
    {
        Model = model;
    }

    public override void Deserialize(XmlElement xml)
    {
        base.Deserialize(xml);

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
            Model.Transform = transformBias;

            // Center game model
            Vector3 center = (Model.BoundingBox.Max + Model.BoundingBox.Min) / 2;
            transformBias.M41 -= center.X;
            transformBias.M42 -= center.Y;
            transformBias.M43 -= Model.BoundingBox.Min.Z;
            transformBias.M43 -= 0.2f;  // A little offset under the ground
        }

        // Get entity transform
        if ((value = xml.GetAttribute("Rotation")) != "")
        {
            Rotation = Helper.StringToQuaternion(value);
        }

        if ((value = xml.GetAttribute("Scale")) != "")
        {
            Scale = Helper.StringToVector3(value);
        }
    }

    /// <summary>
    /// Test to see if this entity is visible from a given view and projection.
    /// </summary>
    private bool IsVisible(Matrix viewProjection)
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

        Model.Transform = transformBias *
            Matrix.CreateScale(Scale) *
            Matrix.CreateFromQuaternion(Rotation) *
            Matrix.CreateTranslation(Position);

        Model.Update(gameTime);

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

// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Text.Json.Serialization;

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

public abstract class BaseEntity : IAudioEmitter, IEventListener, IJsonOnDeserialized
{
    public static int EntityCount;

    public GameWorld World => GameWorld.Singleton;

    /// <summary>
    /// Gets or sets the 3D position of the entity.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets entity name.
    /// </summary>
    public string Name { get; set; } = "Entity " + EntityCount++;

    /// <summary>
    /// Gets or sets the class ID of this world object.
    /// </summary>
    public string ClassID { get; set; }

    public BaseEntity()
    {
        ClassID = GetType().Name;
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

    public virtual void OnDeserialized() { }
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

    public string Model { get; set; }

    public float Alpha { get; set; } = 1;

    public Vector3 ScaleBias { get; set; } = Vector3.One;

    public float RotationXBias { get; set; }

    public float RotationYBias { get; set; }

    public float RotationZBias { get; set; }

    public GameModel GameModel { get; set; }

    public virtual bool Visible => true;

    public bool WithinViewFrustum { get; private set; }

    public Quaternion Rotation { get; set;} = Quaternion.Identity;

    public Vector3 Scale { get; set; } = Vector3.One;

    private Matrix transformBias = Matrix.Identity;

    /// <summary>
    /// Returns the axis aligned bounding box of the game model.
    /// </summary>
    public BoundingBox BoundingBox
    {
        get
        {
            if (GameModel == null)
            {
                return new BoundingBox();
            }

            return GameModel.BoundingBox;
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

    public override void Deserialize(XmlElement xml)
    {
        base.Deserialize(xml);

        string value;

        // Treat game model as level content
        if ((value = xml.GetAttribute("Model")) != "")
        {
            Model = value;
        }

        if ((value = xml.GetAttribute("Alpha")) != "")
        {
            Alpha = float.Parse(value);
        }

        // Get entity transform bias
        if ((value = xml.GetAttribute("RotationXBias")) != "")
        {
            RotationXBias = float.Parse(value);
        }

        if ((value = xml.GetAttribute("RotationYBias")) != "")
        {
            RotationYBias = float.Parse(value);
        }

        if ((value = xml.GetAttribute("RotationZBias")) != "")
        {
            RotationZBias = float.Parse(value);
        }

        if ((value = xml.GetAttribute("ScaleBias")) != "")
        {
            ScaleBias = Helper.StringToVector3(value);
        }

        transformBias = Matrix.CreateScale(ScaleBias) *
                        Matrix.CreateRotationX(MathHelper.ToRadians(RotationXBias) + MathHelper.PiOver2) *
                        Matrix.CreateRotationY(MathHelper.ToRadians(RotationYBias)) *
                        Matrix.CreateRotationZ(MathHelper.ToRadians(RotationZBias));

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

    public override void OnDeserialized()
    {
        base.OnDeserialized();

        if (Model != null)
        {
            GameModel = new(Model);

            // Update model transform
            GameModel.Transform = transformBias;
            GameModel.Alpha = Alpha;

            // Center game model
            Vector3 center = (GameModel.BoundingBox.Max + GameModel.BoundingBox.Min) / 2;
            transformBias.M41 -= center.X;
            transformBias.M42 -= center.Y;
            transformBias.M43 -= GameModel.BoundingBox.Min.Z;
            transformBias.M43 -= 0.2f;  // A little offset under the ground
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

        GameModel.Transform = transformBias *
            Matrix.CreateScale(Scale) *
            Matrix.CreateFromQuaternion(Rotation) *
            Matrix.CreateTranslation(Position);

        GameModel.Update(gameTime);

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

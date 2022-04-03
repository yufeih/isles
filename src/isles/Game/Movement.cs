// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

/// <summary>
/// Interface for the set of movement states to interact with
/// moving entities.
/// </summary>
public interface IMovable : IEventListener
{
    /// <summary>
    /// Gets or sets the position of the moving entity.
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// Gets the path brush of the moving entity.
    /// </summary>
    PathBrush Brush { get; }

    /// <summary>
    /// Gets or sets the facing of the moving entity.
    /// </summary>
    Vector3 Facing { get; set; }

    /// <summary>
    /// Gets the moving speed of the moving entity.
    /// </summary>
    float Speed { get; }

    /// <summary>
    /// Gets whether dynamic obstacles are ignored when moving.
    /// </summary>
    bool IgnoreDynamicObstacles { get; }

    /// <summary>
    /// Gets or sets the tag used for movement.
    /// </summary>
    List<Point> PathMarks { get; set; }
}

/// <summary>
/// Move the entity to a target moving entity.
/// </summary>
public class StateMoveToTarget : BaseState
{
    public float FollowDistance = 30;
    private const float SensorDistance = 10;
    private readonly IMovable owner;
    private readonly BaseEntity target;
    private readonly PathManager pathManager;
    private StateMoveToPosition move;
    private Vector2 lastPosition;
    private readonly float priority;
    private double reactivateTimer;

    public StateMoveToTarget(IMovable owner, BaseEntity target,
                             float priority, PathManager pathManager)
    {
        if (owner == null || target == null || pathManager == null)
        {
            throw new ArgumentNullException();
        }

        this.owner = owner;
        this.target = target;
        this.priority = priority;
        this.pathManager = pathManager;
    }

    public override void Activate()
    {
        if (reactivateTimer <= 0)
        {
            lastPosition.X = target.Position.X;
            lastPosition.Y = target.Position.Y;

            move = new StateMoveToPosition(lastPosition, owner, priority, pathManager);

            reactivateTimer = 2;
        }
    }

    public override void Terminate()
    {
        if (move != null)
        {
            move.Terminate();
        }
    }

    public override StateResult Update(GameTime gameTime)
    {
        ActivateIfInactive();

        if (reactivateTimer > 0)
        {
            reactivateTimer -= gameTime.ElapsedGameTime.TotalSeconds;
        }

        // Checks if the target has changed its position
        Vector2 distance;

        distance.X = target.Position.X - lastPosition.X;
        distance.Y = target.Position.Y - lastPosition.Y;

        if (distance.LengthSquared() > SensorDistance * SensorDistance)
        {
            distance.X = target.Position.X - owner.Position.X;
            distance.Y = target.Position.Y - owner.Position.Y;

            // Checks if we have reached the target
            if (distance.LengthSquared() > FollowDistance * FollowDistance)
            {
                Activate();
            }
        }

        // Update move
        if (move != null)
        {
            StateResult result = move.Update(gameTime);

            distance.X = target.Position.X - owner.Position.X;
            distance.Y = target.Position.Y - owner.Position.Y;

            // Checks if we have reached the target
            if (distance.LengthSquared() < FollowDistance * FollowDistance)
            {
                move.Terminate();
                move = null;

                if (target is not IMovable)
                {
                    return StateResult.Completed;
                }
            }

            // Stop following
            else if (result == StateResult.Completed || result == StateResult.Failed)
            {
                move.Terminate();
                move = null;

                if (result == StateResult.Failed)
                {
                    return State = StateResult.Failed;
                }
            }
        }
        else
        {
            owner.Facing = target.Position - owner.Position;
        }

        return StateResult.Active;
    }
}

public class StateMoveToPosition : BaseState
{
    private readonly IMovable owner;
    private readonly PathManager pathManager;
    private Vector2 originalDestination;
    private Vector2 destination;
    private StateSeekToPosition seek;
    private GraphPath path;
    private LinkedListNode<GraphPathEdge> currentEdge;
    private bool includeDynamic;
    private bool pathQuerying;
    private int retryCounter;
    private readonly int maxRetryTimes = 20;
    private readonly float priority;

    public StateMoveToPosition(Vector2 target, IMovable owner,
                                      float priority, PathManager pathManager)
    {
        if (null == owner || null == pathManager)
        {
            throw new ArgumentNullException();
        }

        this.priority = priority;
        originalDestination = target;
        destination = target;
        this.owner = owner;
        this.pathManager = pathManager;
    }

    public override void Terminate()
    {
        if (seek != null)
        {
            seek.Terminate();
        }

        if (destination.X != owner.Position.X &&
            destination.Y != owner.Position.Y)
        {
            owner.Facing = new Vector3(destination, 0) - owner.Position;
        }

        // FIXME: If this terminate is not called by our user, we won't be
        // able to cancel path query.
        pathManager.CancelQuery(owner);
    }

    public override void Activate()
    {
        // Adjust destination
        Vector2 start;

        start.X = owner.Position.X;
        start.Y = owner.Position.Y;

        // Ignore dynamic obstacles on the first try
        destination = pathManager.FindNextValidPosition(originalDestination, start, destination, owner);

        // Query a path if we can't directly walk through
        if (!pathQuerying && !pathManager.CanMoveBetween(start, destination,
                               owner, includeDynamic && (!owner.IgnoreDynamicObstacles)))
        {
            pathQuerying = true;
            pathManager.QueryPath(this, start, destination, priority, owner);
        }

        // else
        {
            // Seek to destination before the path is found
            seek = new StateSeekToPosition(destination, owner, pathManager)
            {
                WaitTime = 0.1f * retryCounter,
            };
        }

        includeDynamic = true;
    }

    public override StateResult Update(GameTime gameTime)
    {
        ActivateIfInactive();

        if (seek == null)
        {
            return State;
        }

        StateResult result = seek.Update(gameTime);

        if (result == StateResult.Failed)
        {
            seek.Terminate();

            if (retryCounter++ > maxRetryTimes)
            {
                return StateResult.Failed;
            }

            // Reactivate
            State = StateResult.Inactive;
        }
        else if (result == StateResult.Completed)
        {
            // If we are traversing the path and the end is not reached
            if (path != null && currentEdge != null && currentEdge.Next != null)
            {
                currentEdge = currentEdge.Next;

                seek = new StateSeekToPosition(currentEdge.Value.Position, owner, pathManager)
                {
                    WaitTime = 0.1f * retryCounter,
                };

                return State = StateResult.Active;
            }

            seek.Terminate();
            return State = StateResult.Completed;
        }

        return State;
    }

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        if (type == EventType.PathFound)
        {
            pathQuerying = false;

            path = tag as GraphPath;

            if (path == null || path.Edges == null || path.Edges.Count < 1)
            {
                throw new InvalidOperationException();
            }

            currentEdge = path.Edges.First;

            seek = new StateSeekToPosition(currentEdge.Value.Position, owner, pathManager)
            {
                WaitTime = 0.1f * retryCounter,
            };

            return EventResult.Handled;
        }
        else if (type == EventType.PathNotFound)
        {
            pathQuerying = false;

            // Reactivate
            State = (retryCounter > maxRetryTimes) ? StateResult.Failed :
                                                     StateResult.Inactive;
            return EventResult.Handled;
        }

        return EventResult.Unhandled;
    }
}

public class StateSeekToPosition : BaseState
{
    public float WaitTime;
    private readonly IMovable owner;
    private readonly PathManager pathManager;
    private Vector2 destination;
    private float elapsedWaitTime;
    private float totalWaitTime = MaxTotalWaitTime;
    private const float MaxTotalWaitTime = 0.2f;
    private const float MinTotalWaitTime = 0.05f;

    public override void Activate()
    {
    }

    public override void Terminate()
    {
    }

    public StateSeekToPosition(Vector2 target, IMovable owner, PathManager pathManager)
    {
        this.owner = owner;
        this.pathManager = pathManager;
        destination = target;
    }

    public override StateResult Update(GameTime gameTime)
    {
        ActivateIfInactive();

        var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // We don't want to make the entity moving too physically
        // Moves the agent towards the target.
        Vector3 facing;
        facing.X = destination.X - owner.Position.X;
        facing.Y = destination.Y - owner.Position.Y;
        facing.Z = 0;

        if (facing.X == 0 && facing.Y == 0)
        {
            return StateResult.Completed;
        }

        facing.Normalize();

        // Set entity facing
        owner.Facing = facing;

        // Update position
        Vector2 toTarget;
        Vector3 increment = facing * elapsedSeconds * owner.Speed;
        Vector3 target = owner.Position + increment;
        toTarget.X = destination.X - owner.Position.X;
        toTarget.Y = destination.Y - owner.Position.Y;

        if (increment.X * increment.X + increment.Y * increment.Y > toTarget.LengthSquared())
        {
            State = StateResult.Completed;
            target.X = destination.X;
            target.Y = destination.Y;
        }

        // Check for obstacles, wait if there's any obstacle on the way
        if (pathManager.CanBePlacedAt(target.X, target.Y, owner))
        {
            // Move to the target
            elapsedWaitTime = 0;

            // Move one step forward
            target.Z = pathManager.Landscape.GetHeight(target.X, target.Y);

            owner.Position = target;
            pathManager.UpdateMovable(owner);
        }
        else
        {
            // Wait for a random amout of time
            if (elapsedWaitTime == 0)
            {
                totalWaitTime = Helper.RandomInRange(MinTotalWaitTime, WaitTime + MaxTotalWaitTime);
                // if (totalWaitTime > 0.2f)
                //    totalWaitTime = 0.2f;
            }

            return totalWaitTime < (elapsedWaitTime += elapsedSeconds) ? (State = StateResult.Failed) : (State = StateResult.Active);
        }

        return State;
    }
}

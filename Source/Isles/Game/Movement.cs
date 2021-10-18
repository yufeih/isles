//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Engine
{
    #region Movement

    #region Interfaces
    /// <summary>
    /// Interface for the set of movement states to interact with
    /// moving entities.
    /// </summary>
    public interface IMovable : IEventListener
    {
        /// <summary>
        /// Gets or sets the position of the moving entity
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// Gets the path brush of the moving entity
        /// </summary>
        PathBrush Brush { get; }

        /// <summary>
        /// Gets or sets the facing of the moving entity
        /// </summary>
        Vector3 Facing { get; set; }

        /// <summary>
        /// Gets the moving speed of the moving entity
        /// </summary>
        float Speed { get; }

        /// <summary>
        /// Gets whether dynamic obstacles are ignored when moving
        /// </summary>
        bool IgnoreDynamicObstacles { get; }

        /// <summary>
        /// Gets or sets the tag used for movement
        /// </summary>
        object MovementTag { get; set; }
    }
    #endregion

    #region StateMoveToTarget
    /// <summary>
    /// Move the entity to a target moving entity
    /// </summary>
    public class StateMoveToTarget : BaseState
    {
        public float FollowDistance = 30;
        const float SensorDistance = 10;

        IMovable owner;
        IWorldObject target;
        PathManager pathManager;
        StateMoveToPosition move;
        Vector2 lastPosition;
        float priority;
        Random random = new Random();
        double reactivateTimer = 0;

        public StateMoveToTarget(IMovable owner, IWorldObject target,
                                 float priority, PathManager pathManager)
        {
            if (owner == null || target == null || pathManager == null)
                throw new ArgumentNullException();

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
                move.Terminate();
        }

        public override StateResult Update(GameTime gameTime)
        {
            ActivateIfInactive();

            if (reactivateTimer > 0)
                reactivateTimer -= gameTime.ElapsedGameTime.TotalSeconds;

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

                    if (!(target is IMovable))
                        return StateResult.Completed;
                }

                // Stop following
                else if (result == StateResult.Completed || result == StateResult.Failed)
                {
                    move.Terminate();
                    move = null;

                    if (result == StateResult.Failed)
                        return State = StateResult.Failed;
                }
            }
            else
            {
                owner.Facing = target.Position - owner.Position;
            }

            return StateResult.Active;
        }
    }
    #endregion

    #region StateMoveToPosition
    public class StateMoveToPosition : BaseState
    {
        IMovable owner;
        Vector2 destination;
        PathManager pathManager;
        Path path;
        LinkedListNode<PathEdge> currentEdge;
        StateMoveToPositionPrecise move;
        float distanceThreshold;
        float priority;

        public StateMoveToPosition(Vector2 target, IMovable owner, float priority, PathManager pathManager)
        {
            if (owner == null || pathManager == null)
                throw new ArgumentNullException();

            this.priority = priority;
            this.owner = owner;
            this.destination = target;
            this.pathManager = pathManager;
        }

        public override void Activate()
        {
            Vector2 start = new Vector2(owner.Position.X, owner.Position.Y);
            Vector2 toDestination = destination - start;

            float minSearchDistance = PathManager.AreaSize * pathManager.Graph.CellSize * 0.6f;

            if (toDestination.LengthSquared() > minSearchDistance * minSearchDistance)
            {
                // Query a large path
                path = pathManager.QueryPath(start, destination);

                if (path == null || path.Edges.Count <= 0)
                {
                    move = null;
                    State = StateResult.Failed;
                    return;
                }

                currentEdge = path.Edges.First;

                // Smooth the last path edge
                LinkedListNode<PathEdge> last = path.Edges.Last;

                if (last.Previous != null &&
                    pathManager.CanMoveBetween(last.Value.Position, last.Value.Position,
                                               owner, false))
                {
                    path.Edges.Remove(last.Previous);
                }

                // Smooth the first path edge
                if (currentEdge.Next != null &&
                    pathManager.CanMoveBetween(new Vector2(owner.Position.X, owner.Position.Y),
                                                   currentEdge.Next.Value.Position, owner, false))
                {
                    currentEdge = currentEdge.Next;
                }

                distanceThreshold = Helper.RandomInRange(0.5f, 0.7f) * pathManager.LargeGraph.CellSize;

                move = new StateMoveToPositionPrecise(
                    currentEdge.Value.Position, owner, priority, pathManager);
            }
            else
            {
                move = new StateMoveToPositionPrecise(
                    destination, owner, priority, pathManager);
            }
        }

        public override void Terminate()
        {
            if (move != null)
                move.Terminate();
        }

        public override StateResult Update(GameTime gameTime)
        {
            ActivateIfInactive();

            if (move == null)
                return State;

            //pathManager.DrawPath(path);

            StateResult result = move.Update(gameTime);

            if (result == StateResult.Failed)
                return StateResult.Failed;

            // Smoothing on the large graph
            bool stepNext = false;

            if (currentEdge != null && currentEdge.Next != null)
            {
                Vector2 position;
                position.X = owner.Position.X;
                position.Y = owner.Position.Y;

                if (Vector2.Subtract(position, currentEdge.Value.Position).LengthSquared()
                    <= distanceThreshold * distanceThreshold)
                {
                    stepNext = true;
                    distanceThreshold = Helper.RandomInRange(0.3f, 0.7f) * pathManager.LargeGraph.CellSize;
                }
            }
            
            if (result == StateResult.Completed || stepNext)
            {
                // If we are traversing the path and the end is not reached
                if (path != null && currentEdge != null && currentEdge.Next != null)
                {
                    currentEdge = currentEdge.Next;

                    move = new StateMoveToPositionPrecise(
                        currentEdge.Value.Position, owner, priority, pathManager);

                    move.Terminate();
                    return StateResult.Active;
                }

                return StateResult.Completed;
            }

            return StateResult.Active;
        }
    }
    #endregion

    #region StateMoveToPositionPrecise
    public class StateMoveToPositionPrecise : BaseState
    {
        IMovable owner;
        PathManager pathManager;
        Vector2 originalDestination;
        Vector2 destination;
        StateSeekToPosition seek;
        Path path;
        LinkedListNode<PathEdge> currentEdge;
        bool includeDynamic = false;
        bool pathQuerying = false;
        int retryCounter = 0;
        int maxRetryTimes = 20;
        float priority;

        public StateMoveToPositionPrecise(Vector2 target, IMovable owner,
                                          float priority, PathManager pathManager)
        {
            if (null == owner || null == pathManager)
                throw new ArgumentNullException();

            this.priority = priority;
            this.originalDestination = target;
            this.destination = target;
            this.owner = owner;
            this.pathManager = pathManager;
        }

        public override void Terminate()
        {
            if (seek != null)
                seek.Terminate();

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
            destination = pathManager.FindNextValidPosition(originalDestination, 
                                    start, destination, owner, !owner.IgnoreDynamicObstacles);

            // Query a path if we can't directly walk through
            if (!pathQuerying && !pathManager.CanMoveBetween(start, destination,
                                   owner, includeDynamic && (!owner.IgnoreDynamicObstacles)))
            {
                pathQuerying = true;
                pathManager.QueryPath(this, start, destination, priority, owner);
            }
            //else
            {
                // Seek to destination before the path is found
                seek = new StateSeekToPosition(destination, owner, pathManager);
                seek.WaitTime = 0.1f * retryCounter;
            }
            
            includeDynamic = true;
        }

        public override StateResult Update(GameTime gameTime)
        {
            ActivateIfInactive();

            if (seek == null)
                return State;

            //pathManager.DrawPath(path);

            StateResult result = seek.Update(gameTime);

            //BaseGame game = BaseGame.Singleton;
            //Point pos = game.Project(owner.Position);
            //game.Graphics2D.DrawString(retryCounter.ToString(), 14,
            //    new Vector2(pos.X, pos.Y), Color.Blue);

            //BaseGame game = BaseGame.Singleton;
            //Point p = game.Project(owner.Position);
            //game.Graphics2D.DrawLine(p, game.Project(new Vector3(destination,
            //    pathManager.Landscape.GetHeight(destination.X, destination.Y))), Color.White);

            //if (owner is Entity)
            //    (owner as Entity).Model.Tint = (pathQuerying ? new Vector4(0, 0, 0, 1) : Vector4.One);

            if (result == StateResult.Failed)
            {
                seek.Terminate();

                if (retryCounter++ > maxRetryTimes)
                    return StateResult.Failed;

                // Reactivate
                State = StateResult.Inactive;
            }
            else if (result == StateResult.Completed)
            {
                // If we are traversing the path and the end is not reached
                if (path != null && currentEdge != null && currentEdge.Next != null)
                {
                    currentEdge = currentEdge.Next;

                    seek = new StateSeekToPosition(currentEdge.Value.Position, owner, pathManager);
                    seek.WaitTime = 0.1f * retryCounter;

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

                path = tag as Path;

                if (path == null || path.Edges == null || path.Edges.Count < 1)
                    throw new InvalidOperationException();

                currentEdge = path.Edges.First;

                seek = new StateSeekToPosition(currentEdge.Value.Position, owner, pathManager);
                seek.WaitTime = 0.1f * retryCounter;

                //LinkedListNode<PathEdge> edge = path.Edges.First;
                //while (edge != null && edge.Next != null)
                //{
                //    System.Diagnostics.Debug.Assert(
                //        pathManager.CanMoveBetween(edge.Value.Position,
                //                                   edge.Next.Value.Position, owner, true));

                //    edge = edge.Next;
                //}

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
    #endregion

    #region StateSeekToPosition
    public class StateSeekToPosition : BaseState
    {
        public float WaitTime = 0;
        IMovable owner;
        PathManager pathManager;
        Vector2 destination;
        float elapsedWaitTime = 0;
        float totalWaitTime = MaxTotalWaitTime;
        const float MaxSeperation = 0.2f;
        const float MaxTotalWaitTime = 0.2f;
        const float MinTotalWaitTime = 0.05f;

        public override void Activate()
        {
            //Event.SendMessage(EventType.BeginMove, owner, this, null);
        }

        public override void Terminate()
        {
            //Event.SendMessage(EventType.EndMove, owner, this, null);
        }

        public StateSeekToPosition(Vector2 target, IMovable owner, PathManager pathManager)
        {
            if (null == owner || null == pathManager)
                throw new ArgumentNullException();

            this.owner = owner;
            this.pathManager = pathManager;
            this.destination = target;
        }

        public override StateResult Update(GameTime gameTime)
        {
            ActivateIfInactive();

            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // We don't want to make the entity moving too physically
            // Moves the agent towards the target.
            Vector3 facing;
            facing.X = destination.X - owner.Position.X;
            facing.Y = destination.Y - owner.Position.Y;
            facing.Z = 0;

            if (facing.X == 0 && facing.Y == 0)
                return StateResult.Completed;
                
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
                    //if (totalWaitTime > 0.2f)
                    //    totalWaitTime = 0.2f;
                }

                if (totalWaitTime < (elapsedWaitTime += elapsedSeconds))
                    return State = StateResult.Failed;

                return State = StateResult.Active;
            }

            return State;
        }
    }
    #endregion

    #endregion
}

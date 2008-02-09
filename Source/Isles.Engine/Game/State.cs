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

    #region IState

    /// <summary>
    /// Represents a game state to form a finite state machine.
    /// IState is managed by a state machine. Basically, every
    /// kind of state transitions can be represented by a finite
    /// state machine.
    /// </summary>
    public interface IState<T>
    {
        /// <summary>
        /// Called when this state is been activated
        /// </summary>
        void Enter(T owner);

        /// <summary>
        /// Called when this state is been deactivated
        /// </summary>
        void Exit(T owner);

        /// <summary>
        /// Perform any updates for this state
        /// </summary>
        /// <returns>
        /// True indicates keeping the state alive
        /// </returns>
        bool Update(T owner, GameTime gameTime);

        /// <summary>
        /// Perform any drawings for thsi state
        /// </summary>
        void Draw(T owner, GameTime gameTime);
    }

    #endregion

    #region StateMachine

    /// <summary>
    /// A Finite State Machine
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StateMachine<T>
    {
        /// <summary>
        /// Owner of the state machine
        /// </summary>
        protected T stateMachineOwner;

        /// <summary>
        /// Current game state
        /// </summary>
        protected IState<T> currentState;

        /// <summary>
        /// Previous game state
        /// </summary>
        protected IState<T> previousState;

        /// <summary>
        /// Gets or sets state machine owner
        /// </summary>
        public T Owner
        {
            get { return stateMachineOwner; }
            set { stateMachineOwner = Owner; }
        }

        /// <summary>
        /// Gets current game state
        /// </summary>
        public IState<T> State
        {
            get { return currentState; }
        }

        /// <summary>
        /// Gets previous game state
        /// </summary>
        public IState<T> PreviousState
        {
            get { return previousState; }
        }

        public StateMachine()
        {

        }

        public StateMachine(T owner)
        {
            stateMachineOwner = owner;
        }

        /// <summary>
        /// Change to a new game state
        /// </summary>
        /// <param name="newState"></param>
        public virtual void ChangeState(IState<T> newState)
        {
            if (newState != currentState)
            {
                if (currentState != null)
                    currentState.Exit(stateMachineOwner);

                previousState = currentState;
                currentState = newState;

                if (currentState != null)
                    currentState.Enter(stateMachineOwner);
            }
        }

        /// <summary>
        /// Change back to previous state
        /// </summary>
        public virtual void RevertState()
        {
            if (previousState != null)
                ChangeState(previousState);
        }

        /// <summary>
        /// Update the state machine
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (currentState != null)
            {
                if (!currentState.Update(stateMachineOwner, gameTime))
                {
                    ChangeState(null);
                }
            }
        }

        /// <summary>
        /// Perform any drawing functions
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            if (currentState != null)
                currentState.Draw(stateMachineOwner, gameTime);
        }
    }

    #endregion

    #region Generalized States

    /// <summary>
    /// Represents a composite game state.
    /// All child states will be executed.
    /// </summary>
    public class StateComposite<T> : IState<T>
    {
        protected List<IState<T>> states = new List<IState<T>>();

        /// <remarks>
        /// Should be called before attaching this state to a state machine
        /// </remarks>
        public void Add(IState<T> state)
        {
            states.Add(state);
        }

        public void Enter(T owner)
        {
            foreach (IState<T> state in states)
                state.Enter(owner);
        }

        public void Exit(T owner)
        {
            foreach (IState<T> state in states)
                state.Exit(owner);
        }

        public bool Update(T owner, GameTime gameTime)
        {
            bool alive = false;

            foreach (IState<T> state in states)
            {
                if (state.Update(owner, gameTime))
                    alive = true;
            }

            return alive;
        }

        public void Draw(T owner, GameTime gameTime)
        {
            foreach (IState<T> state in states)
                state.Draw(owner, gameTime);
        }
    }


    /// <summary>
    /// Represents a sequence of game states.
    /// Swith to the next state once the current state has done
    /// </summary>
    public class StateSequential<T> : IState<T>
    {
        protected Queue<IState<T>> states = new Queue<IState<T>>();

        /// <remarks>
        /// Should be called before attaching this state to a state machine
        /// </remarks>
        public void Add(IState<T> state)
        {
            states.Enqueue(state);
        }

        public void Clear()
        {
            states.Clear();
        }

        public void Enter(T owner)
        {
            if (states.Count > 0)
                states.Peek().Enter(owner);
        }

        public void Exit(T owner)
        {
            if (states.Count > 0)
                states.Peek().Exit(owner);
        }

        public bool Update(T owner, GameTime gameTime)
        {
            if (states.Count <= 0)
                return false;

            // If the current state is dead
            if (!states.Peek().Update(owner, gameTime))
            {
                states.Dequeue().Exit(owner);

                // Switch to next state
                if (states.Count > 0)
                    states.Peek().Enter(owner);
                else
                    return false;
            }

            return true;
        }

        public void Draw(T owner, GameTime gameTime)
        {
            if (states.Count > 0)
                states.Peek().Draw(owner, gameTime);
        }
    }

    #endregion

    #region AgentStates

    /// <summary>
    /// The agent is in a moving state
    /// </summary>
    public class StateMove : IState<BaseAgent>
    {
        GameWorld world;
        Vector3 destination;

        public StateMove(GameWorld world, Vector3 destination)
        {
            this.world = world;
            this.destination = destination;
                        
            destination.Z = // Fall the destination on the ground
                world.Landscape.GetHeight(destination.X, destination.Y);
        }

        public void Enter(BaseAgent owner)
        {
            // Change agent animation
            owner.PlayAnimation("Walk");
        }

        public void Exit(BaseAgent owner)
        {

        }

        public bool Update(BaseAgent owner, GameTime gameTime)
        {
            // This is a really simple one
            if (Vector3.Subtract(owner.Position, destination).LengthSquared() < 1)
                // Finished moving
                return false;

            // This is a really simple collision detection
            foreach (IWorldObject o in world.GetNearbyObjects(owner.BoundingBox))
            {
                Entity e =  o as Entity;

                // Make sure we are not testing with ourself
                if (e != owner && e != null &&
                    Outline.Intersects(e.Outline, owner.Outline) != ContainmentType.Disjoint)
                {
                    // A collision occured, destroy this agent
                    world.Destroy(owner);
                }
            }

            // Moves the agent towards the target.
            // Note that when set the position of an entity, its Z value is
            // automatically adjusted to fit the height of the landscape.
            owner.Position += Vector3.Normalize(destination - owner.Position);

            return true;
        }

        public void Draw(BaseAgent owner, GameTime gameTime)
        {

        }
    }

    #endregion
}

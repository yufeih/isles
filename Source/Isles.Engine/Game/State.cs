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
        /// <returns>
        /// Gets Whether the state has finished (or goal finished). 
        /// </returns>
        bool Finished { get; }

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
        void Update(T owner, GameTime gameTime);

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
    }

    #endregion

    #region Generalized States

    /// <summary>
    /// Represents a composite game state.
    /// All child states will be executed.
    /// </summary>
    public class StateComposite<T> : IState<T>
    {
        protected bool finished = false;
        protected List<IState<T>> states = new List<IState<T>>();

        public bool Finished
        {
            get { return finished; }
        }

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

        public void Update(T owner, GameTime gameTime)
        {
            finished = true;

            foreach (IState<T> state in states)
            {
                if (!state.Finished)
                {
                    state.Update(owner, gameTime);
                    finished = false;
                }
            }
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
        protected bool finished = false;
        protected Queue<IState<T>> states = new Queue<IState<T>>();

        public bool Finished
        {
            get { return finished; }
        }

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
            states.Peek().Enter(owner);
        }

        public void Exit(T owner)
        {
            states.Peek().Exit(owner);
        }

        public void Update(T owner, GameTime gameTime)
        {
            states.Peek().Update(owner, gameTime);

            if (states.Peek().Finished)
            {
                // Switch to next state
                if (states.Count > 0)
                {
                    states.Dequeue().Exit(owner);
                    states.Peek().Enter(owner);
                }
                else
                {
                    finished = true;
                }
            }
        }

        public void Draw(T owner, GameTime gameTime)
        {
            states.Peek().Draw(owner, gameTime);
        }
    }

    #endregion
}

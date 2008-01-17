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
    /// </summary>
    public interface IState<T>
    {
        void Enter(T owner);
        void Exit(T owner);
        void Update(T owner, GameTime gameTime);
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

}

// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    /// <summary>
    /// Add types of game event here.
    /// </summary>
    public enum EventType
    {
        Unknown,

        // Input events
        LeftButtonDown,
        LeftButtonUp,
        RightButtonDown,
        RightButtonUp,
        MiddleButtonDown,
        MiddleButtonUp,
        DoubleClick,
        Wheel,
        KeyDown,
        KeyUp,

        // Path service events
        PathFound,
        PathNotFound,
        PathMarkObstacles,
        BeginMove,
        EndMove,

        // Timer event
        TimerTick,

        // Game specific events
        Hit,
    }

    /// <summary>
    /// Only unhandled input events will be passed to the next listener.
    /// </summary>
    public enum EventResult
    {
        Handled,
        Unhandled,
    }

    /// <summary>
    /// Interface for any event receiver.
    /// Finally I decided to use the interface style event handling
    /// system instead of the native c# event. Because using c# event,
    /// you can't specify the order of listeners, nor are you able to
    /// Capture/Uncapture mouse event (Useful to garuentee a MouseUp
    /// event after MouseDown occurs).
    /// </summary>
    public interface IEventListener
    {
        EventResult HandleEvent(EventType type, object sender, object tag);
    }

    /// <summary>
    /// Class for dispatching events.
    /// </summary>
    public static class Event
    {
        private struct Message
        {
            public EventType Type;
            public IEventListener Receiver;
            public object Sender;
            public object Tag;
            public float Time;

            public Message(EventType type,
                           IEventListener receiver,
                           object sender,
                           object tag,
                           float delay)
            {
                Type = type;
                Receiver = receiver;
                Sender = sender;
                Tag = tag;
                Time = (float)BaseGame.Singleton
                                      .CurrentGameTime
                                      .TotalGameTime
                                      .TotalSeconds + delay;
            }
        }

        private static readonly LinkedList<Message> queue = new();

        /// <summary>
        /// Send a message to the receiver immediately.
        /// </summary>
        public static EventResult SendMessage(EventType type,
                                              IEventListener receiver,
                                              object sender,
                                              object tag)
        {
            return receiver == null ? EventResult.Unhandled : receiver.HandleEvent(type, sender, tag);
        }

        /// <summary>
        /// Send a delayed message to the receiver.
        /// </summary>
        public static void SendMessage(EventType type,
                                       IEventListener receiver,
                                       object sender,
                                       object tag,
                                       float delayTime)
        {
            if (delayTime < 0)
            {
                throw new ArgumentException();
            }

            if (receiver == null)
            {
                return;
            }

            var message = new Message(type, receiver, sender, tag, delayTime);

            // Add to the list
            LinkedListNode<Message> p = queue.First;

            while (p != null)
            {
                if (p.Value.Time > message.Time)
                {
                    queue.AddBefore(p, message);
                    return;
                }

                p = p.Next;
            }

            queue.AddLast(new Message(type, receiver, sender, tag, delayTime));
        }

        /// <summary>
        /// Update the event dispatcher.
        /// </summary>
        public static void Update(GameTime gameTime)
        {
            // Send pending messages
            LinkedListNode<Message> q;
            LinkedListNode<Message> p = queue.First;

            while (p != null &&
                   p.Value.Time < gameTime.TotalGameTime.TotalSeconds)
            {
                // Send the message
                SendMessage(p.Value.Type,
                            p.Value.Receiver,
                            p.Value.Sender,
                            p.Value.Tag);

                // Delete p
                q = p.Next;
                queue.Remove(p);
                p = q;
            }
        }
    }
}
// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Isles.Engine
{
    public static class Timer
    {
        private static readonly List<IEventListener> handlers = new();
        private static readonly List<float> intervals = new();
        private static readonly List<float> times = new();

        /// <summary>
        /// Adds a new timer alert.
        /// </summary>
        /// <param name="interval">Time interval measured in seconds.</param>
        public static void Add(IEventListener handler, float interval)
        {
            if (handler == null || interval <= 0)
            {
                throw new ArgumentException();
            }

            if (handlers.Contains(handler))
            {
                throw new InvalidOperationException();
            }

            handlers.Add(handler);
            intervals.Add(interval);
            times.Add(0);
        }

        /// <summary>
        /// Removes a timer alert.
        /// </summary>
        public static void Remove(IEventListener handler)
        {
            var i = handlers.FindIndex(item => item == handler);

            if (i >= 0 && i < handlers.Count)
            {
                handlers.RemoveAt(i);
                intervals.RemoveAt(i);
                times.RemoveAt(i);
            }
        }

        /// <summary>
        /// Update the timer and triggers all timer events.
        /// </summary>
        /// <param name="gameTime"></param>
        public static void Update(GameTime gameTime)
        {
            for (var i = 0; i < handlers.Count; i++)
            {
                times[i] += (float)gameTime.ElapsedGameTime.TotalSeconds;

                while (times[i] > intervals[i])
                {
                    times[i] -= intervals[i];
                    Event.SendMessage(EventType.TimerTick, handlers[i], null, null, 0);
                }
            }
        }
    }
}

// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

/// <summary>
/// Add types of game event here.
/// </summary>
public enum EventType
{
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
}

//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

#region Using directives
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
#endregion

namespace Isles.Engine
{
    public class Input
    {
        #region Field
        /// <summary>
        /// Mouse state, set every frame in the Update method.
        /// </summary>
        private MouseState mouseState =
            Microsoft.Xna.Framework.Input.Mouse.GetState(), mouseStateLastFrame;

        /// <summary>
        /// Keyboard state, set every frame in the Update method.
        /// Note: KeyboardState is a class and not a struct,
        /// we have to initialize it here, else we might run into trouble when
        /// accessing any keyboardState data before BaseGame.Update() is called.
        /// We can also NOT use the last state because everytime we call
        /// Keyboard.GetState() the old state is useless (see XNA help for more
        /// information, section Input). We store our own array of keys from
        /// the last frame for comparing stuff.
        /// </summary>
        private KeyboardState keyboardState = 
            Microsoft.Xna.Framework.Input.Keyboard.GetState();

        /// <summary>
        /// Keys pressed last frame, for comparison if a key was just pressed.
        /// </summary>
        private List<Keys> keysPressedLastFrame = new();

        /// <summary>
        /// Mouse wheel delta this frame. XNA does report only the total
        /// scroll value, but we usually need the current delta!
        /// </summary>
        /// <returns>0</returns>
        private int mouseWheelDelta = 0;
        private int mouseWheelValue = 0;

        /// <summary>
        /// Mouse position
        /// </summary>
        public Point MousePosition => new Point(mouseState.X, mouseState.Y);

        /// <summary>
        /// Gets current mouse state
        /// </summary>
        public MouseState Mouse => mouseState;

        /// <summary>
        /// Mouse wheel delta
        /// </summary>
        /// <returns>Int</returns>
        public int MouseWheelDelta => mouseWheelDelta;

        /// <summary>
        /// Mouse in box
        /// </summary>
        /// <param name="rect">Rectangle</param>
        /// <returns>Bool</returns>
        public bool MouseInBox(Rectangle rect)
        {
            return mouseState.X >= rect.X &&
                   mouseState.Y >= rect.Y &&
                   mouseState.X < rect.Right &&
                   mouseState.Y < rect.Bottom;
        }

        /// <summary>
        /// Keyboard
        /// </summary>
        /// <returns>Keyboard state</returns>
        public KeyboardState Keyboard => keyboardState;

        /// <summary>
        /// Gets whether left ALT or right ALT key is pressed
        /// </summary>
        public bool IsAltPressed => keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

        /// <summary>
        /// Gets whether left shift or right shift key is pressed
        /// </summary>
        public bool IsShiftPressed => keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        /// <summary>
        /// Gets whether left ctrl or right ctrl key is pressed
        /// </summary>
        public bool IsCtrlPressed => keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        #endregion

        #region Method
        public static bool IsSpecialKey(Keys key)
        {
            // All keys except A-Z, 0-9 and `-\[];',./= (and space) are special keys.
            // With shift pressed this also results in this keys:
            // ~_|{}:"<>? !@#$%^&*().
            var keyNum = (int)key;
            if ((keyNum >= (int)Keys.A && keyNum <= (int)Keys.Z) ||
                (keyNum >= (int)Keys.D0 && keyNum <= (int)Keys.D9) ||
                key == Keys.Space || // well, space ^^
                key == Keys.OemTilde || // `~
                key == Keys.OemMinus || // -_
                key == Keys.OemPipe || // \|
                key == Keys.OemOpenBrackets || // [{
                key == Keys.OemCloseBrackets || // ]}
                key == Keys.OemQuotes || // '"
                key == Keys.OemQuestion || // /?
                key == Keys.OemPlus) // =+
            {
                return false;
            }

            // Else is is a special key
            return true;
        }

        /// <summary>
        /// Key to char helper conversion method.
        /// Note: If the keys are mapped other than on a default QWERTY
        /// keyboard, this method will not work properly. Most keyboards
        /// will return the same for A-Z and 0-9, but the special keys
        /// might be different.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Char</returns>
        public static char KeyToChar(Keys key, bool shiftPressed)
        {
            // If key will not be found, just return space
            var ret = ' ';
            var keyNum = (int)key;
            if (keyNum >= (int)Keys.A && keyNum <= (int)Keys.Z)
            {
                if (shiftPressed)
                {
                    ret = key.ToString()[0];
                }
                else
                {
                    ret = key.ToString().ToLower()[0];
                }
            }
            else if (keyNum >= (int)Keys.D0 && keyNum <= (int)Keys.D9 &&
                shiftPressed == false)
            {
                ret = (char)((int)'0' + (keyNum - Keys.D0));
            }
            else if (key == Keys.D1 && shiftPressed)
            {
                ret = '!';
            }
            else if (key == Keys.D2 && shiftPressed)
            {
                ret = '@';
            }
            else if (key == Keys.D3 && shiftPressed)
            {
                ret = '#';
            }
            else if (key == Keys.D4 && shiftPressed)
            {
                ret = '$';
            }
            else if (key == Keys.D5 && shiftPressed)
            {
                ret = '%';
            }
            else if (key == Keys.D6 && shiftPressed)
            {
                ret = '^';
            }
            else if (key == Keys.D7 && shiftPressed)
            {
                ret = '&';
            }
            else if (key == Keys.D8 && shiftPressed)
            {
                ret = '*';
            }
            else if (key == Keys.D9 && shiftPressed)
            {
                ret = '(';
            }
            else if (key == Keys.D0 && shiftPressed)
            {
                ret = ')';
            }
            else if (key == Keys.OemTilde)
            {
                ret = shiftPressed ? '~' : '`';
            }
            else if (key == Keys.OemMinus)
            {
                ret = shiftPressed ? '_' : '-';
            }
            else if (key == Keys.OemPipe)
            {
                ret = shiftPressed ? '|' : '\\';
            }
            else if (key == Keys.OemOpenBrackets)
            {
                ret = shiftPressed ? '{' : '[';
            }
            else if (key == Keys.OemCloseBrackets)
            {
                ret = shiftPressed ? '}' : ']';
            }
            else if (key == Keys.OemSemicolon)
            {
                ret = shiftPressed ? ':' : ';';
            }
            else if (key == Keys.OemQuotes)
            {
                ret = shiftPressed ? '"' : '\'';
            }
            else if (key == Keys.OemComma)
            {
                ret = shiftPressed ? '<' : '.';
            }
            else if (key == Keys.OemPeriod)
            {
                ret = shiftPressed ? '>' : ',';
            }
            else if (key == Keys.OemQuestion)
            {
                ret = shiftPressed ? '?' : '/';
            }
            else if (key == Keys.OemPlus)
            {
                ret = shiftPressed ? '+' : '=';
            }

            // Return result
            return ret;
        }

        /// <summary>
        /// Handle keyboard input helper method to catch keyboard input
        /// for an input text. Only used to enter the player name in the game.
        /// </summary>
        /// <param name="inputText">Input text</param>
        public void HandleKeyboardInput(ref string inputText)
        {
            // Is a shift key pressed (we have to check both, left and right)
            var isShiftPressed =
                keyboardState.IsKeyDown(Keys.LeftShift) ||
                keyboardState.IsKeyDown(Keys.RightShift);

            // Go through all pressed keys
            foreach (Keys pressedKey in keyboardState.GetPressedKeys())
            {
                // Only process if it was not pressed last frame
                if (keysPressedLastFrame.Contains(pressedKey) == false)
                {
                    // No special key?
                    if (IsSpecialKey(pressedKey) == false &&
                        // Max. allow 32 chars
                        inputText.Length < 32)
                    {
                        // Then add the letter to our inputText.
                        // Check also the shift state!
                        inputText += KeyToChar(pressedKey, isShiftPressed);
                    }
                    else if (pressedKey == Keys.Back &&
                        inputText.Length > 0)
                    {
                        // Remove 1 character at end
                        inputText = inputText.Substring(0, inputText.Length - 1);
                    }
                }
            }
        }

        private struct Entry
        {
            public float Order;
            public IEventListener Handler;

            public Entry(IEventListener handler, float order)
            {
                Order = order;
                Handler = handler;
            }
        }

        private readonly LinkedList<Entry> handlers = new();

        /// <summary>
        /// Registers a new input event handler
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="order">
        /// Handlers with lower order will be notified first
        /// </param>
        public void Register(IEventListener handler, float order)
        {
            if (null == handler)
            {
                throw new ArgumentNullException();
            }

            // Check if this handler already exists in the list
            LinkedListNode<Entry> p = handlers.First;

            while (p != null)
            {
                if (p.Value.Handler == handler)
                {
                    Log.Write("Warning: Duplicate Input Handler.");
                    return;
                }

                p = p.Next;
            }

            // Add to the list
            p = handlers.First;

            while (p != null)
            {
                if (p.Value.Order > order)
                {
                    handlers.AddBefore(p, new Entry(handler, order));
                    return;
                }

                p = p.Next;
            }

            handlers.AddLast(new Entry(handler, order));
        }

        /// <summary>
        /// Removes an input event handler
        /// </summary>
        /// <param name="handler"></param>
        public void Unregister(IEventListener handler)
        {
            if (null == handler)
            {
                throw new ArgumentNullException();
            }

            LinkedListNode<Entry> p = handlers.First;

            while (p != null)
            {
                if (p.Value.Handler == handler)
                {
                    handlers.Remove(p);
                    break;
                }

                p = p.Next;
            }
        }

        /// <summary>
        /// Clear all handler registries
        /// </summary>
        public void Clear()
        {
            handlers.Clear();
        }

        private IEventListener captured;

        /// <summary>
        /// Capture input event. All input events will always (only) be 
        /// sent to the specified handler.
        /// </summary>
        public void Capture(IEventListener handler)
        {
            if (captured != null)
            {
                throw new InvalidOperationException();
            }

            captured = handler;
        }

        /// <summary>
        /// Release the capture of input events
        /// </summary>
        public void Uncapture()
        {
            captured = null;
        }

        #endregion

        #region Update
        /// <summary>
        /// Time interval for double click, measured in seconds
        /// </summary>
        public const float DoubleClickInterval = 0.25f;

        /// <summary>
        /// Flag for simulating double click
        /// </summary>
        private int doubleClickFlag = 0;
        private double doubleClickTime = 0;

        /// <summary>
        /// Update, called from BaseGame.Update().
        /// Will catch all new states for keyboard, mouse and the gamepad.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Handle mouse input variables
            mouseStateLastFrame = mouseState;
            mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            mouseWheelDelta = mouseState.ScrollWheelValue - mouseWheelValue;
            mouseWheelValue = mouseState.ScrollWheelValue;
                        
            // Handle keyboard input
            keysPressedLastFrame = new List<Keys>(keyboardState.GetPressedKeys());
            keyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();

            // Mouse wheel event
            if (mouseWheelDelta != 0)
            {
                TriggerEvent(EventType.Wheel, null);
            }

            // Mouse Left Button Events
            if (mouseState.LeftButton == ButtonState.Pressed &&
                mouseStateLastFrame.LeftButton == ButtonState.Released)
            {
                if (doubleClickFlag == 0)
                {
                    doubleClickFlag = 1;
                    doubleClickTime = gameTime.TotalGameTime.TotalSeconds;
                    TriggerEvent(EventType.LeftButtonDown, null);
                }
                else if (doubleClickFlag == 1)
                {
                    if ((gameTime.TotalGameTime.TotalSeconds - doubleClickTime) < DoubleClickInterval)
                    {
                        doubleClickFlag = 0;
                        TriggerEvent(EventType.DoubleClick, null);
                    }
                    else
                    {
                        doubleClickTime = gameTime.TotalGameTime.TotalSeconds;
                        TriggerEvent(EventType.LeftButtonDown, null);
                    }
                }
            }
            else if (mouseState.LeftButton == ButtonState.Released &&
                     mouseStateLastFrame.LeftButton == ButtonState.Pressed)
            {
                TriggerEvent(EventType.LeftButtonUp, null);
            }

            // Mouse Right Button Events
            if (mouseState.RightButton == ButtonState.Pressed &&
                mouseStateLastFrame.RightButton == ButtonState.Released)
            {
                TriggerEvent(EventType.RightButtonDown, null);
            }
            else if (mouseState.RightButton == ButtonState.Released &&
                     mouseStateLastFrame.RightButton == ButtonState.Pressed)
            {
                TriggerEvent(EventType.RightButtonUp, null);
            }

            // Mouse Middle Button Events
            if (mouseState.MiddleButton == ButtonState.Pressed &&
                mouseStateLastFrame.MiddleButton == ButtonState.Released)
            {
                TriggerEvent(EventType.MiddleButtonDown, null);
            }
            else if (mouseState.MiddleButton == ButtonState.Released &&
                     mouseStateLastFrame.MiddleButton == ButtonState.Pressed)
            {
                TriggerEvent(EventType.MiddleButtonUp, null);
            }

            // Key down events
            foreach (Keys key in keyboardState.GetPressedKeys())
            {
                if (!keysPressedLastFrame.Contains(key))
                {
                    TriggerEvent(EventType.KeyDown, key);
                }
            }

            // Key up events
            foreach (Keys key in keysPressedLastFrame)
            {
                var found = false;
                foreach (Keys keyCurrent in keyboardState.GetPressedKeys())
                {
                    if (keyCurrent == key)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    TriggerEvent(EventType.KeyUp, key);
                }
            }
        }

        private void TriggerEvent(EventType type, Keys? key)
        {
            // If we're captured, only send the event to
            // the hooker.
            if (captured != null)
            {
                captured.HandleEvent(type, this, key);
                return;
            }

            // Otherwise pump down through all handlers.
            // Stop when the event is handled.
            foreach (Entry entry in handlers)
            {
                if (entry.Handler.HandleEvent(type, this, key) == 
                    EventResult.Handled)
                {
                    break;
                }
            }
        }
        #endregion
    }
}

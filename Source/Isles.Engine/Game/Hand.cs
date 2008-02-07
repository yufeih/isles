using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;

namespace Isles.Engine
{
    #region HandState
    /// <summary>
    /// Hand state
    /// </summary>
    public enum HandState
    {
        /// <summary>
        /// Snap to ground, carrying nothing
        /// </summary>
        Idle,
        /// <summary>
        /// Dragging an entity
        /// </summary>
        Dragging,
        /// <summary>
        /// Carrying entities around after drag them
        /// </summary>
        Carrying,
        /// <summary>
        /// Dropping an entity
        /// </summary>
        Dropping,
        /// <summary>
        /// Ready to cast spells
        /// </summary>
        Cast,
        /// <summary>
        /// Being in the state of casting spells
        /// </summary>
        Casting
    }
    #endregion

    public class Hand : GameModel
    {
        #region Fields
        /// <summary>
        /// Hand state
        /// </summary>
        HandState state = HandState.Idle;

        /// <summary>
        /// Whether the hand is top most,
        /// or not occluded by the other 3D objects
        /// </summary>
        bool topMost = true;

        /// <summary>
        /// Used to smooth hand movement
        /// </summary>
        float distanceSmoother;

        /// <summary>
        /// used to smooth hand movement
        /// </summary>
        Vector3 positionSmoother;

        public const float MinDistanceAboveGround = 0;

        /// <summary>
        /// How high our hand are above ground
        /// </summary>
        float distanceAboveGround = MinDistanceAboveGround;

        /// <summary>
        /// Hand position in world space
        /// </summary>
        Vector3 position;

        /// <summary>
        /// Active entity
        /// </summary>
        Entity activeEntity;

        /// <summary>
        /// Active spell
        /// </summary>
        Spell activeSpell;

        /// <summary>
        /// Is the hand visible
        /// </summary>
        bool visible = true;

        /// <summary>
        /// Current power of the hand
        /// </summary>
        float power;

        /// <summary>
        /// Entities currently holds, use a stack for FILO
        /// </summary>
        Stack<Entity> entities = new Stack<Entity>();

        /// <summary>
        /// Gets or sets the max power of the hand
        /// </summary>
        public float MaxPower = 100.0f;

        /// <summary>
        /// Game world
        /// </summary>
        GameWorld world;

        /// <summary>
        /// Cursor position in 3D space
        /// </summary>
        Vector3 cursorPosition;

        /// <summary>
        /// Cursor radius from eye in 3D space
        /// </summary>
        float cursorDistance = 500.0f;

        /// <summary>
        /// Gets game cursor position in 3D space
        /// </summary>
        public Vector3 CursorPosition
        {
            get { return cursorPosition; }
        }

        /// <summary>
        /// Gets the distance from cursor to eye in 3D space
        /// </summary>
        public float CursorDistance
        {
            get { return cursorDistance; }
        }

        /// <summary>
        /// Gets current hand state
        /// </summary>
        public HandState State
        {
            get { return state; }
        }

        /// <summary>
        /// Gets the top most entity
        /// </summary>
        public Entity CurrentEntity
        {
            get { return activeEntity; }
        }

        /// <summary>
        /// Gets the current spell
        /// </summary>
        public Spell CurrentSpell
        {
            get { return activeSpell; }
        }

        /// <summary>
        /// Gets or sets whether the hand is top most
        /// </summary>
        public bool TopMost
        {
            get { return topMost; }
            set { topMost = value; }
        }

        /// <summary>
        /// Gets current power of the hand.
        /// Initially, the user will have to hold 1 second to gain 100 power
        /// </summary>
        public float Power
        {
            get { return power; }
        }

        /// <summary>
        /// Gets the entities currently holds
        /// </summary>
        public Stack<Entity> Entities
        {
            get { return entities; }
        }

        /// <summary>
        /// Gets the position of the hand in world space
        /// </summary>
        public Vector3 Position
        {
            get { return position; }
        }

        /// <summary>
        /// How high our hand are above ground
        /// </summary>
        public float DistanceAboveGround
        {
            get { return distanceAboveGround; }

            set
            {
                distanceAboveGround =
                    (value > MinDistanceAboveGround ? value : MinDistanceAboveGround);
            }
        }

        /// <summary>
        /// Gets whether the hand is empty
        /// </summary>
        public bool Idle
        {
            get { return state == HandState.Idle; }
        }
        #endregion

        #region Methods
        public Hand(GameWorld world)
        {
            this.world = world;
        }

        public Hand(GameWorld world, Model Model)
            : base(Model)
        {
            this.world = world;
        }

        /// <summary>
        /// Create a hand
        /// </summary>
        /// <param name="gameScreen"></param>
        public Hand(GameWorld world, string modelFilename)
            : base(BaseGame.Singleton.Content.Load<Model>(modelFilename))
        {
            this.world = world;
        }

        public Hand(GameWorld world, string modelFilename, Matrix transform)
            : base(BaseGame.Singleton.Content.Load<Model>(modelFilename))
        {
            this.world = world;

            Model.Root.Transform *= transform;
            Refresh();
        }

        /// <summary>
        /// Reset hand state
        /// </summary>
        public void Reset(GameWorld world)
        {
            this.world = world;

            state = HandState.Idle;
            topMost = true;
            distanceSmoother = 0;
            positionSmoother = position = Vector3.Zero;
            distanceAboveGround = MinDistanceAboveGround;
            activeEntity = null;
            activeSpell = null;
            visible = true;
            power = 0;
            entities.Clear();
            MaxPower = 100.0f;
        }

        /// <summary>
        /// Cast a spell
        /// </summary>
        /// <param name="spell"></param>
        public void Cast(Spell spell)
        {
            if (state == HandState.Idle)
            {
                if (spell.Trigger(this))
                {
                    state = HandState.Cast;

                    activeSpell = spell;
                }
            }
        }

        /// <summary>
        /// Stop current action to perform another action
        /// </summary>
        /// <returns></returns>
        public bool StopActions()
        {
            if (state == HandState.Cast)
                StopCast();

            return state == HandState.Idle;
        }

        /// <summary>
        /// Stops casting the spell
        /// </summary>
        /// <param name="spell"></param>
        public void StopCast()
        {
            state = HandState.Idle;

            activeSpell = null;
        }

        /// <summary>
        /// Gets the position from where spells are casted
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCastPosition()
        {
            //Ray ray = game.Unproject(
            //    game.ScreenWidth - 100,
            //    game.ScreenHeight - 100);

            //return ray.Position + ray.Direction * 20;
            return position;
        }

        /// <summary>
        /// Drag a game entity
        /// </summary>
        /// <param name="entity"></param>
        public bool Drag(Entity entity)
        {
            if (state != HandState.Idle || entity == null)
                return false;

            entities.Push(entity);

            // Change the game state to Carrying
            state = HandState.Carrying;

            // Set active entity
            activeEntity = entity;

            world.Select(null);
            world.Highlight(null);
            return true;
        }

        /// <summary>
        /// Drop the game entity being dragged
        /// </summary>
        /// <returns></returns>
        public Entity Drop()
        {
            if (state == HandState.Idle ||
                state == HandState.Dropping ||
                state == HandState.Carrying)
            {
                Entity entity = entities.Pop();

                if (entities.Count == 0)
                {
                    state = HandState.Idle;
                    activeEntity = null;
                }
                else
                {
                    state = HandState.Carrying;
                    activeEntity = entities.Peek();
                }

                return entity;
            }

            return null;
        }
        #endregion

        #region Update and Draw
        public override void Update(GameTime gameTime)
        {
            if (activeSpell != null)
                activeSpell.Update(gameTime);

            RestrictCursor();

            UpdateCursor();

            SmoothHandPosition(gameTime);

            UpdateState(gameTime);

            base.Update(gameTime);
        }

        #region UpdateState
        private void UpdateState(GameTime gameTime)
        {
            // Pick a game entity
            Entity pickedEntity = world.Pick();

            switch (state)
            {
                case HandState.Idle:

                    // Highlight picked entity
                    world.Highlight(pickedEntity);

                    // Left click to select an entity
                    if (Input.MouseLeftButtonJustPressed)
                    {
                        // Select the picked entity
                        world.Select(pickedEntity);

                        Input.SuppressMouseEvent();
                    }

                    // Right click to begin drag the picked entity
                    else if (pickedEntity != null && Input.MouseRightButtonJustPressed)
                    {
                        // Notify BeginDrag event
                        if (pickedEntity.BeginDrag(this))
                        {
                            // Reset hand power
                            power = 0;

                            // Set active entity (The entity being dragged)
                            activeEntity = pickedEntity;

                            // Deselect all
                            world.Select(null);

                            // Change to dragging state
                            state = HandState.Dragging;
                        }

                        Input.SuppressMouseEvent();
                    }
                    break;

                case HandState.Dragging:

                    // Notify EndDrag event
                    if (Input.MouseRightButtonJustReleased)
                    {
                        // Note when we finished dragging a game entity,
                        // a different entity may get dragged, e.g., dragging
                        // a crop out of a farmland.
                        //
                        // So entities should call Hand.Drag method in their
                        // EndDrag method to tell the hand which entity they
                        // wish to drag out, the entity itself or something else.
                        //
                        // As a consequence, hand state is reset to Idle first.
                        
                        state = HandState.Idle;

                        activeEntity.EndDrag(this);

                        // Reset drag power
                        power = 0;

                        Input.SuppressMouseEvent();
                    }
                    else
                    {
                        // Update drag power
                        power += gameTime.ElapsedGameTime.Milliseconds * 0.2f; // FIXME: make our hand powerful
                        if (power > MaxPower)
                            power = MaxPower;

                        // Nofity dragging event
                        activeEntity.Dragging(this);
                    }
                    break;

                case HandState.Carrying:
                    
                    // Notify BeginDrop event
                    if (Input.MouseLeftButtonJustPressed || Input.MouseRightButtonJustPressed)
                    {
                        // Pass in the picked entity as the drop target
                        if (activeEntity.BeginDrop(
                                this, pickedEntity, Input.MouseLeftButtonJustPressed))
                        {
                            // Change to dropping state
                            state = HandState.Dropping;
                        }

                        Input.SuppressMouseEvent();
                    }
                    else
                    {
                        // Notify carrying event to all entities
                        foreach (Entity entity in entities)
                            entity.Follow(this);
                    }
                    break;

                case HandState.Dropping:
                    
                    // Notify EndDrop event
                    if (Input.MouseLeftButtonJustReleased || Input.MouseRightButtonJustReleased)
                    {
                        if (activeEntity.EndDrop(this, pickedEntity, Input.MouseLeftButtonJustReleased))
                        {
                            Drop();
                        }
                        else
                        {
                            // Otherwise we're still carrying the same thing around
                            state = HandState.Carrying;
                        }

                        Input.SuppressMouseEvent();
                    }
                    else
                    {
                        // Notify dropping event
                        activeEntity.Dropping(this, pickedEntity, Input.MouseLeftButtonPressed);
                    }
                    break;

                case HandState.Cast:

                    // Notify cast event if mouse pressed
                    if (activeSpell != null &&
                       (Input.MouseLeftButtonJustPressed || Input.MouseRightButtonJustPressed))
                    {
                        if (activeSpell.BeginCast(this, Input.MouseLeftButtonJustPressed))
                        {
                            // Change to casting state
                            state = HandState.Casting;
                        }

                        Input.SuppressMouseEvent();
                    }
                    break;

                case HandState.Casting:

                    // Notify EndCast event
                    if (Input.MouseLeftButtonJustReleased || Input.MouseRightButtonJustReleased)
                    {
                        if (activeSpell.EndCast(this, Input.MouseLeftButtonJustReleased))
                        {
                            // If we want to keep on casting this spell
                            state = HandState.Cast;
                        }
                        else
                        {
                            // Otherwise stop casting the spell
                            StopCast();
                        }

                        Input.SuppressMouseEvent();
                    }
                    else
                    {
                        // Notify casting event
                        activeSpell.Casting(this, Input.MouseLeftButtonPressed);
                    }
                    break;
            }
        }
        #endregion

        private void SmoothHandPosition(GameTime gameTime)
        {
            if (state == HandState.Cast || state == HandState.Casting)
            {
                // Draw back the hand
                position = Vector3.Transform(new Vector3(15, -10, -60), game.ViewInverse);
                //positionSmoother = Vector3.Transform(new Vector3(50, -20, -200), game.ViewInverse);
                //position += (positionSmoother - position) * (float)(
                //        gameTime.ElapsedGameTime.TotalMilliseconds * 0.02f);
                //distanceSmoother = 100;
            }
            else
            {
                // Snap hand to landscape
                distanceSmoother += (cursorDistance - distanceSmoother) *
                        gameTime.ElapsedGameTime.Milliseconds * 0.01f;

                position = game.PickRay.Position + distanceSmoother * game.PickRay.Direction;

                // Make sure our hand is above ground
                float height = world.Landscape.GetHeight(position.X, position.Y);
                if (position.Z < height + distanceAboveGround)
                    position.Z = height + distanceAboveGround;
            }
        }

        private void UpdateCursor()
        {
            // Update game cursor position
            Nullable<Vector3> hitPoint = world.Landscape.Pick();
            if (hitPoint.HasValue)
            {
                cursorPosition = hitPoint.Value;
                cursorDistance = Vector3.Subtract(
                    hitPoint.Value, game.PickRay.Position).Length();
            }
            else
            {
                cursorPosition = game.PickRay.Position +
                                 game.PickRay.Direction * cursorDistance;
            }
        }

        #region Restrict Cursor
        private void RestrictCursor()
        {
            if (!BaseGame.Singleton.IsActive)
                return;

            const int BorderThickness = 5;

            bool resetCursor = false;
            Point cursor = Input.MousePosition;
            if (Input.MousePosition.X < BorderThickness)
            {
                resetCursor = true;
                cursor.X = BorderThickness;
            }
            else if (Input.MousePosition.Y < BorderThickness)
            {
                resetCursor = true;
                cursor.Y = BorderThickness;
            }
            else if (Input.MousePosition.X > game.ScreenWidth - BorderThickness)
            {
                resetCursor = true;
                cursor.X = game.ScreenWidth - BorderThickness;
            }
            else if (Input.MousePosition.Y > game.ScreenHeight - BorderThickness)
            {
                resetCursor = true;
                cursor.Y = game.ScreenHeight - BorderThickness;
            }

            if (resetCursor)
                Mouse.SetPosition(cursor.X, cursor.Y);
        }
        #endregion

        public override void Draw(GameTime gameTime, GameModel.BasicEffectSettings setupBasicEffect)
        {
            // Draw current spell
            if (activeSpell != null)
                activeSpell.Draw(gameTime);

            // To make our hand top most, clear the depth buffer before rendering it.
            // In addition, make sure the hand is drawed last
            if (topMost)
                game.GraphicsDevice.Clear(ClearOptions.DepthBuffer, Color.White, 1.0f, 0);

            game.GraphicsDevice.RenderState.DepthBufferEnable = true;
            
            // Make our hand face the cursor
            //if (state == HandState.Cast || state == HandState.Casting)
            //{
            //    Vector3 forward = Vector3.Normalize(screen.CursorPosition - position);

            //    world = Matrix.CreateFromAxisAngle(
            //                Vector3.Cross(-Vector3.UnitZ, forward),
            //                (float)Math.Acos((double)Vector3.Dot(-Vector3.UnitZ, forward))) *
            //            Matrix.CreateTranslation(Vector3.Transform(position, game.View));
            //}
            //else
            {
                // Update hand position
                Transform = Matrix.CreateTranslation(Vector3.Transform(position, game.View));            
            }

            if (!visible)
                return;

            if (player == null)
            {
                Model.CopyAbsoluteBoneTransformsTo(bones);

                // Draw plain mesh
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.World = bones[mesh.ParentBone.Index] * Transform;
                        effect.View = Matrix.Identity;
                        effect.Projection = game.Projection;
                        effect.EnableDefaultLighting();

                        if (setupBasicEffect != null)
                            setupBasicEffect(effect);
                    }

                    mesh.Draw();
                }
            }
            else
            {

                Matrix[] bones = player.GetSkinTransforms();

                // Render the skinned mesh.
                foreach (ModelMesh mesh in Model.Meshes)
                {
                    foreach (Effect effect in mesh.Effects)
                    {
                        effect.Parameters["Bones"].SetValue(bones);
                        effect.Parameters["View"].SetValue(Matrix.Identity);
                        effect.Parameters["Projection"].SetValue(game.Projection);
                    }

                    mesh.Draw();
                }
            }
        }
        #endregion
    }
}

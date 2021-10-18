//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Pipeline;
using Isles.Graphics;
using Isles.Engine;
using Isles.UI;


namespace Isles
{
    #region Charactor
    /// <summary>
    /// Base class for all game charactors
    /// </summary>
    public class Charactor : GameObject, IMovable
    {
        #region Field
        public EffectGlow Glow;
        public bool ShowGlow;
        public bool IsHero;
        public int Food;

        /// <summary>
        /// Gets or sets the speed of this charactor
        /// </summary>
        public virtual float Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        float speed;

        /// <summary>
        /// Used by the movement system
        /// </summary>
        public object MovementTag
        {
            get { return pathManagerTag; }
            set { pathManagerTag = value; }
        }

        object pathManagerTag;


        /// <summary>
        /// Gets or sets the radius of the charactor
        /// </summary>
        public float PathObstructorRadius
        {
            get { return pathObstructorRadius; }
            set { pathObstructorRadius = value; }
        }

        float pathObstructorRadius = 5;


        /// <summary>
        /// Gets or sets the facing of the charactor
        /// </summary>
        public Vector3 Facing
        {
            get { return new Vector3((float)Math.Cos(rotation), (float)Math.Sin(rotation), 0); }
            set { targetRotation = (float)(Math.Atan2(value.Y, value.X)); }
        }

        float rotation;
        float targetRotation;


        /// <summary>
        /// Gets or sets the path brush of the charactor
        /// </summary>
        public PathBrush Brush
        {
            get { return brush; }
            set { brush = value; }
        }

        PathBrush brush;

        /// <summary>
        /// Whether dynamic obstacles (E.g. units) are ignored when moving
        /// </summary>
        public bool IgnoreDynamicObstacles
        {
            get { return ignoreDynamicObstacles; }
            
            set
            {
                if (!value)
                {
                    // Make sure we don't stuck with each other
                    Vector2 spawnPoint;
                    spawnPoint.X = Position.X;
                    spawnPoint.Y = Position.Y;
                    spawnPoint = World.PathManager.FindValidPosition(spawnPoint, brush);
                    Position = new Vector3(spawnPoint, 0);
                    World.PathManager.UpdateMovable(this);
                    Fall();
                }

                ignoreDynamicObstacles = value;
            }
        
        }

        bool ignoreDynamicObstacles = false;

        /// <summary>
        /// Gets or sets the combat spell for this charactor
        /// </summary>
        public SpellCombat Combat;

        /// <summary>
        /// Gets or sets the default idle for this charactor
        /// </summary>
        public IState Idle;

        /// <summary>
        /// Stores those commands that are queued
        /// </summary>
        public Queue<IState> QueuedStates = new Queue<IState>();

        /// <summary>
        /// Animation names
        /// </summary>
        public virtual string IdleAnimation
        {
            get { return "Idle"; }
        }

        public virtual string RunAnimation
        {
            get { return "Run"; }
        }

        public virtual string AttackAnimation
        {
            get { return "Attack"; }
        }
        #endregion

        #region Method
        public Charactor(GameWorld world) : base(world)
        {
            VisibleInFogOfWar = false;
        }

        public Charactor(GameWorld world, string classID) : base(world, classID)
        {
            VisibleInFogOfWar = false;
        }

        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);

            string value;

            if ((value = xml.GetAttribute("Speed")) != "")
                speed = float.Parse(value);

            if ((value = xml.GetAttribute("ObstructorRadius")) != "")
                pathObstructorRadius = float.Parse(value);

            if ((value = xml.GetAttribute("IsHero")) != "")
                IsHero = bool.Parse(value);

            int.TryParse(xml.GetAttribute("Food"), out Food);
        }

        /// <summary>
        /// Stop moving
        /// </summary>
        public void Stop()
        {
            if (moving)
            {
                elapsedAnimationTime = 0;
                moving = false;
            }  
            
            Model.Play(IdleAnimation, true, 0.2f);
            positionLastFrame = Position;
        }

        public override void PerformAction(Vector3 position, bool queueAction)
        {
            MoveTo(position, queueAction);
        }

        public override void PerformAction(Entity entity, bool queueAction)
        {
            if (entity is GameObject && IsOpponent(entity as GameObject))
                AttackTo(entity as GameObject, queueAction);
            else if (entity != this)
                MoveTo(entity, queueAction);
        }

        public void MoveTo(Vector3 position, bool queueAction)
        {
            if (IsAlive)
            {
                if (Sound != null && Owner is LocalPlayer)
                    Audios.Play(Sound, Audios.Channel.Unit, this);

                IState state = new StateMoveToPosition(new Vector2(position.X, position.Y), this, Priority, World.PathManager);

                if (queueAction)
                    QueuedStates.Enqueue(state);
                else
                    State = state;
            }
        }

        public void MoveTo(IWorldObject target, bool queueAction)
        {
            if (IsAlive)
            {
                if (Sound != null && Owner is LocalPlayer)
                    Audios.Play(Sound, Audios.Channel.Unit, this);

                IState state = new StateMoveToTarget(this, target, Priority, World.PathManager);

                if (queueAction)
                    QueuedStates.Enqueue(state);
                else
                    State = state;
            }
        }

        public void AttackTo(Vector3 position, bool queueAction)
        {
            if (IsAlive)
            {
                if (Combat == null)
                {
                    MoveTo(position, queueAction);
                    return;
                }

                if (Sound != null && Owner is LocalPlayer)
                    Audios.Play(Sound, Audios.Channel.Unit, this);

                IState state = new StateAttack(World, this, position, Combat);

                if (queueAction)
                    QueuedStates.Enqueue(state);
                else
                    State = state;
            }
        }

        public void AttackTo(GameObject target, bool queueAction)
        {
            if (IsAlive)
            {
                if (Combat == null)
                {
                    MoveTo(target, queueAction);
                    return;
                }

                if (Sound != null && Owner is LocalPlayer)
                    Audios.Play(Sound, Audios.Channel.Unit, this);

                IState state = new StateAttack(World, this, target, Combat);

                if (queueAction)
                    QueuedStates.Enqueue(state);
                else
                    State = state;
            }
        }

        protected override void UpdateOutline(Outline outline)
        {
            Vector2 position;
            position.X = Position.X;
            position.Y = Position.Y;
            outline.SetCircle(position, PathObstructorRadius * 2);
        }

        /// <summary>
        /// Gets whether the target point is reached
        /// </summary>
        public virtual bool TargetPointReached(Vector3 point)
        {
            return Outline.Overlaps(new Vector2(point.X, point.Y));
        }

        /// <summary>
        /// Gets whether target entity is reached
        /// </summary>
        public virtual bool TargetReached(Entity entity)
        {
            return entity == null ? false :
                   entity.Outline.DistanceTo(new Vector2(Position.X, Position.Y)) < Outline.Radius;
        }
        
        /// <summary>
        /// Make the charactor visible and active in the world. Should be called after Unspawn
        /// </summary>
        public void Spawn(Vector3 position)
        {
            Vector2 spawnPosition;
            spawnPosition.X = position.X;
            spawnPosition.Y = position.Y;
            spawnPosition = World.PathManager.FindValidPosition(spawnPosition, Brush);
            Position = new Vector3(spawnPosition, 0);
            Fall();
            Visible = true;
            World.PathManager.AddMovable(this);
        }

        /// <summary>
        /// Make the charactor invisible and inactive in the world.
        /// </summary>
        public void Unspawn()
        {
            Visible = false;
            World.PathManager.RemoveMovable(this);
        }

        public override void OnCreate()
        {
            // Setup owner
            if (Owner != null)
            {
                Owner.Food += Food;
                Owner.Add(this);
                Owner.MarkFutureObject(ClassID);
            }

            Combat = new SpellCombat(World, this);

            // Create a path brush
            brush = World.PathManager.CreateBrush(pathObstructorRadius);

            // Find a valid position
            Position = new Vector3(World.PathManager.FindValidPosition(
                       new Vector2(Position.X, Position.Y), brush), 0);

            // Fall on the ground
            Fall();

            // Add this to the path manager
            World.PathManager.AddMovable(this);

            if (Model != null)
                Model.Play(IdleAnimation);

            positionLastFrame = Position;

            SelectionAreaRadius = PathObstructorRadius;

            // Initialize idle state
            if (Idle == null)
                Idle = new StateCharactorIdle(this);

            State = Idle;

            base.OnCreate();
        }

        protected override void OnDie()
        {
            if (Owner != null)
            {
                Owner.Food -= Food;
                Owner.Remove(this);
                Owner.UnmarkFutureObject(ClassID);
            }

            // Play the horrible death sfx
            if (Helper.Random.Next(4) == 0 && Owner is LocalPlayer)
                Audios.Play("Death", Audios.Channel.Interface, null);

            World.PathManager.RemoveMovable(this);

            State = new StateCharactorDie(this);
        }

        protected override bool OnStateChanged(IState newState, ref IState resultState) 
        {
            if (!IsAlive && !(newState is StateCharactorDie))
                return false;

            // Handle queued states
            if (newState == null)
            {
                resultState = QueuedStates.Count > 0 ? QueuedStates.Dequeue() : Idle;
                return true;
            }

            // Remove all queued states
            QueuedStates.Clear();
            resultState = newState;
            return true;
        }

        protected override void ShowSpells(GameUI ui)
        {
            if (Sound != null && Owner is LocalPlayer)
                Audios.Play(Sound, Audios.Channel.Unit, this);

            base.ShowSpells(ui);
            
            if (Owner is LocalPlayer)
            {
                ui.SetUIElement(0, false, (Owner as LocalPlayer).Attack.Button);
                ui.SetUIElement(1, false, (Owner as LocalPlayer).Move.Button);
            }
        }

        // Variables to avoid animation jittering
        const float MinAnimationDuraction = 0.2f;
        float elapsedAnimationTime = 0;
        Vector3 positionLastFrame;
        bool moving = false;

        public override void Draw(GameTime gameTime)
        {
            if (ShowGlow && ShouldDrawModel)
            {
                if (Glow == null)
                    Glow = new EffectGlow(World, this);
                Glow.Update(gameTime);
                ShowGlow = false;
            }

            base.Draw(gameTime);
        }
        
        public override void Update(GameTime gameTime)
        {
            if (IsAlive)
            {
                // Adjust the facing of the charactor.
                // Smooth entity rotation exponentially
                const float PiPi = 2 * MathHelper.Pi;
                float rotationOffset = targetRotation - rotation;

                while (rotationOffset > MathHelper.Pi)
                    rotationOffset -= PiPi;
                while (rotationOffset < -MathHelper.Pi)
                    rotationOffset += PiPi;

                if (Math.Abs(rotationOffset) > float.Epsilon)
                {
                    float smoother = (float)gameTime.ElapsedGameTime.TotalSeconds * 5;
                    if (smoother > 1) smoother = 1;
                    rotation += rotationOffset * smoother;
                    Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation);
                }

                // Update moving
                elapsedAnimationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Adjust the animation 
                bool moved = !(Math2D.FloatEquals(Position.X, positionLastFrame.X) &&
                               Math2D.FloatEquals(Position.Y, positionLastFrame.Y));

                positionLastFrame = Position;

                if (moved && !moving)
                {
                    moving = true;
                    elapsedAnimationTime = 0;
                    Model.Play(RunAnimation, true, 0.2f);
                }
                else if (!moved && moving && elapsedAnimationTime > MinAnimationDuraction)
                {
                    elapsedAnimationTime = 0;
                    moving = false;
                    Model.Play(IdleAnimation, true, 0.2f);
                }

                if (Combat != null)
                    Combat.Update(gameTime);
            }

            base.Update(gameTime);
        }

        public GameObject AttackTarget;

        public override void TriggerAttack(Entity target)
        {
            if (IsAlive)
            {
                Stop();
                Facing = target.Position - Position;
                Model.Play(AttackAnimation, false, 0.0f, OnComplete, null);
                AttackTarget = target as GameObject;
            }
        }

        private void OnComplete(object sender, EventArgs e)
        {
            if (IsAlive && AttackTarget != null)
            {
                if (SoundCombat != null)
                    Audios.Play(SoundCombat, this);

                AttackTarget.Health -= ComputeHit(this, AttackTarget);
            }
        }
        #endregion
    }
    #endregion

    #region Worker
    public class Worker : Charactor
    {
        public int LumberCarried = 0;
        public int GoldCarried = 0;
        public int LumberCapacity = 10;
        public int GoldCapacity = 10;

        GameModel wood;
        GameModel gold;

        public Worker(GameWorld world, string classID) : base(world, classID) { }

        public override string RunAnimation
        {
            get { return LumberCarried > 0 ? "Carry" : "Run"; }
        }

        //public override string AttackAnimation
        //{
        //    get { return Helper.Random.Next(2) == 0 ? "Attack" : "Chop"; }
        //}
        
        public override float Speed
        {
            get
            {
                if (LumberCarried > 0 || GoldCarried > 0)
                    return base.Speed * 0.75f;

                return base.Speed;
            }

            set { base.Speed = value; }
        }

        public override void OnCreate()
        {
            base.OnCreate();

            wood = GetAttachment("Wood");
            gold = GetAttachment("Gold");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        protected override void OnSelect(GameUI ui)
        {
            if (ui != null)
                Tree.Pickable = true;
            base.OnSelect(ui);
        }

        protected override void OnDeselect(GameUI ui)
        {
            if (ui != null)
                Tree.Pickable = false;
            base.OnDeselect(ui);
        }

        protected override void DrawAttachments(GameTime gameTime)
        {
            foreach (KeyValuePair<GameModel, int> pair in Attachment)
            {
                if (pair.Key == wood)
                {
                    if (LumberCarried > 0)
                        pair.Key.Draw(gameTime);
                }
                else if (pair.Key == gold)
                {
                    if (GoldCarried > 0)
                        pair.Key.Draw(gameTime);
                }
                else pair.Key.Draw(gameTime);
            }
        }

        public override void PerformAction(Entity entity, bool queueAction)
        {
            if (IsAlive)
            {
                IState state = null;

                if (Sound != null && Owner is LocalPlayer)
                    Audios.Play(Sound, Audios.Channel.Unit, this); 

                // Harvest lumber
                if (entity is Tree && (entity as Tree).IsAlive && (entity as Tree).Lumber > 0)
                    state = new StateHarvestLumber(World, this, entity as Tree);
                // Harvest gold
                else if (entity is Goldmine && (entity as Goldmine).Gold > 0)
                    state = new StateHarvestGold(World, this, entity as Goldmine);
                // Action on buildings
                else if (entity is Building)
                {
                    Building building = entity as Building;

                    if (building != null && building.Owner == Owner)
                    {
                        // Help construct building
                        if (building.State == Building.BuildingState.Constructing)
                            state = new StateConstruct(World, this, building);

                        // Help repair building
                        else if (building.State == Building.BuildingState.Normal &&
                                 building.Health < building.MaximumHealth)
                            state = new StateRepair(World, this, building);

                        // Go on harvesting
                        else if (building.ClassID == Owner.TownhallName &&
                                 building.State == Building.BuildingState.Normal)
                        {
                            System.Diagnostics.Debug.Assert(LumberCarried * GoldCarried == 0);

                            if (LumberCarried != 0)
                                state = new StateHarvestLumber(World, this, building);
                            else if (GoldCarried != 0)
                                state = new StateHarvestGold(World, this, building);
                        }
                        else if (building.ClassID == Owner.LumbermillName &&
                                 building.State == Building.BuildingState.Normal)
                        {
                            System.Diagnostics.Debug.Assert(LumberCarried * GoldCarried == 0);
                            state = new StateHarvestLumber(World, this, building);
                        }
                    }
                }

                if (state != null)
                    if (queueAction)
                        QueuedStates.Enqueue(state);
                    else
                        State = state;
                else
                    base.PerformAction(entity, queueAction);
            }
        }
    }
    #endregion

    #region Hunter
    public class Hunter : Charactor
    {
        bool weaponVisible = true;
        GameModel weapon;
        KeyValuePair<TimeSpan, EventHandler>[] trigger;

        public Hunter(GameWorld world, string type)
            : base(world, type)
        {
        }

        public override string AttackAnimation
        {
            get { return Helper.Random.Next(2) == 0 ? "Attack" : "Attack_2"; }
        }

        public override void OnCreate()
        {
            base.OnCreate();
            
            // Get weapon from attachment
            weapon = GetAttachment("Weapon");

            if (weapon == null)
                throw new Exception("The input model do not have a weapon attached.");

            AnimationClip clip = Model.GetAnimationClip("Attack");
            if (clip == null)
                throw new InvalidOperationException();

            TimeSpan time = new TimeSpan((long)(clip.Duration.Ticks * 18.0f / 41));
            trigger = new KeyValuePair<TimeSpan, EventHandler>[]
            {
                new KeyValuePair<TimeSpan, EventHandler>(time, Launch),
            };
        }

        protected override void DrawAttachments(GameTime gameTime)
        {
            foreach (KeyValuePair<GameModel, int> pair in Attachment)
            {
                if (pair.Key == weapon && weaponVisible || pair.Key != weapon)
                    pair.Key.Draw(gameTime);
            }
        }

        protected override bool OnStateChanged(IState newState, ref IState resultState)
        {
            weaponVisible = true;
            return base.OnStateChanged(newState, ref resultState);
        }

        public override void TriggerAttack(Entity target)
        {
            if (IsAlive)
            {
                Stop();
                Facing = target.Position - Position;
                Model.Play(AttackAnimation, false, 0.0f, OnComplete, trigger);
                AttackTarget = target as GameObject;
            }
        }

        void OnComplete(object sender, EventArgs e)
        {
            weaponVisible = true;
        }

        void Launch(object sender, EventArgs e)
        {
            Missile missile = new Missile(World, weapon, AttackTarget);

            missile.Hit += new EventHandler(Hit);
            World.Add(missile);

            weaponVisible = false;

            if (ShouldDrawModel)
                Audios.Play("HunterLaunch", this);
        }

        void Hit(object sender, EventArgs e)
        {
            IProjectile projectile = sender as IProjectile;

            if (projectile != null && projectile.Target is GameObject)
            {
                GameObject target = projectile.Target as GameObject;

                if (target.ShouldDrawModel && target.IsAlive)
                    Audios.Play("HunterHit", target);

                target.Health -= ComputeHit(this, target);
            }
        }
    }
    #endregion

    #region FireSorceress
    public class FireSorceress : Charactor
    {
        int rightHand;
        KeyValuePair<TimeSpan, EventHandler>[] trigger;

        public FireSorceress(GameWorld world, string classID)
            : base(world, classID) { }

        public override string AttackAnimation
        {
            get { return Helper.Random.Next(2) == 0 ? "Attack" : "Attack_2"; }
        }

        public override void OnCreate()
        {
            base.OnCreate();

            rightHand = Model.GetBone("Bip01_R_Hand");

            if (Owner != null && Owner.IsAvailable("PunishOfNatureUpgrade"))
                AddSpell("PunishOfNature");
        }

        public override void TriggerAttack(Entity target)
        {
            if (IsAlive)
            {
                if (Owner is ComputerPlayer && Helper.Random.Next(10) == 0)
                {
                    foreach (Spell spell in Spells)
                    {
                        if (spell is SpellPunishOfNature)
                        {
                            spell.Cast();
                            break;
                        }
                    }
                }
                else if (Owner is ComputerPlayer && !Owner.IsAvailable("Hellfire"))
                {
                    if (Owner is ComputerPlayer)
                    {
                        foreach (Spell spell in Spells)
                        {
                            if (spell is SpellSummon)
                            {
                                spell.Cast();
                                break;
                            }
                        }
                    }
                }
                else
                {

                    AnimationClip clip = Model.GetAnimationClip(AttackAnimation);
                    if (clip == null)
                        throw new InvalidOperationException();

                    TimeSpan time = new TimeSpan((long)(clip.Duration.Ticks * 0.5f));
                    trigger = new KeyValuePair<TimeSpan, EventHandler>[]
                    {
                        new KeyValuePair<TimeSpan, EventHandler>(time, Launch),
                    };

                    Stop();
                    Facing = target.Position - Position;
                    Model.Play("Attack", false, 0.0f, null, trigger);
                    AttackTarget = target as GameObject;
                }
            }
        }

        void Launch(object sender, EventArgs e)
        {
            Vector3 spawn = Vector3.Zero;

            if (rightHand >= 0)
                spawn = Model.GetBoneTransform(rightHand).Translation;
            else
                spawn = TopCenter - Vector3.UnitZ * 5;

            EffectFireball fireball = new EffectFireball(
                World, spawn, Vector3.UnitZ * 50, AttackTarget);
            fireball.Projectile.Hit += new EventHandler(Hit);
            World.Add(fireball);

            if (ShouldDrawModel)
                Audios.Play("FireballCast", this);
        }

        void Hit(object sender, EventArgs e)
        {
            IProjectile projectile = sender as IProjectile;

            if (projectile != null && projectile.Target is GameObject)
            {
                GameObject target = projectile.Target as GameObject;

                if (target.ShouldDrawModel)
                    Audios.Play("FireballHit", target);

                target.Health -= ComputeHit(this, target);
            }
        }
    }
    #endregion

    #region Hellfire
    public class Hellfire : Charactor
    {
        public Hellfire(GameWorld world, string classID)
            : base(world, classID) { }

        public override string AttackAnimation
        {
            get { return Helper.Random.Next(2) == 0 ? "Attack" : "Attack_2"; }
        }
    }
    #endregion
}

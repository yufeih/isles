// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Isles;

public class Charactor : GameObject, IMovable
{
    public EffectGlow Glow;
    public bool ShowGlow;
    public bool IsHero;
    public int Food;

    /// <summary>
    /// Gets or sets the speed of this charactor.
    /// </summary>
    public virtual float Speed { get; set; }

    /// <summary>
    /// Used by the movement system.
    /// </summary>
    public List<Point> PathMarks { get; set; }

    /// <summary>
    /// Gets or sets the radius of the charactor.
    /// </summary>
    public float PathObstructorRadius { get; set; } = 5;

    /// <summary>
    /// Gets or sets the facing of the charactor.
    /// </summary>
    public Vector3 Facing
    {
        get => new((float)Math.Cos(rotation), (float)Math.Sin(rotation), 0);
        set => targetRotation = (float)Math.Atan2(value.Y, value.X);
    }

    private float rotation;
    private float targetRotation;

    /// <summary>
    /// Gets or sets the path brush of the charactor.
    /// </summary>
    public PathBrush Brush { get; set; }

    /// <summary>
    /// Whether dynamic obstacles (E.g. units) are ignored when moving.
    /// </summary>
    public bool IgnoreDynamicObstacles
    {
        get => ignoreDynamicObstacles;

        set
        {
            if (!value)
            {
                // Make sure we don't stuck with each other
                Vector2 spawnPoint;
                spawnPoint.X = Position.X;
                spawnPoint.Y = Position.Y;
                spawnPoint = World.PathManager.FindValidPosition(spawnPoint, Brush);
                Position = new Vector3(spawnPoint, 0);
                World.PathManager.UpdateMovable(this);
            }

            ignoreDynamicObstacles = value;
        }
    }

    private bool ignoreDynamicObstacles;

    /// <summary>
    /// Gets or sets the combat spell for this charactor.
    /// </summary>
    public SpellCombat Combat;

    /// <summary>
    /// Gets or sets the default idle for this charactor.
    /// </summary>
    public BaseState Idle;

    /// <summary>
    /// Stores those commands that are queued.
    /// </summary>
    public Queue<BaseState> QueuedStates = new();

    /// <summary>
    /// Animation names.
    /// </summary>
    public virtual string IdleAnimation => "Idle";

    public virtual string RunAnimation => "Run";

    public virtual string AttackAnimation => "Attack";

    public override bool Visible => State is not StateHarvestGold harvestGold || (harvestGold.state != StateHarvestGold.StateType.Wait && harvestGold.state != StateHarvestGold.StateType.Harvest);

    public Charactor(string classID) : base(classID)
    {
        VisibleInFogOfWar = false;
    }

    public override void Deserialize(XmlElement xml)
    {
        base.Deserialize(xml);

        string value;

        if ((value = xml.GetAttribute("Speed")) != "")
        {
            Speed = float.Parse(value);
        }

        if ((value = xml.GetAttribute("ObstructorRadius")) != "")
        {
            PathObstructorRadius = float.Parse(value);
        }

        if ((value = xml.GetAttribute("IsHero")) != "")
        {
            IsHero = bool.Parse(value);
        }

        int.TryParse(xml.GetAttribute("Food"), out Food);
    }

    /// <summary>
    /// Stop moving.
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
        {
            AttackTo(entity as GameObject, queueAction);
        }
        else if (entity != this)
        {
            MoveTo(entity, queueAction);
        }
    }

    public void MoveTo(Vector3 position, bool queueAction)
    {
        if (IsAlive)
        {
            if (Sound != null && Owner is LocalPlayer)
            {
                Audios.Play(Sound, Audios.Channel.Unit, this);
            }

            BaseState state = new StateMoveToPosition(new Vector2(position.X, position.Y), this, Priority, World.PathManager);

            if (queueAction)
            {
                QueuedStates.Enqueue(state);
            }
            else
            {
                State = state;
            }
        }
    }

    public void MoveTo(BaseEntity target, bool queueAction)
    {
        if (IsAlive)
        {
            if (Sound != null && Owner is LocalPlayer)
            {
                Audios.Play(Sound, Audios.Channel.Unit, this);
            }

            BaseState state = new StateMoveToTarget(this, target, Priority, World.PathManager);

            if (queueAction)
            {
                QueuedStates.Enqueue(state);
            }
            else
            {
                State = state;
            }
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
            {
                Audios.Play(Sound, Audios.Channel.Unit, this);
            }

            BaseState state = new StateAttack(World, this, position, Combat);

            if (queueAction)
            {
                QueuedStates.Enqueue(state);
            }
            else
            {
                State = state;
            }
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
            {
                Audios.Play(Sound, Audios.Channel.Unit, this);
            }

            BaseState state = new StateAttack(World, this, target, Combat);

            if (queueAction)
            {
                QueuedStates.Enqueue(state);
            }
            else
            {
                State = state;
            }
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
    /// Gets whether the target point is reached.
    /// </summary>
    public virtual bool TargetPointReached(Vector3 point)
    {
        return Outline.Overlaps(new Vector2(point.X, point.Y));
    }

    /// <summary>
    /// Gets whether target entity is reached.
    /// </summary>
    public virtual bool TargetReached(Entity entity)
    {
        return entity != null && entity.Outline.DistanceTo(new Vector2(Position.X, Position.Y)) < Outline.Radius;
    }

    /// <summary>
    /// Make the charactor visible and active in the world. Should be called after Unspawn.
    /// </summary>
    public void Spawn(Vector3 position)
    {
        Vector2 spawnPosition;
        spawnPosition.X = position.X;
        spawnPosition.Y = position.Y;
        spawnPosition = World.PathManager.FindValidPosition(spawnPosition, Brush);
        Position = new Vector3(spawnPosition, 0);
        World.PathManager.AddMovable(this);
    }

    /// <summary>
    /// Make the charactor invisible and inactive in the world.
    /// </summary>
    public void Unspawn()
    {
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

        Combat = new SpellCombat(this);

        // Create a path brush
        Brush = World.PathManager.CreateBrush(PathObstructorRadius);

        // Find a valid position
        Position = new Vector3(World.PathManager.FindValidPosition(
                   new Vector2(Position.X, Position.Y), Brush), 0);

        // Add this to the path manager
        World.PathManager.AddMovable(this);

        if (Model != null)
        {
            Model.Play(IdleAnimation);
        }

        positionLastFrame = Position;

        SelectionAreaRadius = PathObstructorRadius;

        // Initialize idle state
        if (Idle == null)
        {
            Idle = new StateCharactorIdle(this);
        }

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
        {
            Audios.Play("Death", Audios.Channel.Interface, null);
        }

        World.PathManager.RemoveMovable(this);

        State = new StateCharactorDie(this);
    }

    protected override bool OnStateChanged(BaseState newState, ref BaseState resultState)
    {
        if (!IsAlive && newState is not StateCharactorDie)
        {
            return false;
        }

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
        {
            Audios.Play(Sound, Audios.Channel.Unit, this);
        }

        base.ShowSpells(ui);

        if (Owner is LocalPlayer)
        {
            ui.SetUIElement(0, false, (Owner as LocalPlayer).Attack.Button);
            ui.SetUIElement(1, false, (Owner as LocalPlayer).Move.Button);
        }
    }

    // Variables to avoid animation jittering
    private const float MinAnimationDuraction = 0.2f;
    private float elapsedAnimationTime;
    private Vector3 positionLastFrame;
    private bool moving;

    public override void Update(GameTime gameTime)
    {
        if (IsAlive)
        {
            // Adjust the facing of the charactor.
            // Smooth entity rotation exponentially
            const float PiPi = 2 * MathHelper.Pi;
            var rotationOffset = targetRotation - rotation;

            while (rotationOffset > MathHelper.Pi)
            {
                rotationOffset -= PiPi;
            }

            while (rotationOffset < -MathHelper.Pi)
            {
                rotationOffset += PiPi;
            }

            if (Math.Abs(rotationOffset) > float.Epsilon)
            {
                var smoother = (float)gameTime.ElapsedGameTime.TotalSeconds * 5;
                if (smoother > 1)
                {
                    smoother = 1;
                }

                rotation += rotationOffset * smoother;
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation);
            }

            // Update moving
            elapsedAnimationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Adjust the animation
            var moved = !(Math2D.FloatEquals(Position.X, positionLastFrame.X) &&
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
            {
                Combat.Update(gameTime);
            }
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
            {
                Audios.Play(SoundCombat, this);
            }

            AttackTarget.Health -= ComputeHit(this, AttackTarget);
        }
    }
}

public class Worker : Charactor
{
    public int LumberCarried;
    public int GoldCarried;
    public int LumberCapacity = 10;
    public int GoldCapacity = 10;
    public GameModel wood;
    public GameModel gold;

    public Worker(string classID) : base(classID) { }

    public override string RunAnimation => LumberCarried > 0 ? "Carry" : "Run";

    public override float Speed
    {
        get => LumberCarried > 0 || GoldCarried > 0 ? base.Speed * 0.75f : base.Speed;

        set => base.Speed = value;
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

    protected override void OnSelect(GameUI ui)
    {
        if (ui != null)
        {
            Tree.Pickable = true;
        }

        base.OnSelect(ui);
    }

    protected override void OnDeselect(GameUI ui)
    {
        if (ui != null)
        {
            Tree.Pickable = false;
        }

        base.OnDeselect(ui);
    }

    public override void PerformAction(Entity entity, bool queueAction)
    {
        if (IsAlive)
        {
            BaseState state = null;

            if (Sound != null && Owner is LocalPlayer)
            {
                Audios.Play(Sound, Audios.Channel.Unit, this);
            }

            // Harvest lumber
            if (entity is Tree && (entity as Tree).IsAlive && (entity as Tree).Lumber > 0)
            {
                state = new StateHarvestLumber(World, this, entity as Tree);
            }

            // Harvest gold
            else if (entity is Goldmine && (entity as Goldmine).Gold > 0)
            {
                state = new StateHarvestGold(World, this, entity as Goldmine);
            }

            // Action on buildings
            else if (entity is Building)
            {
                if (entity is Building building && building.Owner == Owner)
                {
                    // Help construct building
                    if (building.State == Building.BuildingState.Constructing)
                    {
                        state = new StateConstruct(World, this, building);
                    }

                    // Help repair building
                    else if (building.State == Building.BuildingState.Normal &&
                             building.Health < building.MaximumHealth)
                    {
                        state = new StateRepair(World, this, building);
                    }

                    // Go on harvesting
                    else if (building.ClassID == Owner.TownhallName &&
                             building.State == Building.BuildingState.Normal)
                    {
                        System.Diagnostics.Debug.Assert(LumberCarried * GoldCarried == 0);

                        if (LumberCarried != 0)
                        {
                            state = new StateHarvestLumber(World, this, building);
                        }
                        else if (GoldCarried != 0)
                        {
                            state = new StateHarvestGold(World, this, building);
                        }
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
            {
                if (queueAction)
                {
                    QueuedStates.Enqueue(state);
                }
                else
                {
                    State = state;
                }
            }
            else
            {
                base.PerformAction(entity, queueAction);
            }
        }
    }
}

public class Hunter : Charactor
{
    public bool weaponVisible = true;
    public GameModel weapon;

    public Hunter(string type)
        : base(type)
    {
    }

    public override string AttackAnimation => Helper.Random.Next(2) == 0 ? "Attack" : "Attack_2";

    public override void OnCreate()
    {
        base.OnCreate();

        // Get weapon from attachment
        weapon = GetAttachment("Weapon");

        if (weapon == null)
        {
            throw new Exception("The input model do not have a weapon attached.");
        }
    }

    protected override bool OnStateChanged(BaseState newState, ref BaseState resultState)
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
            Model.Play(AttackAnimation, false, 0.0f, OnComplete, (18.0f / 41, Launch));
            AttackTarget = target as GameObject;
        }
    }

    private void OnComplete(object sender, EventArgs e)
    {
        weaponVisible = true;
    }

    private void Launch()
    {
        var missile = new Missile(weapon, AttackTarget);

        missile.Hit += Hit;
        World.Add(missile);

        weaponVisible = false;

        if (ShouldDrawModel)
        {
            Audios.Play("HunterLaunch", this);
        }
    }

    private void Hit(object sender, EventArgs e)
    {
        if (sender is IProjectile projectile && projectile.Target is GameObject)
        {
            var target = projectile.Target as GameObject;

            if (target.ShouldDrawModel && target.IsAlive)
            {
                Audios.Play("HunterHit", target);
            }

            target.Health -= ComputeHit(this, target);
        }
    }
}

public class FireSorceress : Charactor
{
    private int rightHand;

    public FireSorceress(string classID)
        : base(classID) { }

    public override string AttackAnimation => Helper.Random.Next(2) == 0 ? "Attack" : "Attack_2";

    public override void OnCreate()
    {
        base.OnCreate();

        rightHand = Model.GetBone("Bip01_R_Hand");

        if (Owner != null && Owner.IsAvailable("PunishOfNatureUpgrade"))
        {
            AddSpell("PunishOfNature");
        }
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
                Stop();
                Facing = target.Position - Position;
                Model.Play("Attack", false, 0.0f, null, (0.5f, Launch));
                AttackTarget = target as GameObject;
            }
        }
    }

    private void Launch()
    {
        Vector3 spawn = Vector3.Zero;

        spawn = rightHand >= 0 ? Model.GetBoneTransform(rightHand).Translation : TopCenter - Vector3.UnitZ * 5;

        var fireball = new EffectFireball(spawn, Vector3.UnitZ * 50, AttackTarget);
        fireball.Projectile.Hit += Hit;
        World.Add(fireball);

        if (ShouldDrawModel)
        {
            Audios.Play("FireballCast", this);
        }
    }

    private void Hit(object sender, EventArgs e)
    {
        if (sender is IProjectile projectile && projectile.Target is GameObject)
        {
            var target = projectile.Target as GameObject;

            if (target.ShouldDrawModel)
            {
                Audios.Play("FireballHit", target);
            }

            target.Health -= ComputeHit(this, target);
        }
    }
}

public class Hellfire : Charactor
{
    public Hellfire(string classID)
        : base(classID) { }

    public override string AttackAnimation => Helper.Random.Next(2) == 0 ? "Attack" : "Attack_2";
}

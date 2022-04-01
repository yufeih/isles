// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;

namespace Isles;

/// <summary>
/// Respresents a building in the game.
/// </summary>
public class Building : GameObject, IPlaceable
{
    /// <summary>
    /// State of the building.
    /// </summary>
    public enum BuildingState
    {
        Normal,
        PreConstruct,
        Wait,
        Constructing,
        Destroyed,
    }

    /// <summary>
    /// Gets the building state.
    /// </summary>
    public new BuildingState State => state;

    public BuildingState state = BuildingState.Normal;

    /// <summary>
    /// Gets or sets the time to construct this building.
    /// </summary>
    public float ConstructionTime;

    /// <summary>
    /// Gets or sets the time to construct this building.
    /// </summary>
    public float ConstructionTimeElapsed;

    /// <summary>
    /// Gets or sets how much wood needed for the building.
    /// </summary>
    public int Lumber { get; set; }

    /// <summary>
    /// Gets or sets how much gold needed for the building.
    /// </summary>
    public int Gold { get; set; }

    /// <summary>
    /// Gets or sets how much food this building can provide.
    /// </summary>
    public int Food { get; set; }

    /// <summary>
    /// Gets or sets the number of builders.
    /// </summary>
    public int BuilderCount { get; set; }

    private const float CorporationTradeoff = 0.5f;

    /// <summary>
    /// Gets or sets the rotation on the xy plane.
    /// </summary>
    public float RotationZ { get; set; }

    /// <summary>
    /// Path brush.
    /// </summary>
    public Vector2 ObstructorSize { get; set; }
    private readonly List<Point> pathGrids = new();

    /// <summary>
    /// Gets the units that can be trained from this building.
    /// </summary>
    public List<string> Units { get; private set; } = new();

    /// <summary>
    /// Gets the pending requests that are going to be handled.
    /// </summary>
    public Queue<Spell> QueuedSpells { get; } = new();

    /// <summary>
    /// Gets or sets the spawn point of this building.
    /// </summary>
    public Vector3 SpawnPoint;
    public Worker Builder;

    private EffectConstruct construct;
    private List<EffectFire> fire;
    private List<Vector3> fireSpawnPoints;
    private List<Vector3> fireSpawnPointsLeft;

    /// <summary>
    /// Halo effect.
    /// </summary>
    private EffectHalo halo;
    
    
    public string Halo { get; set; }

    public Building(string classID)
        : base(classID)
    {
        // Reset sound die to explosion
        SoundDie = "Explosion";
    }

    public override void Deserialize(XmlElement xml)
    {
        Lumber = int.Parse(xml.GetAttribute("Lumber"));
        Gold = int.Parse(xml.GetAttribute("Gold"));
        Food = int.Parse(xml.GetAttribute("Food"));
        ConstructionTime = float.Parse(xml.GetAttribute("ConstructionTime"));

        string value;

        if ((value = xml.GetAttribute("ObstructorSize")) != "")
        {
            ObstructorSize = Helper.StringToVector2(value);
        }

        if ((value = xml.GetAttribute("SpawnPoint")) != "")
        {
            SpawnPoint = Helper.StringToVector3(value);
        }

        if ((value = xml.GetAttribute("Units")) != "")
        {
            Units = new List<string>(
                value.Split(new char[] { ',', ' ', '\n', '\r' }));
            Units.RemoveAll(delegate (string v) { return v.Length <= 0; });
        }

        if ((value = xml.GetAttribute("Halo")) != "")
        {
            Halo = value;
        }

        // Deserialize models after default attributes are assigned
        base.Deserialize(xml);
    }

    protected override void UpdateOutline(Outline outline)
    {
        Vector2 position;
        position.X = Position.X;
        position.Y = Position.Y;

        outline.SetRectangle(-ObstructorSize / 2, ObstructorSize / 2, position, RotationZ);
    }

    public override void OnCreate()
    {
        if (state == BuildingState.Normal)
        {
            OnComplete();
            MarkGrids();
        }

        if (Owner != null)
        {
            Owner.MarkFutureObject(ClassID);
        }

        base.OnCreate();
    }

    protected override void OnCreateSpell(Spell spell)
    {
        if (spell is SpellTraining training)
        {
            training.Enable = false;
            Units.Add(training.Type);
        }
    }

    protected override void ShowSpells(GameUI ui)
    {
        if (Sound != null)
        {
            Audios.Play(Sound, Audios.Channel.Building, this);
        }

        base.ShowSpells(ui);
    }

    protected virtual void OnComplete()
    {
        if (Owner != null)
        {
            Owner.Add(this);
            Owner.FoodCapacity += Food;
        }

        foreach (Spell spell in Spells)
        {
            spell.Enable = true;
        }

        if (Owner is LocalPlayer local &&
            local.CurrentGroup != null && local.CurrentGroup.Contains(this))
        {
            Focused = true;
        }
    }

    private void MarkGrids()
    {
        pathGrids.AddRange(World.PathManager.EnumerateGridsInOutline(Outline));
        World.PathManager.Mark(pathGrids);
    }

    protected override void OnSelect(GameUI ui)
    {
        Tree.Pickable = true;
        base.OnSelect(ui);
    }

    protected override void OnDeselect(GameUI ui)
    {
        Tree.Pickable = false;
        base.OnDeselect(ui);
    }

    public static Building LastDestroyedBuilding;

    public override void OnDestroy()
    {
        LastDestroyedBuilding = this;
        base.OnDestroy();
    }

    protected override void OnDie()
    {
        if (pathGrids != null)
        {
            World.PathManager.Unmark(pathGrids);
        }

        if (Owner != null && state == BuildingState.Normal)
        {
            Owner.Remove(this);
            Owner.FoodCapacity -= Food;
            Owner.UnmarkFutureObject(ClassID);
        }

        // Create explosion
        if (ShouldDrawModel)
        {
            new EffectExplosion((TopCenter + Position) / 2);
        }

        state = BuildingState.Destroyed;

        World.Remove(this);
    }

    /// <summary>
    /// Gets whether a given type of unit can be trained from this building.
    /// </summary>
    public bool CanTrain(string type)
    {
        if (state == BuildingState.Normal && Units != null && Units.Contains(type))
        {
            return !Owner.IsUnique(type) || !Owner.IsFutureAvailable(type);
        }

        return false;
    }

    /// <summary>
    /// Trains a given type of unit.
    /// </summary>
    public bool TrainUnit(string type)
    {
        if (CanTrain(type))
        {
            float gold, lumber, food;

            if ((gold = GameDefault.Singleton.GetGold(type)) > Owner.Gold)
            {
                if (Owner is LocalPlayer)
                {
                    Audios.Play("NotEnoughGold", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Insufficient Gold!", MessageType.Unavailable, Color.White);
                }

                return false;
            }
            else if ((lumber = GameDefault.Singleton.GetLumber(type)) > Owner.Lumber)
            {
                if (Owner is LocalPlayer)
                {
                    Audios.Play("NotEnoughLumber", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Insufficient Lumber!", MessageType.Unavailable, Color.White);
                }

                return false;
            }
            else if ((food = GameDefault.Singleton.GetFood(type)) > Owner.FoodCapacity - Owner.Food)
            {
                if (Owner is LocalPlayer)
                {
                    Audios.Play("MoreFarms", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Build More Farms!", MessageType.Unavailable, Color.White);
                }

                return false;
            }

            Owner.Gold -= gold;
            Owner.Lumber -= lumber;
            Owner.Food += food;
            Owner.MarkFutureObject(type);

            foreach (Spell s in Spells)
            {
                if (s is SpellTraining train && train.Type == type)
                {
                    train.Count++;
                    QueuedSpells.Enqueue(train);
                    return true;
                }
            }
        }

        return false;
    }

    public void CancelTraining()
    {
        // Removes the last spell
        if (QueuedSpells.Count > 0)
        {
            Spell[] spells = QueuedSpells.ToArray();
            QueuedSpells.Clear();
            for (var i = 0; i < spells.Length - 1; i++)
            {
                QueuedSpells.Enqueue(spells[i]);
            }

            Spell removed = spells[^1];
            if (removed is SpellTraining)
            {
                var spell = removed as SpellTraining;
                var type = spell.Type;
                Owner.Gold += GameDefault.Singleton.GetGold(type);
                Owner.Lumber += GameDefault.Singleton.GetLumber(type);
                Owner.Food -= GameDefault.Singleton.GetFood(type);

                if (Owner is ComputerPlayer)
                {
                    (Owner as ComputerPlayer).UnmarkFutureObject(type);
                }

                if (--spell.Count <= 0)
                {
                    spell.CoolDownElapsed = spell.CoolDown;
                }
            }
        }
    }

    public void CancelTraining(Spell target)
    {
        // Removes the last specified spell
        if (QueuedSpells.Count > 0)
        {
            Spell[] spells = QueuedSpells.ToArray();
            QueuedSpells.Clear();

            var removedIndex = -1;
            for (var i = spells.Length - 1; i >= 0; i--)
            {
                if (spells[i] == target)
                {
                    removedIndex = i;
                    break;
                }
            }

            if (removedIndex < 0)
            {
                throw new ArgumentException();
            }

            for (var i = 0; i < spells.Length; i++)
            {
                if (i != removedIndex)
                {
                    QueuedSpells.Enqueue(spells[i]);
                }
            }

            Spell removed = spells[removedIndex];
            if (removed is SpellTraining)
            {
                var spell = removed as SpellTraining;
                var type = spell.Type;
                Owner.Gold += GameDefault.Singleton.GetGold(type);
                Owner.Lumber += GameDefault.Singleton.GetLumber(type);
                Owner.Food -= GameDefault.Singleton.GetFood(type);

                Owner.UnmarkFutureObject(type);

                if (--spell.Count <= 0)
                {
                    spell.CoolDownElapsed = spell.CoolDown;
                }
            }
        }
    }

    public List<object> RallyPoints = new();

    public override void PerformAction(Entity entity, bool queueAction)
    {
        if (!queueAction)
        {
            RallyPoints.Clear();
        }

        RallyPoints.Add(entity);

        if (Owner is LocalPlayer)
        {
            Audios.Play("Rally");
        }
    }

    public override void PerformAction(Vector3 position, bool queueAction)
    {
        if (!queueAction)
        {
            RallyPoints.Clear();
        }

        RallyPoints.Add(position);

        if (Owner is LocalPlayer)
        {
            Audios.Play("Rally");
        }
    }

    protected override void DrawFogOfWar()
    {
        if (state != BuildingState.PreConstruct &&
            state != BuildingState.Constructing)
        {
            base.DrawFogOfWar();
        }
    }

    public override void Update(GameTime gameTime)
    {
        // Pre construct
        if (state == BuildingState.PreConstruct)
        {
            GameModel.Tint = IsLocationPlacable() ? Vector3.One : Vector3.UnitX;
        }

        // Wait
        if (state == BuildingState.Wait)
        {
            if (CanPlace(false))
            {
                RestoreStates();
                MarkGrids();
                state = BuildingState.Constructing;
                BeginConstruct();
            }
            else if (waitTimer < 0)
            {
                RestoreStates();
                if (Owner is LocalPlayer)
                {
                    Audios.Play("CannotBuild", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Can't Build There...", MessageType.Unavailable, Color.White);
                }

                CancelPlace();
                OnDie();
            }

            waitTimer -= gameTime.ElapsedGameTime.TotalSeconds;
        }

        // Construct building
        if (state == BuildingState.Constructing && BuilderCount > 0)
        {
            if (construct == null)
            {
                construct = new EffectConstruct(Outline * 0.5f, Position.Z, Position.Z + 10);
            }

            if (ShouldDrawModel)
            {
                construct.Update(gameTime);
            }

            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            ConstructionTimeElapsed += elapsedSeconds * (1 + (BuilderCount - 1) * CorporationTradeoff);

            if (ConstructionTimeElapsed > ConstructionTime)
            {
                ConstructionTimeElapsed = ConstructionTime;

                // Build complete
                OnComplete();

                GameModel.Alpha = 1.0f;
                state = BuildingState.Normal;
            }
        }

        // Building onfire
        const float StartBurnPercentage = 0.7f;
        if (state == BuildingState.Normal && Health < MaximumHealth * StartBurnPercentage)
        {
            if (fire == null)
            {
                fire = new List<EffectFire>();

                fireSpawnPoints = new List<Vector3>();
                fireSpawnPointsLeft = new List<Vector3>();

                var index = 1;
                int bone;
                while ((bone = GameModel.GetBone("fire" + index)) >= 0)
                {
                    index++;
                    fireSpawnPoints.Add(GameModel.GetBoneTransform(bone).Translation);
                }

                fireSpawnPointsLeft.AddRange(fireSpawnPoints);
            }

            // How many fire we should set
            var count = 1 + (int)(fireSpawnPoints.Count * (1 - Health / (MaximumHealth * StartBurnPercentage)));
            if (count > fireSpawnPoints.Count)
            {
                count = fireSpawnPoints.Count;
            }

            if (count > fire.Count)
            {
                // Setup a new fires
                Vector3 position = fireSpawnPointsLeft[
                    Helper.Random.Next(fireSpawnPointsLeft.Count)];

                fireSpawnPointsLeft.Remove(position);

                fire.Add(new EffectFire(position));

                Audios.Play("Damage", this);
            }
            else if (count < fire.Count)
            {
                // Remove an existing fire
                var index = Helper.Random.Next(fire.Count);

                Vector3 position = fire[index].Position;

                fire.RemoveAt(index);

                fireSpawnPointsLeft.Add(position);
            }

            // Update fires
            if (ShouldDrawModel)
            {
                foreach (EffectFire effect in fire)
                {
                    effect.Update(gameTime);
                }
            }
        }

        // Repair building
        if (state == BuildingState.Normal && Health < MaximumHealth && BuilderCount > 0)
        {
            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Health += 0.2f * MaximumHealth / ConstructionTime * elapsedSeconds *
                            (1 + (BuilderCount - 1) * CorporationTradeoff);
        }

        // Produce units & upgrades
        if (state == BuildingState.Normal && QueuedSpells.Count > 0)
        {
            if (QueuedSpells.Peek().Ready)
            {
                QueuedSpells.Peek().Cast();
            }

            // Create halo effect
            if (ShouldDrawModel && Halo != null)
            {
                if (halo == null)
                {
                    halo = new EffectHalo(TopCenter, 30, Halo);
                }

                halo.Update(gameTime);
            }
        }

        base.Update(gameTime);
    }

    public override EventResult HandleEvent(EventType type, object sender, object tag)
    {
        if (type == EventType.KeyDown && tag is Keys? && (tag as Keys?).Value == Keys.Escape)
        {
            // Press Esc to cancel build
            if (state == BuildingState.Constructing)
            {
                CancelPlace();
                OnDie();
                return EventResult.Handled;
            }

            // Cancel last spell
            if (state == BuildingState.Normal)
            {
                CancelTraining();
            }
        }

        return EventResult.Unhandled;
    }

    private void RestoreStates()
    {
        // Restore states
        foreach (var pair in negotiables)
        {
            pair.Key.State = pair.Value;
        }
    }

    public bool BeginPlace()
    {
        if (Owner != null)
        {
            if (Owner is LocalPlayer)
            {
                if (Gold > Owner.Gold)
                {
                    Audios.Play("NotEnoughGold", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Insufficient Gold!", MessageType.Unavailable, Color.White);
                    return false;
                }

                if (Lumber > Owner.Lumber)
                {
                    Audios.Play("NotEnoughLumber", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Insufficient Lumber!", MessageType.Unavailable, Color.White);
                    return false;
                }
            }

            Owner.Gold -= Gold;
            Owner.Lumber -= Lumber;
            GameModel.Alpha = 0.3f;
            state = BuildingState.PreConstruct;
        }

        return true;
    }

    private List<Charactor> GetNegotiables()
    {
        var obstructors = new List<Charactor>();
        var nearbyObjects = World.GetNearbyObjects(Position, Math.Max(ObstructorSize.X / 2,
                                                                  ObstructorSize.Y / 2));
        foreach (var wo in nearbyObjects)
        {
            if (wo is Charactor c && c.IsAlive && c.Owner == Owner)
            {
                obstructors.Add(c);
            }
        }

        return obstructors;
    }

    public bool IsLocationPlacable()
    {
        if (Owner is LocalPlayer)
        {
            if (World.FogOfWar.Contains(Position.X, Position.Y))
            {
                return false;
            }

            if (GameUI.Singleton.Overlaps(World.Game.Input.MousePosition))
            {
                return false;
            }
        }

        // Exclude nearby units with the same owner
        List<Charactor> negotiables = GetNegotiables();

        foreach (Charactor c in negotiables)
        {
            World.PathManager.Unmark(c);
        }

        var canPlace = CanPlace(true);

        foreach (Charactor c in negotiables)
        {
            World.PathManager.Mark(c);
        }

        return canPlace;
    }

    private bool CanPlace(bool enlargeOutline)
    {
        const float RadiusSquared = 80 * 80;

        // Buildings can't be placed near the goldmine
        foreach (var o in World.GetNearbyObjects(Position, 50))
        {
            if (o is Goldmine g)
            {
                Vector2 v;
                v.X = g.Position.X - Position.X;
                v.Y = g.Position.Y - Position.Y;
                if (v.LengthSquared() < RadiusSquared)
                {
                    return false;
                }
            }
        }

        return World.PathManager.CanBePlacedAt(
               World.PathManager.EnumerateGridsInOutline(
               enlargeOutline ? Outline + 5 : Outline), true);
    }

    private double waitTimer;
    private readonly List<KeyValuePair<Charactor, BaseState>> negotiables = new();

    public void Place()
    {
        if (Owner is LocalPlayer)
        {
            Audios.Play("Place");
        }

        // Check if the building can be directly placed at the target
        if (!CanPlace(false))
        {
            // Tell the nearby charactors to avoid the building
            Vector2 position;
            position.X = Position.X;
            position.Y = Position.Y;
            var temp = new List<Point>();
            var grids = new List<Point>(World.PathManager.EnumerateGridsInOutline(Outline * 1.5f));
            World.PathManager.Mark(grids);

            negotiables.Clear();

            foreach (Charactor c in GetNegotiables())
            {
                Vector2 target = World.PathManager.FindValidPosition(position, c.Brush);
                negotiables.Add(new KeyValuePair<Charactor, BaseState>(c, c.State));
                c.MoveTo(new Vector3(target, 0), false);

                temp.Clear();
                temp.AddRange(World.PathManager.Graph.EnumerateGridsInBrush(target, c.Brush));
                grids.AddRange(temp);
                World.PathManager.Mark(temp);
            }

            World.PathManager.Unmark(grids);

            // Reset wait timer
            waitTimer = 4;

            state = BuildingState.Wait;
        }
        else
        {
            MarkGrids();
            state = BuildingState.Constructing;
            BeginConstruct();
        }
    }

    private void BeginConstruct()
    {
        if (Builder != null)
        {
            BaseState state = new StateConstruct(World, Builder, this);
            if (World.Game.Input.IsShiftPressed)
            {
                Builder.QueuedStates.Enqueue(state);
            }
            else
            {
                Builder.State = state;
            }
        }
    }

    public void CancelPlace()
    {
        Owner.Gold += Gold;
        Owner.Lumber += Lumber;
    }
}

public class Tower : Building
{
    private readonly SpellCombat combat;
    private GameObject currentTarget;

    public Tower(string classID)
        : base(classID)
    {
        // Add a combat spell
        combat = new SpellCombat(this);
    }

    public override void TriggerAttack(Entity target)
    {
        Vector3 velocity = target.Position - Position;
        velocity.Z = 0;
        velocity.Z = (float)(velocity.Length() * Math.Tan(MathHelper.ToRadians(30)));
        velocity.Normalize();
        velocity *= 75;

        var fireball = new EffectFireball(
            TopCenter - Vector3.UnitZ * 5, velocity, target,
            "Frost", "FrostExplosion");
        fireball.Projectile.Hit += Hit;
        World.Add(fireball);

        if (ShouldDrawModel)
        {
            Audios.Play("FireballCast", this);
        }
    }

    public override void PerformAction(Entity entity, bool queueAction)
    {
        currentTarget = entity as GameObject;
    }

    public override void Update(GameTime gameTime)
    {
        if (state == BuildingState.Normal && IsAlive)
        {
            // Trigger attack
            if (IsOpponent(currentTarget) &&
                combat.CanAttakTarget(currentTarget) &&
                combat.TargetWithinRange(currentTarget))
            {
                combat.Cast(currentTarget);
            }
            else
            {
                currentTarget = StateAttack.FindAnotherTarget(World, this, null, combat);
            }

            if (combat != null)
            {
                combat.Update(gameTime);
            }
        }

        base.Update(gameTime);
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

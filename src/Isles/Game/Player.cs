// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Isles
{
    public enum PlayerType
    {
        Dummy,
        Local,
        Computer,
        Remote,
    }

    public enum PlayerRelation
    {
        Opponent,
        Ally,
        Neutral,
    }

    public enum Race
    {
        Islander,
        Steamer,
    }

    public class PlayerInfo
    {
        public string Name;
        public int Team;
        public Color TeamColor;
        public Race Race;
        public Vector2 SpawnPoint;
        public PlayerType Type;
    }

    /// <summary>
    /// Represent either human player, computer oppnent.
    /// </summary>
    public abstract class Player : IEventListener
    {
        public static List<Player> AllPlayers = new();
        public static LocalPlayer LocalPlayer;

        /// <summary>
        /// Gets player from id.
        /// </summary>
        public static Player FromID(int id)
        {
            return AllPlayers[id];
        }

        public static void Reset()
        {
            AllPlayers.Clear();
            LocalPlayer = null;
        }

        private static readonly List<string> BuildingRegistry = new();
        private static readonly List<string> CharactorRegistry = new();

        public static void RegisterBuilding(string type)
        {
            if (!BuildingRegistry.Contains(type))
            {
                BuildingRegistry.Add(type);
            }

            System.Diagnostics.Debug.Assert(!CharactorRegistry.Contains(type));
        }

        public static void RegisterCharactor(string type)
        {
            if (!CharactorRegistry.Contains(type))
            {
                CharactorRegistry.Add(type);
            }

            System.Diagnostics.Debug.Assert(!BuildingRegistry.Contains(type));
        }

        public static bool IsBuilding(string type)
        {
            return BuildingRegistry.Contains(type);
        }

        public static bool IsCharactor(string type)
        {
            return CharactorRegistry.Contains(type);
        }

        /// <summary>
        /// Gets or sets the name of the player.
        /// </summary>
        public string Name;

        /// <summary>
        /// Gets or sets which team the player belongs.
        /// </summary>
        public int Team;

        /// <summary>
        /// Gets or sets the color of the team.
        /// </summary>
        public Color TeamColor;

        /// <summary>
        /// Gets or sets the race of the player.
        /// </summary>
        public Race Race;

        /// <summary>
        /// Gets or sets the spawnpoint of the player.
        /// </summary>
        public Vector2 SpawnPoint;

        /// <summary>
        /// Gets or sets how much gold the player have.
        /// </summary>
        public float Gold
        {
            get => gold;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                gold = value;
            }
        }

        private float gold;

        /// <summary>
        /// Gets or sets how much lumber the player have.
        /// </summary>
        public float Lumber
        {
            get => lumber;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                lumber = value;
            }
        }

        private float lumber;

        /// <summary>
        /// Gets or sets how much food the player have.
        /// </summary>
        public float Food
        {
            get => food;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                food = value;
            }
        }

        private float food;

        public const float MaxFoodCapacity = 100;

        /// <summary>
        /// Gets or sets current food capacity of this player.
        /// </summary>
        public float FoodCapacity
        {
            get => foodCapacity;
            set
            {
                if (value > MaxFoodCapacity)
                {
                    value = MaxFoodCapacity;
                }

                foodCapacity = value;
            }
        }

        private float foodCapacity;

        /// <summary>
        /// Gets or sets how many trees are cutted down by this player.
        /// </summary>
        public int TreesCuttedDown
        {
            get => treesCuttedDown;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                treesCuttedDown = value;
            }
        }

        private int treesCuttedDown;

        /// <summary>
        /// Gets or sets the attack point of this player.
        /// </summary>
        public float AttackPoint;

        /// <summary>
        /// Gets or sets the attack point of this player.
        /// </summary>
        public float DefensePoint;

        /// <summary>
        /// Gets or sets how much smoke is produced by this player.
        /// </summary>
        public float SmokeProduced
        {
            get => smokeProduced;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                smokeProduced = value;
            }
        }

        private float smokeProduced;

        /// <summary>
        /// Gets the environment condition of the player.
        /// </summary>
        public float EnvironmentLevel
        {
            get
            {
                var value = smokeProduced + treesCuttedDown * 1.0f;
                return MathHelper.Clamp(value * 0.1f, 0, 1);
            }
        }

        /// <summary>
        /// Stores whether a technique is available. Instead of storing a
        /// bool value, we stores a reference counter. E.g., if we have
        /// multiple townhalls, we don't have to worry about how many we have now,
        /// simply call Mark/Unmark when a town hall is built or destroyed.
        /// </summary>
        /// <remarks>
        /// Note that some dependencies are not enitities, e.g., techniques,
        /// so you cannot directly use the number of objects.
        /// </remarks>
        private readonly Dictionary<string, int> availability = new();

        /// <summary>
        /// Gets object dependency table.
        /// </summary>
        public List<KeyValuePair<string, string>> Dependencies { get; } = new();

        /// <summary>
        /// Gets objects owned by this player.
        /// </summary>
        public Dictionary<string, LinkedList<GameObject>> Objects { get; } = new();

        /// <summary>
        /// Gets a dictionary storing the number of objects that will be available in the near future.
        /// </summary>
        public Dictionary<string, int> FutureObjects { get; } = new();

        /// <summary>
        /// Gets all objects with the specified type.
        /// </summary>
        public LinkedList<GameObject> GetObjects(string type)
        {
            return Objects.TryGetValue(type, out LinkedList<GameObject> value) ? value : null;
        }

        /// <summary>
        /// Marks a technique as available.
        /// </summary>
        public void Add(GameObject entity)
        {
            if (!availability.ContainsKey(entity.ClassID))
            {
                availability.Add(entity.ClassID, 0);
            }

            if (!Objects.ContainsKey(entity.ClassID))
            {
                Objects.Add(entity.ClassID, new LinkedList<GameObject>());
            }

            availability[entity.ClassID]++;
            Objects[entity.ClassID].AddFirst(entity);
        }

        /// <summary>
        /// Marks a technique as unavailable.
        /// </summary>
        public void Remove(GameObject entity)
        {
            if (availability.ContainsKey(entity.ClassID))
            {
                availability[entity.ClassID]--;
                Objects[entity.ClassID].Remove(entity);

                System.Diagnostics.Debug.Assert(availability[entity.ClassID] >= 0);
            }
        }

        /// <summary>
        /// Marks a technique as available.
        /// </summary>
        public void MarkFutureObject(string type)
        {
            if (!FutureObjects.ContainsKey(type))
            {
                FutureObjects.Add(type, 0);
            }

            FutureObjects[type]++;
        }

        /// <summary>
        /// Marks a technique as unavailable.
        /// </summary>
        public void UnmarkFutureObject(string type)
        {
            if (FutureObjects.ContainsKey(type))
            {
                FutureObjects[type]--;

                System.Diagnostics.Debug.Assert(FutureObjects[type] >= 0);
            }
        }

        /// <summary>
        /// Gets whether the specified type is a hero.
        /// </summary>
        public bool IsUnique(string type)
        {
            return GameDefault.Singleton.IsUnique(type);
        }

        /// <summary>
        /// Gets whether a technique is currently available.
        /// </summary>
        public bool IsAvailable(string name)
        {
            availability.TryGetValue(name, out var value);
            return value > 0;
        }

        public bool IsFutureAvailable(string name)
        {
            FutureObjects.TryGetValue(name, out var value);
            return value > 0;
        }

        /// <summary>
        /// Marks a dependency to be available.
        /// </summary>
        public void MarkAvailable(string name)
        {
            if (!availability.ContainsKey(name))
            {
                availability.Add(name, 0);
            }

            availability[name]++;
        }

        /// <summary>
        /// Marks a dependency to be unavailable.
        /// </summary>
        public void MarkUnavailable(string name)
        {
            availability[name]--;

            System.Diagnostics.Debug.Assert(availability[name] >= 0);
        }

        /// <summary>
        /// Checks the dependency for given technique.
        /// </summary>
        public bool CheckDependency(string name)
        {
            var available = true;

            // FIXME: What if the name is not in the list
            for (var i = 0; i < Dependencies.Count; i++)
            {
                if (Dependencies[i].Key == name && !IsAvailable(Dependencies[i].Value))
                {
                    available = false;
                }
            }

            return available;
        }

        /// <summary>
        /// Adds a new dependency.
        /// </summary>
        public void AddDependency(string what, string dependsOnWhat)
        {
            Dependencies.Add(new KeyValuePair<string, string>(what, dependsOnWhat));
        }

        /// <summary>
        /// Removes a new dependency.
        /// </summary>
        public void RemoveDependency(string what, string dependsOnWhat)
        {
            for (var i = 0; i < Dependencies.Count; i++)
            {
                if (Dependencies[i].Key == what && Dependencies[i].Value == dependsOnWhat)
                {
                    Dependencies.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>
        /// Enumerate all game objects owned by this player.
        /// </summary>
        public IEnumerable<GameObject> EnumerateObjects()
        {
            foreach (KeyValuePair<string, LinkedList<GameObject>> list in Objects)
            {
                foreach (GameObject e in list.Value)
                {
                    yield return e;
                }
            }
        }

        /// <summary>
        /// Finds all the objects of a given type.
        /// </summary>
        public IEnumerable<GameObject> EnumerateObjects(string type)
        {
            if (Objects.TryGetValue(type, out LinkedList<GameObject> value))
            {
                foreach (GameObject o in value)
                {
                    yield return o;
                }
            }
        }

        /// <summary>
        /// Find the nearest object to the specified position of a given type.
        /// </summary>
        /// <param name="excluded">This object will be excluded.</param>
        public Entity FindNearestObject(Vector3 position, string type, Entity excluded)
        {
            if (type == null)
            {
                return null;
            }

            Entity nearest = null;
            var distanceSquared = float.MaxValue;

            foreach (Entity e in EnumerateObjects(type))
            {
                if (e == excluded)
                {
                    continue;
                }

                var dist = Vector3.Subtract(e.Position, position).LengthSquared();

                if (dist < distanceSquared)
                {
                    distanceSquared = dist;
                    nearest = e;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Names.
        /// </summary>
        public string TownhallName => Race == Race.Islander ? "Townhall" : "SteamFort";

        public string LumbermillName => Race == Race.Islander ? "Lumbermill" : null;

        public string HouseName => Race == Race.Islander ? "Farmhouse" : "Steamhouse";

        public string WorkerName => Race == Race.Islander ? "Follower" : "Miner";

        public string TowerName => Race == Race.Islander ? "Tower" : "SteamCannon";

        public string HeroName => Race == Race.Islander ? "FireSorceress" : "Steambot";

        /// <summary>
        /// Gets the relationship with the target player.
        /// </summary>
        public PlayerRelation GetRelation(Player player)
        {
            // Currently there are only allies and opponents
            return (player != null && player.Team == Team) ? PlayerRelation.Ally :
                                                             PlayerRelation.Opponent;
            // return PlayerRelation.Opponent;
        }

        public abstract void Update(GameTime gameTime);

        public virtual void Draw(GameTime gameTime) { }

        public virtual EventResult HandleEvent(EventType type, object sender, object tag)
        {
            return EventResult.Unhandled;
        }

        public static Vector2 GetCenter(IEnumerable<GameObject> objects)
        {
            var count = 0;
            Vector2 start = Vector2.Zero;

            // Compute the center and space of the selected charactors
            foreach (GameObject c in objects)
            {
                start.X += c.Position.X;
                start.Y += c.Position.Y;
                count++;
            }

            // Compute the average position
            start /= count;

            return start;
        }

        /// <summary>
        /// Creates a list of positions around the center.
        /// </summary>
        /// <returns>
        /// Target position for the corresponding charactor.
        /// </returns>
        public static IEnumerable<KeyValuePair<GameObject, Vector2>> CreateSquardPositions(
                List<GameObject> members, Vector3 center)
        {
            float space = 0;
            Vector2 start = GetCenter(members);

            // Compute the center and space of the selected charactors
            foreach (GameObject charactor in members)
            {
                // Find the max radius
                if (charactor.SelectionAreaRadius > space)
                {
                    space = charactor.SelectionAreaRadius * 1.5f;
                }
            }

            // Compute the space between charactors
            space *= 2;

            // Compute squard size
            var squardWidth = (int)Math.Sqrt(members.Count - 1) + 1;
            var squardHeight = (members.Count - 1) / squardWidth + 1;

            // Compute local to world transform
            Vector2 translation;
            translation.X = center.X;
            translation.Y = center.Y;

            var rotation = -MathHelper.PiOver2 + (float)Math.Atan2(start.Y - center.Y,
                                                                     start.X - center.X);

            // Transform charactor positions from world space to local space
            List<KeyValuePair<GameObject, Vector2>> startPositions;
            startPositions = new List<KeyValuePair<GameObject, Vector2>>(members.Count);

            for (var i = 0; i < members.Count; i++)
            {
                startPositions.Add(new KeyValuePair<GameObject, Vector2>(members[i],
                                         Math2D.WorldToLocal(new Vector2(members[i].Position.X,
                                                                         members[i].Position.Y),
                                                                         start, rotation)));
            }

            // Sort start position by Y value
            startPositions.Sort(delegate(KeyValuePair<GameObject, Vector2> a,
                                         KeyValuePair<GameObject, Vector2> b)
            {
                return a.Value.Y.CompareTo(b.Value.Y);
            });

            int index = 0, begin = 0;
            var compare = new Comparer();
            var positions = new Vector2[members.Count];
            var temp = new List<GameObject>(squardWidth);

            List<KeyValuePair<GameObject, Vector2>> orders;
            orders = new List<KeyValuePair<GameObject, Vector2>>();

            for (var y = 0; y < squardHeight; y++)
            {
                // Compute end positions in world space
                for (var x = 0; x < squardWidth; x++)
                {
                    if (index < positions.Length)
                    {
                        positions[index].X = x * space - space * (squardWidth - 1) / 2;
                        positions[index].Y = y * space - space * (squardHeight - 1) / 2;
                        positions[index] = Math2D.LocalToWorld(positions[index], translation,
                                                                                 rotation);

                        index++;
                    }
                }

                // Sort start position by X value
                startPositions.Sort(begin, index - begin, compare);

                // Associate positions with charactors
                for (var i = begin; i < index; i++)
                {
                    orders.Add(new KeyValuePair<GameObject, Vector2>(
                            startPositions[i].Key, positions[i]));
                }

                begin = index;
            }

            // Sort orders by object priority to make sound effect working correctly
            orders.Sort(delegate(KeyValuePair<GameObject, Vector2> pair1,
                                 KeyValuePair<GameObject, Vector2> pair2)
            {
                return (int)(pair1.Key.Priority - pair2.Key.Priority);
            });

            return orders;
        }

        private class Comparer : IComparer<KeyValuePair<GameObject, Vector2>>
        {
            /// <summary>
            /// For CreateSquardPositions.
            /// </summary>
            public int Compare(KeyValuePair<GameObject, Vector2> x, KeyValuePair<GameObject, Vector2> y)
            {
                return x.Value.X.CompareTo(y.Value.X);
            }
        }

        public abstract void Start(GameWorld world);
    }

    /// <summary>
    /// Dummy player will do nothing.
    /// </summary>
    public class DummyPlayer : Player
    {
        public override void Update(GameTime gameTime) { }

        public override void Start(GameWorld world) { }
    }

    /// <summary>
    /// The human player on the local machine.
    /// </summary>
    public class LocalPlayer : Player
    {
        /// <summary>
        /// Standard stuff.
        /// </summary>
        private readonly BaseGame game = BaseGame.Singleton;
        private GameCamera camera;
        private GameWorld world;

        /// <summary>
        /// Selected.
        /// </summary>
        public List<GameObject> Selected { get; } = new();

        public List<GameObject> CurrentGroup => Groups.Count <= 0 ? null : Groups[CurrentGroupIndex];

        public List<List<GameObject>> Groups { get; } = new();

        /// <summary>
        /// Gets or sets the index of the current group.
        /// </summary>
        public int CurrentGroupIndex { get; private set; }

        public bool SelectionDirty { get; set; } = true;

        private readonly List<GameObject> highlighted = new();
        private readonly List<GameObject>[] teams = new List<GameObject>[10];

        /// <summary>
        /// Multiselecting.
        /// </summary>
        private bool multiSelecting;
        private Rectangle multiSelectRectangle;
        private Point multiSelectStart;
        private bool keyDoublePressed;
        private double doubleClickTime;
        private bool traceCamera;

        /// <summary>
        /// Spells.
        /// </summary>
        public SpellAttack Attack;
        public SpellMove Move;

        /// <summary>
        /// List of attacker. We will draw a glow over them.
        /// </summary>
        private readonly List<GameObject> attackers = new();
        private readonly List<double> attackTimer = new();

        public override void Start(GameWorld world)
        {
            this.world = world ?? throw new ArgumentNullException();
            camera = game.Camera as GameCamera;
            Attack = new SpellAttack(world);
            Move = new SpellMove(world);

            for (var i = 0; i < teams.Length; i++)
            {
                teams[i] = new List<GameObject>();
            }
        }

        public void AddAttacker(GameObject attacker)
        {
            for (var i = 0; i < attackers.Count; i++)
            {
                if (attackers[i] == attacker)
                {
                    attackTimer[i] = 0;
                    return;
                }
            }

            attackers.Add(attacker);
            attackTimer.Add(0);
        }

        public void Highlight(GameObject entity)
        {
            foreach (GameObject o in highlighted)
            {
                if (o.Highlighted)
                {
                    o.Highlighted = false;
                }
            }

            highlighted.Clear();

            if (entity != null)
            {
                entity.Highlighted = true;
                highlighted.Add(entity);
            }
        }

        public void HighlightMultiple(IEnumerable<GameObject> entities)
        {
            if (entities == null)
            {
                return;
            }

            foreach (GameObject o in highlighted)
            {
                if (o.Highlighted)
                {
                    o.Highlighted = false;
                }
            }

            highlighted.Clear();

            foreach (GameObject o in entities)
            {
                if (o != null)
                {
                    o.Highlighted = true;
                    highlighted.Add(o);
                }
            }
        }

        public void Select(GameObject entity, bool shift)
        {
            if (shift)
            {
                if (entity != null)
                {
                    // Units and buildings can't be selected at the same time
                    if ((entity is Charactor && Selected.Count > 0 && Selected[0] is Building) ||
                        (entity is Building && Selected.Count > 0 && Selected[0] is Charactor))
                    {
                        return;
                    }

                    if (!entity.Selected)
                    {
                        Selected.Add(entity);
                    }

                    entity.Selected = !entity.Selected;
                }
            }
            else
            {
                foreach (GameObject o in Selected)
                {
                    if (o.Selected)
                    {
                        o.Selected = false;
                    }
                }

                Selected.Clear();

                if (entity != null)
                {
                    entity.Selected = true;
                    Selected.Add(entity);
                }
            }

            OnSelectedChanged();
        }

        public void SelectMultiple(IEnumerable<GameObject> entities, bool shift)
        {
            if (entities == null)
            {
                return;
            }

            if (shift)
            {
                foreach (GameObject entity in entities)
                {
                    // Units and buildings can't be selected at the same time
                    if ((entity is Charactor && Selected.Count > 0 && Selected[0] is Building) ||
                        (entity is Building && Selected.Count > 0 && Selected[0] is Charactor))
                    {
                        return;
                    }

                    if (!entity.Selected)
                    {
                        Selected.Add(entity);
                    }

                    entity.Selected = !entity.Selected;
                }
            }
            else
            {
                foreach (GameObject o in Selected)
                {
                    if (o.Selected)
                    {
                        o.Selected = false;
                    }
                }

                Selected.Clear();

                foreach (GameObject o in entities)
                {
                    if (o != null)
                    {
                        o.Selected = true;
                        Selected.Add(o);
                    }
                }
            }

            // Sorted selected by priority
            Selected.Sort((x, y) =>
            {
                var result = x.Priority.CompareTo(y.Priority);
                return result != 0 ? result : string.Compare(x.ClassID, y.ClassID);
            });

            OnSelectedChanged();
        }

        private void OnSelectedChanged()
        {
            SelectionDirty = true;

            Groups.Clear();

            if (Selected.Count != 0)
            {
                var currentClassID = Selected[0].ClassID;
                var group = new List<GameObject>();
                foreach (GameObject o in Selected)
                {
                    if (currentClassID == o.ClassID)
                    {
                        group.Add(o);
                    }
                    else
                    {
                        Groups.Add(group);
                        currentClassID = o.ClassID;
                        group = new List<GameObject>
                        {
                            o,
                        };
                    }
                }

                Groups.Add(group);
                Focus(0);
            }
            else
            {
                GameUI.Singleton.ClearUIElement();
            }

            CurrentGroupIndex = 0;
        }

        private IEnumerable<GameObject> ObjectsFromRectangle(Rectangle rectangle)
        {
            if (rectangle.Height == 0 || rectangle.Width == 0)
            {
                return null;
            }

            // Select multiple objects
            _ = Matrix.Identity;

            var left = (float)(2 * rectangle.Left - game.ScreenWidth) / game.ScreenWidth;
            var right = (float)(2 * rectangle.Right - game.ScreenWidth) / game.ScreenWidth;
            var bottom = (float)(game.ScreenHeight - 2 * rectangle.Top) / game.ScreenHeight;
            var top = (float)(game.ScreenHeight - 2 * rectangle.Bottom) / game.ScreenHeight;

            var size = Vector3.Transform(new Vector3(1, 1, 0), game.ProjectionInverse);

            Matrix.CreatePerspectiveOffCenter(
                left * size.X, right * size.X, bottom * size.Y, top * size.Y, 1.0f, 5000.0f, out Matrix rectProject);

            return SelectablesFromObjects(world.ObjectsFromRegion(
                                            new BoundingFrustum(game.View * rectProject)));
        }

        private IEnumerable<GameObject> SelectablesFromObjects(IEnumerable<IWorldObject> iEnumerable)
        {
            foreach (IWorldObject o in iEnumerable)
            {
                if (o is GameObject)
                {
                    yield return o as GameObject;
                }
            }
        }

        /// <summary>
        /// This method group the input selectable.
        /// </summary>
        private IEnumerable<GameObject> Filter(IEnumerable<GameObject> selectables)
        {
            if (selectables == null)
            {
                yield break;
            }

            var objects = new List<GameObject>(selectables);

            if (objects.Count <= 0)
            {
                yield break;
            }

            var type = -1;  // 0 for units, 1 for buildings, 2 for other player's object

            foreach (GameObject o in selectables)
            {
                if (o.Owner is LocalPlayer)
                {
                    System.Diagnostics.Debug.Assert(o is Charactor || o is Building);

                    if (o is Charactor)
                    {
                        type = 0;
                        break;
                    }

                    if (o is Building)
                    {
                        type = 1;
                    }
                }
                else if (type < 0)
                {
                    type = 2;
                }
            }

            // Units
            if (type == 0)
            {
                foreach (GameObject o in selectables)
                {
                    if (o is Charactor && o.Owner is LocalPlayer)
                    {
                        yield return o;
                    }
                }
            }
            else if (type == 1)
            {
                foreach (GameObject o in selectables)
                {
                    if (o is Building && o.Owner is LocalPlayer)
                    {
                        yield return o;
                    }
                }
            }
            else if (type == 2)
            {
                foreach (GameObject o in selectables)
                {
                    if (!(o.Owner is LocalPlayer))
                    {
                        yield return o;
                        break; // We can only select one other player's unit
                    }
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        private IEnumerable<GameObject> GetObjectsOfTheSameClass(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException();
            }

            foreach (Entity e in gameObject.Owner.EnumerateObjects(gameObject.ClassID))
            {
                if (e is GameObject o && o.Owner == gameObject.Owner &&
                    o.Visible && o.IsVisible(game.ViewProjection) &&
                    o.IsAlive && o.ClassID == gameObject.ClassID)
                {
                    yield return o;
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update spells
            if (Attack != null)
            {
                Attack.Update(gameTime);
            }

            if (Move != null)
            {
                Move.Update(gameTime);
            }

            // Update multi-selecting rectangle
            if (multiSelecting)
            {
                // Update selection rectangle
                multiSelectRectangle.X = Math.Min(
                    game.Input.MousePosition.X, multiSelectStart.X);
                multiSelectRectangle.Y = Math.Min(
                    game.Input.MousePosition.Y, multiSelectStart.Y);
                multiSelectRectangle.Width = Math.Abs(
                    game.Input.MousePosition.X - multiSelectStart.X);
                multiSelectRectangle.Height = Math.Abs(
                    game.Input.MousePosition.Y - multiSelectStart.Y);

                // Highlight selected entities in realtime
                HighlightMultiple(Filter(ObjectsFromRectangle(multiSelectRectangle)));
            }

            // Highlight etity on mouse hover
            else
            {
                if (!GameUI.Singleton.Overlaps(game.Input.MousePosition))
                {
                    Highlight(world.Pick() as GameObject);
                }
                else
                {
                    Highlight(null);
                }
            }

            // Remove dead members
            Selected.RemoveAll(delegate(GameObject o)
            {
                if (o != null && !o.IsAlive)
                {
                    o.Focused = false;
                    o.Selected = false;
                    return true;
                }

                return false;
            });

            highlighted.RemoveAll(delegate(GameObject o)
            {
                if (o != null && !o.IsAlive)
                {
                    o.Highlighted = false;
                    return true;
                }

                return false;
            });

            for (var i = 0; i < teams.Length; i++)
            {
                teams[i].RemoveAll(delegate(GameObject o)
                {
                    return o != null && !o.IsAlive;
                });
            }

            var hasRemoved = false;
            foreach (List<GameObject> list in Groups)
            {
                list.RemoveAll(delegate(GameObject o)
                {
                    if (o != null && !o.IsAlive)
                    {
                        hasRemoved = true;
                        o.Focused = false;
                        o.Selected = false;
                        return true;
                    }

                    return false;
                });
            }

            if (hasRemoved && Groups[CurrentGroupIndex].Count <= 0)
            {
                Focus(0);
            }

            if (hasRemoved)
            {
                SelectionDirty = true;
            }

            // Camera tracing
            if (camera != null)
            {
                // Stop tracing units if camera is interrupted by user
                if (camera.MovedByUser)
                {
                    traceCamera = false;
                }

                if (traceCamera)
                {
                    camera.FlyTo(ProjectToScreenCenter(GetCenter(Selected)), false);
                }
            }

            // Update attacker
            const double DisappearTime = 4;
            const float RevealRadius = 40;
            var elapsedTime = gameTime.ElapsedGameTime.TotalSeconds;
            for (var i = 0; i < attackers.Count; i++)
            {
                if ((attackTimer[i] += elapsedTime) > DisappearTime)
                {
                    attackers.RemoveAt(i);
                    attackTimer.RemoveAt(i);
                    i--;
                }
                else if (world.FogOfWar != null)
                {
                    world.FogOfWar.DrawVisibleArea(RevealRadius, attackers[i].Position.X,
                                                                 attackers[i].Position.Y);
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // Draw selection rectangle
            DrawSelectionRectangle(gameTime);
        }

        /// <summary>
        /// Vertex type used to draw 2D shapes.
        /// </summary>
        public struct VertexColor2D
        {
            public Vector3 Position;
            public int Color;

            public static readonly VertexElement[] VertexElement = new VertexElement[]
            {
                new VertexElement(0, 0, VertexElementFormat.Vector3,
                    VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 12, VertexElementFormat.Color,
                    VertexElementMethod.Default, VertexElementUsage.Color, 0),
            };
        }

        private void DrawSelectionRectangle(GameTime gameTime)
        {
            if (multiSelecting)
            {
                // 0 1
                // 2 3
                var corners = new Point[4];

                corners[0].X = corners[2].X = multiSelectRectangle.Left;
                corners[1].X = corners[3].X = multiSelectRectangle.Right;
                corners[0].Y = corners[1].Y = multiSelectRectangle.Top;
                corners[2].Y = corners[3].Y = multiSelectRectangle.Bottom;

                Color LineColor = Color.White;

                game.Graphics2D.DrawShadowedLine(corners[0], corners[1], LineColor, Color.Black);
                game.Graphics2D.DrawShadowedLine(corners[2], corners[0], LineColor, Color.Black);
                game.Graphics2D.DrawShadowedLine(corners[1], corners[3], LineColor, Color.Black);
                game.Graphics2D.DrawShadowedLine(corners[3], corners[2], LineColor, Color.Black);
            }
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            // Pass on the message to the selected
            foreach (GameObject o in Selected)
            {
                if (o.HandleEvent(type, sender, tag) == EventResult.Handled)
                {
                    return EventResult.Handled;
                }
            }

            var input = sender as Input;

            if (type == EventType.KeyDown && tag is Keys? && (tag as Keys?).Value == Keys.Space)
            {
                // Press space to back to spawnpoint
                if (game.Camera is GameCamera)
                {
                    (game.Camera as GameCamera).FlyTo(new Vector3(SpawnPoint, 0), false);
                }

                return EventResult.Handled;
            }

            if (type == EventType.KeyDown && tag is Keys? && (tag as Keys?).Value == Keys.F1)
            {
                // Press F1 to select the main hero
                LinkedList<GameObject> hero = GetObjects(HeroName);

                if (hero != null && hero.Count > 0)
                {
                    Select(hero.First.Value, false);
                }

                return EventResult.Handled;
            }

            // Left click to select an entity
            if (type == EventType.LeftButtonDown && !multiSelecting)
            {
                if (game.Camera is GameCamera)
                {
                    (game.Camera as GameCamera).Freezed = true;
                }

                input.Capture(this);
                multiSelecting = true;
                multiSelectStart = game.Input.MousePosition;
                multiSelectRectangle.X = game.Input.MousePosition.X;
                multiSelectRectangle.Y = game.Input.MousePosition.Y;
                multiSelectRectangle.Width = multiSelectRectangle.Height = 0;
            }
            else if (type == EventType.LeftButtonUp && multiSelecting)
            {
                if (game.Camera is GameCamera)
                {
                    (game.Camera as GameCamera).Freezed = false;
                }

                input.Uncapture();
                multiSelecting = false;
                var shift = input.Keyboard.IsKeyDown(Keys.LeftShift) ||
                             input.Keyboard.IsKeyDown(Keys.RightShift);

                if (multiSelectRectangle.Width == 0 && multiSelectRectangle.Height == 0)
                {
                    if (world.Pick() is GameObject)
                    {
                        Select(world.Pick() as GameObject, shift);
                    }
                }
                else
                {
                    Highlight(null);
                    SelectMultiple(Filter(ObjectsFromRectangle(multiSelectRectangle)), shift);
                }
            }

            // Double click to select entities of the same type within the screen
            if (type == EventType.DoubleClick && world.Pick() is GameObject)
            {
                var o = world.Pick() as GameObject;

                if (o.Owner is LocalPlayer)
                {
                    SelectMultiple(GetObjectsOfTheSameClass(o), false);
                }

                return EventResult.Handled;
            }

            // Handle teams
            if (type == EventType.KeyDown && tag is Keys?)
            {
                var value = KeyToNumber((tag as Keys?).Value, false);
                if (value >= 0 && value < 10)
                {
                    if (Selected.Count > 0)
                    {
                        // Add to team
                        if (input.Keyboard.IsKeyDown(Keys.LeftShift) ||
                            input.Keyboard.IsKeyDown(Keys.RightShift))
                        {
                            object first = null;
                            if (teams[value].Count > 0)
                            {
                                first = teams[value][0];
                            }

                            foreach (GameObject o in Selected)
                            {
                                if (teams[value].Contains(o) ||
                                    (first is Charactor && o is Building) ||
                                    (first is Building && o is Charactor))
                                {
                                    continue;
                                }

                                teams[value].Add(o);
                            }
                        }

                        // Create a new team
                        else if (input.Keyboard.IsKeyDown(Keys.LeftControl) ||
                                 input.Keyboard.IsKeyDown(Keys.RightControl))
                        {
                            teams[value].Clear();
                            teams[value].AddRange(Selected);
                        }
                    }

                    // Select a team
                    if (teams[value].Count > 0)
                    {
                        var currentSeconds = game.CurrentGameTime.TotalGameTime.TotalSeconds;

                        if (keyDoublePressed)
                        {
                            if ((currentSeconds - doubleClickTime) < Input.DoubleClickInterval)
                            {
                                keyDoublePressed = false;
                                if (game.Camera is GameCamera && teams[value].Count > 0)
                                {
                                    (game.Camera as GameCamera).FlyTo(
                                       ProjectToScreenCenter(GetTeamPosition(teams[value])), false);
                                }
                            }
                            else
                            {
                                doubleClickTime = currentSeconds;
                            }
                        }
                        else
                        {
                            keyDoublePressed = true;
                            doubleClickTime = currentSeconds;
                        }

                        SelectMultiple(teams[value], false);
                    }
                }
            }

            // Tab for subteam control
            if (type == EventType.KeyDown && tag is Keys? &&
                (tag as Keys?).Value == Keys.Tab && Groups != null && Groups.Count > 0)
            {
                SelectionDirty = true;
                Focus((CurrentGroupIndex + 1) % Groups.Count);

                return EventResult.Handled;
            }

            // Handle right click event
            if (Selected.Count > 0 && type == EventType.RightButtonDown)
            {
                Entity picked = world.Pick();
                Vector3? location = world.Landscape.Pick();
                EventResult handled = EventResult.Unhandled;

                var queueAction = game.Input.Keyboard.IsKeyDown(Keys.LeftShift) ||
                                   game.Input.Keyboard.IsKeyDown(Keys.RightShift);

                if (picked != null && world.FogOfWar != null &&
                                      world.FogOfWar.Contains(picked.Position.X, picked.Position.Y))
                {
                    picked = null;
                }

                if (picked != null)
                {
                    // Move to target
                    if (picked is GameObject)
                    {
                        (picked as GameObject).Flash();
                    }

                    foreach (GameObject o in Selected)
                    {
                        if (o != picked)
                        {
                            o.PerformAction(picked, queueAction);
                        }
                        else if (location.HasValue)
                        {
                            o.PerformAction(location.Value, queueAction);
                        }
                    }

                    return EventResult.Handled;
                }

                if (location.HasValue)
                {
                    PerformAction(location.Value);
                    return EventResult.Handled;
                }

                if (handled == EventResult.Handled)
                {
                    return EventResult.Handled;
                }
            }

            return EventResult.Unhandled;
        }

        private Vector2 GetTeamPosition(List<GameObject> list)
        {
            GameObject minObject = null;
            var min = float.MaxValue;

            foreach (GameObject o in list)
            {
                if (o.Priority < min)
                {
                    min = o.Priority;
                    minObject = o;
                }
            }

            return minObject != null ? new Vector2(minObject.Position.X, minObject.Position.Y) : Vector2.Zero;
        }

        public void SelectGroup(GameObject o)
        {
            for (var i = 0; i < Groups.Count; i++)
            {
                if (Groups[i].Contains(o))
                {
                    SelectMultiple(Groups[i], false);
                    break;
                }
            }
        }

        public void Focus(GameObject o)
        {
            for (var i = 0; i < Groups.Count; i++)
            {
                if (Groups[i].Contains(o))
                {
                    Focus(i);
                    break;
                }
            }
        }

        public void Focus(int newGroup)
        {
            if (newGroup >= 0 && newGroup < Groups.Count)
            {
                if (CurrentGroupIndex >= 0 && CurrentGroupIndex < Groups.Count)
                {
                    foreach (GameObject o in Groups[CurrentGroupIndex])
                    {
                        o.Focused = false;
                    }
                }

                CurrentGroupIndex = newGroup;
                foreach (GameObject o in Groups[CurrentGroupIndex])
                {
                    o.Focused = true;
                }

                // if (groups[currentGroup].Count > 0 && groups[currentGroup][0] is Charactor &&
                //    groups[currentGroup][0].Owner is LocalPlayer)
                // {
                //    GameUI.Singleton.SetUIElement(0, false, Attack.Button);
                //    GameUI.Singleton.SetUIElement(1, false, Move.Button);
                // }
                // else
                // {
                //    GameUI.Singleton.SetUIElement(0, false, null);
                //    GameUI.Singleton.SetUIElement(1, false, null);
                // }
            }
        }

        private Vector3 ProjectToScreenCenter(Vector2 position)
        {
            Vector3 center;
            center.X = position.X;
            center.Y = position.Y;
            center.Z = world.Landscape.GetHeight(position.X, position.Y);

            Ray ray = game.Unproject(game.ScreenWidth / 2, game.ScreenHeight / 2);

            var dot = Vector3.Dot(-Vector3.UnitZ, ray.Direction);

            if (dot == 0)
            {
                return center;
            }

            var distance = center.Z / dot;

            center += ray.Direction * distance;

            return center;
        }

        private int KeyToNumber(Keys key, bool numpad)
        {
            return numpad ? (int)key - (int)Keys.NumPad0 : (int)key - (int)Keys.D0;
        }

        public void PerformAction(Vector3 location)
        {
            var queueAction = game.Input.IsShiftPressed;

            // Move to a location on the map
            if (Selected.Count > 0 &&
                Selected[0] is Charactor && Selected[0].Owner is LocalPlayer)
            {
                // Send out the orders
                foreach (KeyValuePair<GameObject, Vector2> pair in
                         CreateSquardPositions(Selected, location))
                {
                    pair.Key.PerformAction(new Vector3(pair.Value, 0), queueAction);
                }

                // Let the camera follow units
                if (camera != null && game.Settings.TraceUnits)
                {
                    traceCamera = true;
                    camera.MovedByUser = false;
                }
            }
            else
            {
                foreach (GameObject o in Selected)
                {
                    if (o.Owner is LocalPlayer)
                    {
                        o.PerformAction(location, queueAction);
                    }
                }
            }

            location.Z = world.Landscape.GetHeight(location.X, location.Y);
            GameUI.Singleton.SetCursorFocus(location, Color.Green);
        }

        public void AttackTo(Vector3 location)
        {
            // Move to a location on the map
            if (Selected.Count > 0 && Selected[0] is Charactor &&
                Selected[0].Owner is LocalPlayer)
            {
                foreach (KeyValuePair<GameObject, Vector2> pair in
                         CreateSquardPositions(Selected, location))
                {
                    (pair.Key as Charactor).AttackTo(
                        new Vector3(pair.Value, 0), game.Input.IsShiftPressed);
                }

                // Let the camera follow units
                if (camera != null && game.Settings.TraceUnits)
                {
                    traceCamera = true;
                    camera.MovedByUser = false;
                }
            }
        }

        public void AttackTo(GameObject target)
        {
            if (Selected.Count > 0 && Selected[0] is Charactor &&
                Selected[0].Owner is LocalPlayer)
            {
                foreach (Charactor c in Selected)
                {
                    c.AttackTo(target, game.Input.IsShiftPressed);
                }
            }
        }

        public void MoveTo(Vector3 location)
        {
            // Move to a location on the map
            if (Selected.Count > 0 && Selected[0] is Charactor &&
                Selected[0].Owner is LocalPlayer)
            {
                foreach (KeyValuePair<GameObject, Vector2> pair in
                         CreateSquardPositions(Selected, location))
                {
                    (pair.Key as Charactor).MoveTo(
                        new Vector3(pair.Value, 0), game.Input.IsShiftPressed);
                }

                // Let the camera follow units
                if (camera != null && game.Settings.TraceUnits)
                {
                    traceCamera = true;
                    camera.MovedByUser = false;
                }
            }
        }

        public void MoveTo(GameObject target)
        {
            if (Selected.Count > 0 && Selected[0] is Charactor &&
                Selected[0].Owner is LocalPlayer)
            {
                foreach (Charactor c in Selected)
                {
                    c.MoveTo(target, game.Input.IsShiftPressed);
                }
            }
        }
    }

    public class ComputerPlayer : Player
    {
        public GameWorld World;
        public GoalDevelop Develop;
        public GoalAttack Attack;
        public GoalDefend Defend;

        /// <summary>
        /// A dictionary storing each request and its priority.
        /// </summary>
        public Dictionary<string, float> Requests = new();

        /// <summary>
        /// Gets the first townhall.
        /// </summary>
        public Building Townhall
        {
            get
            {
                if (townhall == null || !townhall.IsAlive)
                {
                    if (Objects.TryGetValue(TownhallName, out LinkedList<GameObject> value) && value.Count > 0)
                    {
                        townhall = value.First.Value as Building;
                    }
                }

                return townhall;
            }
        }

        private Building townhall;

        /// <summary>
        /// Gets the enermy of the player.
        /// </summary>
        public Player Enermy
        {
            get
            {
                if (enermy == null)
                {
                    foreach (Player player in Player.AllPlayers)
                    {
                        if (GetRelation(player) == PlayerRelation.Opponent)
                        {
                            enermy = player;
                            break;
                        }
                    }
                }

                return enermy;
            }
        }

        private Player enermy;

        /// <summary>
        /// Gets the default rally point.
        /// </summary>
        public Vector3 DefaultRallyPoint => defaultRallyPoint;

        private Vector3 defaultRallyPoint;

        /// <summary>
        /// Start running the computer player.
        /// </summary>
        public override void Start(GameWorld world)
        {
            World = world ?? throw new ArgumentNullException();

            // Computer default rally point
            Vector2 toPlayer = Enermy.SpawnPoint - SpawnPoint;

            toPlayer.Normalize();
            toPlayer *= Helper.RandomInRange(50, 150);

            defaultRallyPoint.X = SpawnPoint.X + toPlayer.X;
            defaultRallyPoint.Y = SpawnPoint.Y + toPlayer.Y;
            defaultRallyPoint.Z = 0;

            // Init goals
            Develop = new GoalDevelop(world, this);
            Attack = new GoalAttack(world, this);
            Defend = new GoalDefend(world, this);
        }

        /// <summary>
        /// Request to build/train/upgrade with the specified count and priority scaler.
        /// </summary>
        public void Request(string type, int count, float scaler)
        {
            FutureObjects.TryGetValue(type, out var existingCount);

            count -= existingCount;
            Request(type, count > 0 ? scaler : 0.5f);
        }

        /// <summary>
        /// Request to build/train/upgrade with the specified evaluation.
        /// </summary>
        public void Request(string type, float evaluation)
        {
            if (!Requests.ContainsKey(type))
            {
                Requests.Add(type, evaluation);
            }
            else
            {
                Requests[type] = MathHelper.Lerp(Requests[type], evaluation, 0.5f);
            }

            // Check dependencis
            // if (!CheckDependency(type))
            // {
            //    foreach (KeyValuePair<string, string> pair in Dependencies)
            //    {
            //        if (type == pair.Key && !IsFutureAvailable(pair.Value))
            //        {
            //            // Dependency table should not contain any circle,
            //            // or this method will fail.
            //            if (Requests.ContainsKey(pair.Value) &&
            //                Requests[pair.Value] < evaluation * 1.5f)
            //                Request(pair.Value, evaluation * 1.5f);
            //        }
            //    }
            // }
        }

        /// <summary>
        /// Checks if we have enough money to do a certain thing.
        /// </summary>
        public bool HasEnoughMoney(string type)
        {
            var gold = GameDefault.Singleton.GetGold(type);
            var lumber = GameDefault.Singleton.GetLumber(type);
            var food = IsBuilding(type) ? 0 : GameDefault.Singleton.GetFood(type);

            return gold <= Gold && lumber <= Lumber &&
                   (food == 0 || (food > 0 && food <= FoodCapacity - Food));
        }

        /// <summary>
        /// Construct a building of the specified typ.
        /// </summary>
        public void Construct(string type)
        {
            Vector3 startPosition = Townhall != null ? Townhall.Position : new Vector3(SpawnPoint, 0);

            // Townhall should be treated seperately
            if (CheckDependency(type))
            {
                var queue = false;
                Worker builder = null;
                var minCost = int.MaxValue;

                // Find a peon
                foreach (GameObject o in EnumerateObjects(WorkerName))
                {
                    if (o is not Worker p)
                    {
                        continue;
                    }

                    var cost = 10;

                    if (p.State is StateCharactorIdle)
                    {
                        cost = 0;
                    }
                    else if (p.State is StateHarvestLumber)
                    {
                        cost = 1;
                    }
                    else if (p.State is StateHarvestGold)
                    {
                        cost = 2;
                    }

                    if (cost < minCost)
                    {
                        minCost = cost;
                        builder = p;
                        queue = p.State is StateConstruct;
                    }
                }

                if (builder != null)
                {
                    if (World.Create(type) is not Building building)
                    {
                        throw new ArgumentException();
                    }

                    // Find a place to build
                    float Radius = 80;
                    var valid = false;
                    var counter = 0;

                    while (!valid)
                    {
                        Vector3 position = startPosition;

                        position.X += Helper.RandomInRange(-Radius, Radius);
                        position.Y += Helper.RandomInRange(-Radius, Radius);

                        building.Position = position;
                        valid = building.IsLocationPlacable();

                        if (++counter > 2)
                        {
                            counter = 0;
                            Radius += 20;
                        }
                    }

                    // Place the building
                    building.Owner = this;
                    building.BeginPlace();
                    building.Fall();
                    building.Place();
                    building.PerformAction(defaultRallyPoint, false);
                    World.Add(building);
                    IState state = builder.State;
                    builder.PerformAction(building, queue);
                    builder.QueuedStates.Enqueue(state);
                }
            }
        }

        /// <summary>
        /// Trains a unit of a given type.
        /// </summary>
        public void Train(string type)
        {
            Building building = null;
            var minRequest = int.MaxValue;

            foreach (GameObject o in EnumerateObjects())
            {
                if (o is Building b && b.CanTrain(type) && b.QueuedSpells.Count < minRequest)
                {
                    minRequest = b.QueuedSpells.Count;
                    building = b;
                }
            }

            if (building != null)
            {
                building.TrainUnit(type);
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (Develop != null)
            {
                Develop.Update(gameTime);
            }

            if (Attack != null)
            {
                Attack.Update(gameTime);
            }

            if (Defend != null)
            {
                Defend.Update(gameTime);
            }
        }
    }
}

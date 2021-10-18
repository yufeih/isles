//-----------------------------------------------------------------------------
//  Isles v1.0
//
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Isles.Graphics;
using Isles.Engine;
using Isles.UI;

namespace Isles
{

    /// <summary>
    /// Game replay and network game uses this interface
    /// </summary>
    public interface IGameObject
    {
        /// <summary>
        /// Serialize any change of this recordable
        /// </summary>
        void Serialize(Stream output);

        /// <summary>
        /// Deserialize this recordable from input bytes
        /// </summary>
        void Deserialize(Stream input);
    }

    /// <summary>
    /// Base class for all game objects.
    /// Incoorperating game logic specific members.
    /// </summary>
    public abstract class GameObject : Entity, ISelectable, IGameObject
    {
        /// <summary>
        /// Gets or sets the owner of this charactor
        /// </summary>
        public Player Owner
        {
            get => owner;

            set => owner = value;
        }

        private Player owner;

        /// <summary>
        /// Gets or sets the priority of the game object
        /// </summary>
        public float Priority
        {
            get => priority;
            set => priority = value;
        }

        private float priority;

        /// <summary>
        /// Gets or sets the icon for this game object
        /// </summary>
        public Icon Icon => icon;

        private Icon icon;

        /// <summary>
        /// Gets or sets the snap for this game object
        /// </summary>
        public Icon Snapshot => snapshot;

        private Icon snapshot;

        /// <summary>
        /// Gets snapshot texture
        /// </summary>
        public static Texture2D SnapshotTexture
        {
            get
            {
                if (snapshotTexture == null)
                {
                    snapshotTexture = BaseGame.Singleton.ZipContent.Load<Texture2D>("UI/Snapshots");
                }

                return snapshotTexture;
            }
        }

        private static Texture2D snapshotTexture;

        private SpellButton profileButton;

        /// <summary>
        /// Gets or sets the profile button
        /// </summary>
        public SpellButton ProfileButton
        {
            get => profileButton;
            set => profileButton = value;
        }

        private TipBox tip;

        /// <summary>
        /// Gets or sets the tip for the object
        /// </summary>
        public TipBox Tip
        {
            get => tip;
            set => tip = value;
        }

        /// <summary>
        /// Gets or sets the view distance of this game object
        /// </summary>
        public float ViewDistance = 100;

        /// <summary>
        /// Gets or sets the radius of selection circle
        /// </summary>
        public float SelectionAreaRadius = 10;

        /// <summary>
        /// Gets or sets the sound effect associated with this game object
        /// </summary>
        public string Sound;

        /// <summary>
        /// Gets or sets the sound effect for combat
        /// </summary>
        public string SoundCombat;

        /// <summary>
        /// Gets or sets the sound effect for die
        /// </summary>
        public string SoundDie;

        /// <summary>
        /// Gets or sets the health of this game object
        /// </summary>
        public float Health
        {
            get => health;

            set
            {
                if (value > maximumHealth)
                {
                    value = maximumHealth;
                }

                // Cannot reborn
                if (value > 0 && health <= 0)
                {
                    value = 0;
                }

                if (value <= 0 && health > 0)
                {
                    // Clear all spells
                    foreach (Spell spell in Spells)
                    {
                        spell.Enable = false;
                    }

                    Spells.Clear();

                    if (SoundDie != null && ShouldDrawModel)
                    {
                        Audios.Play(SoundDie, this);
                    }

                    if (Highlighted)
                    {
                        Highlighted = false;
                    }

                    if (Selected)
                    {
                        Selected = false;
                    }

                    OnDie();
                }

                if (value < health && health > 0 && maximumHealth > 0 && owner is LocalPlayer)
                {
                    Audios.Play("UnderAttack", Audios.Channel.UnderAttack, null);
                }

                if (value < 0)
                {
                    value = 0;
                }

                health = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum health of this charactor
        /// </summary>
        public float MaximumHealth
        {
            get => maximumHealth;

            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (maximumHealth < health)
                {
                    Health = maximumHealth;
                }

                maximumHealth = value;
            }
        }

        /// <summary>
        /// Gets whether the game object is alive
        /// </summary>
        public bool IsAlive => health > 0 || (health <= 0 && maximumHealth <= 0);

        private float maximumHealth;
        private float health;

        /// <summary>
        /// Gets or sets game object spells
        /// </summary>
        public List<Spell> Spells = new();

        /// <summary>
        /// Gets or sets if the game object is selected
        /// </summary>
        public bool Selected
        {
            get => selected;

            set
            {
                selected = value;

                if (selected)
                {
                    OnSelect(GameUI.Singleton);
                }
                else
                {
                    OnDeselect(GameUI.Singleton);
                }
            }
        }

        private bool selected;

        /// <summary>
        /// Gets of sets if the game object is highlighted
        /// </summary>
        public bool Highlighted
        {
            get => highlighted;

            set
            {
                highlighted = value;

                if (Model != null)
                {
                    Model.Glow = highlighted ? Vector4.One : Vector4.Zero;
                }

                if (highlighted && owner != null && ShouldDrawModel)
                {
                    if (tip == null)
                    {
                        tip = CreateTipBox();
                    }

                    GameUI.Singleton.TipBoxContainer.Add(tip);
                }
                else if (!highlighted && owner != null)
                {
                    GameUI.Singleton.TipBoxContainer.Remove(tip);
                }
            }
        }

        private bool highlighted;

        /// <summary>
        /// Gets of sets if the game object is currently focused
        /// </summary>
        public bool Focused
        {
            get => focused;

            set
            {
                focused = value;

                if (value)
                {
                    ShowSpells(GameUI.Singleton);
                }
            }
        }

        private bool focused;

        /// <summary>
        /// Gets the top center position of the game object
        /// </summary>
        public Vector3 TopCenter
        {
            get
            {
                Vector3 v = Position;
                v.Z = BoundingBox.Max.Z;
                return v;
            }
        }

        /// <summary>
        /// Min/Max attack point
        /// </summary>
        public Vector2 AttackPoint;

        /// <summary>
        /// Min/Max defense point
        /// </summary>
        public Vector2 DefensePoint;

        /// <summary>
        /// Gets or sets the min/max attack range of this charactor
        /// </summary>
        public Vector2 AttackRange;

        /// <summary>
        /// Gets or sets the duration of each individual attack
        /// </summary>
        public float AttackDuration;

        /// <summary>
        /// Flash related stuff
        /// </summary>
        private const float FlashDuration = 0.5f;
        private float flashElapsedTime = FlashDuration + 0.1f;

        /// <summary>
        /// Get or sets the attachment of the game model
        /// </summary>
        public List<KeyValuePair<GameModel, int>> Attachment = new();

        public GameObject(GameWorld world)
            : base(world) { }

        public GameObject(GameWorld world, string classID)
            : base(world)
        {

            if (GameDefault.Singleton.WorldObjectDefaults.TryGetValue(classID, out XmlElement xml))
            {
                Deserialize(xml);
            }
        }

        /// <summary>
        /// Make the game object flash
        /// </summary>
        public void Flash()
        {
            flashElapsedTime = 0;
        }

        /// <summary>
        /// Serialize any changes made to the game object
        /// </summary>
        public virtual void Serialize(Stream stream)
        {

        }

        /// <summary>
        /// Deserialize any changes made to the game object
        /// </summary>
        public virtual void Deserialize(Stream stream)
        {
        }

        /// <summary>
        /// Deserialize the game object from an xml element
        /// </summary>
        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);

            if (xml.HasAttribute("Health"))
            {
                float.TryParse(xml.GetAttribute("Health"), out health);
            }

            if (xml.HasAttribute("MaxHealth"))
            {
                float.TryParse(xml.GetAttribute("MaxHealth"), out maximumHealth);
            }

            if (health > maximumHealth)
            {
                maximumHealth = health;
            }

            if (xml.HasAttribute("Owner"))
            {
                owner = Player.FromID(int.Parse(xml.GetAttribute("Owner")));
            }

            if (xml.HasAttribute("Priority"))
            {
                priority = float.Parse(xml.GetAttribute("Priority"));
            }

            if (xml.HasAttribute("AreaRadius"))
            {
                SelectionAreaRadius = float.Parse(xml.GetAttribute("AreaRadius"));
            }

            if (xml.HasAttribute("Attack"))
            {
                AttackPoint = Helper.StringToVector2(xml.GetAttribute("Attack"));
            }

            if (xml.HasAttribute("Defense"))
            {
                DefensePoint = Helper.StringToVector2(xml.GetAttribute("Defense"));
            }

            // Make sure max (Y) is greater or equal then min (X)
            if (AttackPoint.Y < AttackPoint.X)
            {
                AttackPoint.Y = AttackPoint.X;
            }

            if (DefensePoint.Y < DefensePoint.X)
            {
                DefensePoint.Y = DefensePoint.X;
            }

            if (xml.HasAttribute("AttackDuration"))
            {
                AttackDuration = float.Parse(xml.GetAttribute("AttackDuration"));
            }

            if (xml.HasAttribute("AttackRange"))
            {
                AttackRange = Helper.StringToVector2(xml.GetAttribute("AttackRange"));
            }

            if (xml.HasAttribute("ViewDistance"))
            {
                ViewDistance = float.Parse(xml.GetAttribute("ViewDistance"));
            }

            // Initialize attachments
            if (xml.HasAttribute("Attachment"))
            {
                var value = xml.GetAttribute("Attachment");

                var items = value.Split(new char[] { '|' });

                for (var i = 0; i < items.Length; i += 2)
                {
                    var model = new GameModel(items[i]);
                    var attachPoint = Model.GetBone(items[i + 1]);

                    if (attachPoint < 0)
                    {
                        throw new Exception("Bone '" + items[i + 1] + "' do not exist in model '" + items[i] + "'.");
                    }

                    Attachment.Add(new KeyValuePair<GameModel, int>(model, attachPoint));
                }
            }

            var iconIndex = 0;
            if (xml.HasAttribute("Icon"))
            {
                iconIndex = int.Parse(xml.GetAttribute("Icon"));
                icon = Icon.FromTiledTexture(iconIndex);

                profileButton = new SpellButton
                {
                    Texture = icon.Texture,
                    SourceRectangle = icon.Region,
                    Hovered = Icon.RectangeFromIndex(iconIndex + 1),
                    Pressed = Icon.RectangeFromIndex(iconIndex + 2),
                    Anchor = Anchor.BottomLeft,
                    ScaleMode = ScaleMode.ScaleY
                };

                profileButton.Click += new EventHandler(delegate (object sender, EventArgs e)
                {
                    Player.LocalPlayer.Focus(this);
                });

                profileButton.DoubleClick += new EventHandler(delegate (object sender, EventArgs e)
                {
                    Player.LocalPlayer.SelectGroup(this);
                });
            }

            if (xml.HasAttribute("Snapshot") &&
                int.TryParse(xml.GetAttribute("Snapshot"), out iconIndex))
            {
                snapshot = Icon.FromTiledTexture(iconIndex, 8, 4, SnapshotTexture);
            }

            if (xml.HasAttribute("Sound"))
            {
                Sound = xml.GetAttribute("Sound");
            }

            if (xml.HasAttribute("SoundCombat"))
            {
                SoundCombat = xml.GetAttribute("SoundCombat");
            }

            if (xml.HasAttribute("SoundDie"))
            {
                SoundDie = xml.GetAttribute("SoundDie");
            }

            // Initialize spells
            if (xml.HasAttribute("Spells"))
            {
                var spells = xml.GetAttribute("Spells").Split(new char[] { ',', ' ', '\n', '\r' });

                for (var i = 0; i < spells.Length; i++)
                {
                    if (spells[i].Length > 0)
                    {
                        AddSpell(spells[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Add a new spell for the game object
        /// </summary>
        /// <param name="spell"></param>
        public void AddSpell(string name)
        {
            Dictionary<string, XmlElement> objectConfig = GameDefault.Singleton.WorldObjectDefaults;
            Dictionary<string, XmlElement> spellConfig = GameDefault.Singleton.SpellDefaults;

            var spell = Spell.Create(name, World);
            if (objectConfig.ContainsKey(name))
            {
                spell.Deserialize(objectConfig[name]);
            }

            if (spellConfig.ContainsKey(name))
            {
                spell.Deserialize(spellConfig[name]);
            }

            spell.Owner = this;
            OnCreateSpell(spell);
            Spells.Add(spell);

            if (Selected && Focused)
            {
                ShowSpells(GameUI.Singleton);
            }
        }

        protected virtual void OnCreateSpell(Spell spell) { }
        public virtual void PerformAction(Vector3 position, bool queueAction) { }
        public virtual void PerformAction(Entity entity, bool queueAction) { }

        public override void OnCreate()
        {
            UpdateFogOfWar();

            if (owner != null)
            {
                //Model.Tint = owner.TeamColor.ToVector3();

                if (AttackPoint.X > 0)
                {
                    AttackPoint.X += owner.AttackPoint;
                    AttackPoint.Y += owner.AttackPoint;
                }

                if (DefensePoint.X > 0)
                {
                    DefensePoint.X += owner.DefensePoint;
                    DefensePoint.Y += owner.DefensePoint;
                }
            }
        }

        public override float? Intersects(Ray ray)
        {
            return IsAlive ? base.Intersects(ray) : null;
        }

        public override bool Intersects(BoundingFrustum frustum)
        {
            return IsAlive && base.Intersects(frustum);
        }

        /// <summary>
        /// Gets the relationship with another game object
        /// </summary>
        public PlayerRelation GetRelation(GameObject gameObject)
        {
            return owner == null || gameObject == null || gameObject.owner == null ? PlayerRelation.Neutral : owner.GetRelation(gameObject.owner);
        }

        public bool IsAlly(GameObject gameObject)
        {
            return GetRelation(gameObject) == PlayerRelation.Ally;
        }

        public bool IsOpponent(GameObject gameObject)
        {
            return GetRelation(gameObject) == PlayerRelation.Opponent;
        }

        public override void Update(GameTime gameTime)
        {
            // Update spells
            foreach (Spell spell in Spells)
            {
                spell.Update(gameTime);
            }

            // Update fog of war state for other players
            UpdateFogOfWar();

            base.Update(gameTime);

            // Update attachments after model is updated
            foreach (KeyValuePair<GameModel, int> attach in Attachment)
            {
                if (attach.Value >= 0)
                {
                    attach.Key.Transform = Model.GetBoneTransform(attach.Value);
                }
            }

            // Draw fog of war
            if (owner != null && Visible && World.FogOfWar != null &&
                (owner is LocalPlayer || World.Game.Settings.RevealMap))
            {
                DrawFogOfWar();
            }
        }

        private void UpdateFogOfWar()
        {
            if (Visible && !(owner is LocalPlayer) && World.FogOfWar != null)
            {
                var nowState = World.FogOfWar.Contains(Position.X, Position.Y);

                if (nowState && !InFogOfWar)
                {
                    // Enter fog of war
                    EnterFogOfWar();
                    InFogOfWar = true;
                }
                else if (!nowState && InFogOfWar)
                {
                    // Leave fog of war
                    LeaveFogOfWar();
                    InFogOfWar = false;
                }
            }
        }

        public static Texture2D SelectionAreaTexture
        {
            get
            {
                if (selectionAreaTexture == null || selectionAreaTexture.IsDisposed)
                {
                    selectionAreaTexture = BaseGame.Singleton.ZipContent.Load<Texture2D>("Textures/SelectionArea");
                }

                return selectionAreaTexture;
            }
        }

        private static Texture2D selectionAreaTexture;

        public static Texture2D SelectionAreaTextureLarge
        {
            get
            {
                if (selectionAreaTextureLarge == null || selectionAreaTexture.IsDisposed)
                {
                    selectionAreaTextureLarge = BaseGame.Singleton.ZipContent.Load<Texture2D>("Textures/SelectionAreaLarge");
                }

                return selectionAreaTextureLarge;
            }
        }

        private static Texture2D selectionAreaTextureLarge;

        public bool ShowStatus = true;

        /// <summary>
        /// Gets or sets whether this game object is currently in the fog of war
        /// </summary>
        public bool InFogOfWar;

        /// <summary>
        /// Gets or sets whether this game object will be shown in the fog of war
        /// </summary>
        public bool VisibleInFogOfWar = true;

        /// <summary>
        /// Colors
        /// </summary>
        private static readonly Vector3 Green = Color.Green.ToVector3();
        private static readonly Vector3 Yellow = Color.Yellow.ToVector3();
        private static readonly Vector3 Red = Color.Red.ToVector3();

        /// <summary>
        /// Gets whether models should be drawed
        /// </summary>
        public bool ShouldDrawModel => !InFogOfWar && Visible && WithinViewFrustum;

        public override void Draw(GameTime gameTime)
        {
            // Flash the model
            if (flashElapsedTime <= FlashDuration)
            {
                var glow = (float)Math.Sin(MathHelper.Pi * flashElapsedTime / FlashDuration);
                Model.Glow = new Vector4(MathHelper.Clamp(glow, 0, 1));

                flashElapsedTime += (float)(gameTime.ElapsedGameTime.TotalSeconds);
                if (flashElapsedTime > FlashDuration)
                {
                    Model.Glow = Vector4.UnitW;
                }
            }

            // Draw model
            if (!InFogOfWar)
            {
                base.Draw(gameTime);

                if (owner != null)
                {
                    GameUI.Singleton.Minimap.DrawGameObject(Position, 4, owner.TeamColor);
                }
            }

            // Draw copyed model shadow
            if (InFogOfWar && Spotted && VisibleInFogOfWar && modelShadow != null)
            {
                if (WithinViewFrustum)
                {
                    modelShadow.Draw(gameTime);
                }

                if (owner != null)
                {
                    GameUI.Singleton.Minimap.DrawGameObject(Position, 4, owner.TeamColor);
                }
            }

            // Draw attachments
            if (ShouldDrawModel)
            {
                DrawAttachments(gameTime);
            }

            // Draw status
            if (Visible && !InFogOfWar && WithinViewFrustum && ShowStatus)
            {
                if (Selected && IsAlive)
                {
                    World.Landscape.DrawSurface(SelectionAreaRadius > 16 ?
                                                SelectionAreaTextureLarge : SelectionAreaTexture, Position,
                                                SelectionAreaRadius * 2, SelectionAreaRadius * 2,
                                                owner == null ? Color.Yellow : (
                                                owner.GetRelation(Player.LocalPlayer) == PlayerRelation.Opponent ?
                                                Color.Red : Color.GreenYellow));
                }

                if (IsAlive && maximumHealth > 0)
                {
                    if (Highlighted || World.Game.Input.Keyboard.IsKeyDown(Keys.LeftAlt) ||
                                       World.Game.Input.Keyboard.IsKeyDown(Keys.RightAlt))
                    {
                        Color color = ColorFromPercentage(1.0f * health / maximumHealth);
                        GameUI.Singleton.DrawProgress(TopCenter, 0, (int)(SelectionAreaRadius * 10.0f),
                            100 * health / maximumHealth, color);
                        DrawStatus();
                    }
                }
            }
        }

        protected virtual void DrawAttachments(GameTime gameTime)
        {
            foreach (KeyValuePair<GameModel, int> attach in Attachment)
            {
                attach.Key.Draw(gameTime);
            }
        }

        protected virtual TipBox CreateTipBox()
        {
            TextField content = null;
            var title = new TextField(Name, 16f / 23, Color.Gold,
                                            new Rectangle(0, 6, 150, 20))
            {
                Centered = true
            };
            if (Owner.Name != null && Owner.Name != "")
            {
                content = new TextField(Owner.Name, 15f / 23, Color.White,
                                        new Rectangle(0, 25, 150, 20))
                {
                    Centered = true
                };
            }
            var tip = new TipBox(150, title.RealHeight +
                                        (content != null ? content.RealHeight : 0) + 20);

            tip.Add(title);
            if (content != null)
            {
                tip.Add(content);
            }

            return tip;
        }

        protected virtual void DrawFogOfWar()
        {
            World.FogOfWar.DrawVisibleArea(ViewDistance, Position.X, Position.Y);
        }

        protected virtual void DrawStatus() { }

        public static Color ColorFromPercentage(float percentage)
        {
            Vector3 v, v1, v2;

            if (percentage > 0.5f)
            {
                percentage = (percentage - 0.5f) * 2;
                v1 = Yellow;
                v2 = Green;
            }
            else
            {
                percentage *= 2;
                v1 = Red;
                v2 = Yellow;
            }

            v.X = MathHelper.Lerp(v1.X, v2.X, percentage);
            v.Y = MathHelper.Lerp(v1.Y, v2.Y, percentage);
            v.Z = MathHelper.Lerp(v1.Z, v2.Z, percentage);

            return new Color(v);
        }

        public override void DrawShadowMap(GameTime gameTime, ShadowEffect shadow)
        {
            if (!InFogOfWar)
            {
                base.DrawShadowMap(gameTime, shadow);
            }
            else if (modelShadow != null && IsVisible(shadow.ViewProjection))
            {
                modelShadow.DrawShadowMap(gameTime, shadow);
            }
        }

        public override void DrawReflection(GameTime gameTime, Matrix view, Matrix projection)
        {
            if (!InFogOfWar)
            {
                base.DrawReflection(gameTime, view, projection);
            }
        }

        /// <summary>
        /// Whether this game object has been spotted by local player
        /// </summary>
        public bool Spotted;

        /// <summary>
        /// Copyed model for drawing in the fog of war
        /// </summary>
        private GameModel modelShadow;

        protected virtual void LeaveFogOfWar()
        {
            modelShadow = null;
            Spotted = true;
        }

        protected virtual void EnterFogOfWar()
        {
            // Create a model shadow, draw the shadow model when we're in fog of war
            if (VisibleInFogOfWar)
            {
                modelShadow = Model.ShadowCopy();
                modelShadow.Tint *= 0.3f;
            }
        }

        /// <summary>
        /// Gets the game model attached to the specified bone
        /// </summary>
        public GameModel GetAttachment(string boneName)
        {
            var bone = Model.GetBone(boneName);

            if (bone < 0)
            {
                return null;
            }

            foreach (KeyValuePair<GameModel, int> pair in Attachment)
            {
                if (pair.Value == bone)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        public static float ComputeHit(GameObject from, GameObject to)
        {
            // A * (  K + clamp(1 - D / A) * (1 - K) ) * 10

            const float Scaler = 1.0f;
            const float MaxWeakeness = 0.4f;
            const float BuildingWeakeness = 0.5f;

            var attack = Helper.RandomInRange(from.AttackPoint.X, from.AttackPoint.Y);
            var defense = Helper.RandomInRange(to.DefensePoint.X, to.DefensePoint.Y);

            if (attack < 0)
            {
                attack = 0;
            }

            if (defense < 0)
            {
                defense = 0;
            }

            var value = attack * (MaxWeakeness +
                MathHelper.Clamp(1 - defense / attack, 0, 1) * (1 - MaxWeakeness)) * Scaler;

            return (to is Building) ? value * BuildingWeakeness : value;
        }

        /// <summary>
        /// Virtual methods
        /// </summary>
        /// <param name="ui"></param>
        protected virtual void OnSelect(GameUI ui)
        {
            Flash();
        }

        protected virtual void OnDeselect(GameUI ui)
        {

        }

        protected virtual void ShowSpells(GameUI ui)
        {
            ui.ClearUIElement();

            if (owner is LocalPlayer)
            {
                for (var i = 0; i < Spells.Count; i++)
                {
                    ui.SetUIElement(i, true, Spells[i].Button);
                }
            }
        }

        protected virtual void OnDie() { }

        /// <summary>
        /// Called after gameworld/UI are initialized
        /// </summary>
        public virtual void Start(GameWorld world) { }

        /// <summary>
        /// Press the attack
        /// </summary>
        public virtual void TriggerAttack(Entity target) { }
    }

    public class Tree : GameObject
    {
        /// <summary>
        /// Whether trees are pickable
        /// </summary>
        public static bool Pickable;

        /// <summary>
        /// Gets or sets how many wood the tree can provide
        /// </summary>
        public int Lumber
        {
            get => lumber;

            set
            {
                if (everGreen && value < 100)
                {
                    value = 100;
                }

                lumber = value;
            }
        }

        private int lumber = 50;

        /// <summary>
        /// Gets or sets whether this tree is ever green
        /// </summary>
        public bool EverGreen
        {
            get => everGreen;
            set
            {
                if (Lumber > 0)
                {
                    everGreen = value;
                }
            }
        }

        private bool everGreen;

        /// <summary>
        /// Gets or sets how many peons are harvesting this tree
        /// </summary>
        public int HarvesterCount
        {
            get => harvesterCount;

            set
            {
                System.Diagnostics.Debug.Assert(
                    value >= 0 && value <= StateHarvestLumber.MaxPeonsPerTree);

                harvesterCount = value;
            }
        }

        private int harvesterCount;
        private Vector3 rotationAxis;
        private float shakeTime;
        private float totalShakeTime = 0.2f;
        private float maxShakeAngle;
        private Quaternion treeRotation;
        private readonly Random random = new();
        private List<Point> pathGrids = new();

        public Tree(GameWorld world) : base(world, "Tree")
        {
            ShowStatus = false;
            Spotted = true;
        }

        public override void Deserialize(XmlElement xml)
        {
            string value;
            if ((value = xml.GetAttribute("Lumber")) != "")
            {
                lumber = int.Parse(value);
            }

            base.Deserialize(xml);

            // Randomize scale and rotation
            var size = Helper.RandomInRange(0.9f, 1.1f);
            Scale = new Vector3(size, size, Helper.RandomInRange(0.9f, 1.1f));
            Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Helper.RandomInRange(0, 2 * MathHelper.Pi));
        }

        public override float? Intersects(Ray ray)
        {
            return Pickable ? base.Intersects(ray) : null;
        }

        public override bool Intersects(BoundingFrustum frustum)
        {
            return Pickable && base.Intersects(frustum);
        }

        public void Hit(IWorldObject hitter)
        {
            if (lumber > 0 && hitter != null && shakeTime <= 0)
            {
                var toSender = Vector3.Subtract(hitter.Position, Position);
                toSender.Normalize();
                rotationAxis = Vector3.Cross(toSender, Vector3.UnitZ);
                shakeTime = totalShakeTime;
                maxShakeAngle = MathHelper.ToRadians(5 + (float)(random.NextDouble() * 5));
            }

            if (lumber <= 0 && pathGrids != null)
            {
                maxShakeAngle = MathHelper.ToRadians(240);
                shakeTime = totalShakeTime = 2.0f;

                var o = hitter as GameObject;
                if (o != null && o.Owner != null)
                {
                    o.Owner.TreesCuttedDown++;
                }

                Audios.Play("TreeFall", o);

                // Remove this obstacle
                World.PathManager.Unmark(pathGrids);
                pathGrids = null;
            }
        }

        public override void OnCreate()
        {
            treeRotation = Rotation;

            pathGrids.AddRange(World.PathManager.EnumerateGridsInCircle(
                               new Vector2(Position.X, Position.Y), 4));
            World.PathManager.Mark(pathGrids);
        }

        private ParticleEffect glow;
        private ParticleEffect star;

        public override void Update(GameTime gameTime)
        {
            if (everGreen && ShouldDrawModel)
            {
                if (star == null)
                {
                    star = new EffectStar(World, this);

                    if (Helper.Random.Next(10) == 0)
                    {
                        glow = new EffectGlow(World, this);
                    }
                }

                if (glow != null)
                {
                    glow.Update(gameTime);
                }

                if (star != null)
                {
                    star.Update(gameTime);
                }
            }

            if (shakeTime > 0)
            {
                var shakeAmount = maxShakeAngle * (float)Math.Sin(
                    shakeTime * MathHelper.Pi / totalShakeTime);
                shakeTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (shakeAmount > MathHelper.ToRadians(160))
                {
                    GameServer.Singleton.Destroy(this);
                }

                Rotation = treeRotation * Quaternion.CreateFromAxisAngle(rotationAxis, shakeAmount);
            }

            //BaseGame game = BaseGame.Singleton;
            //Point p = game.Project(Position);
            //game.Graphics2D.DrawString(harvesterCount.ToString() + ", " + lumber,
            //    15, new Vector2(p.X, p.Y), Color.Blue);

            base.Update(gameTime);
        }
    }

    public class Goldmine : GameObject
    {
        /// <summary>
        /// Gets or sets how many peons are harvesting this tree
        /// </summary>
        public int HarvesterCount
        {
            get => harvesterCount;

            set
            {
                System.Diagnostics.Debug.Assert(value >= 0);

                harvesterCount = value;
            }
        }

        private int harvesterCount;

        /// <summary>
        /// Gets or sets how much gold the goldmine have
        /// </summary>
        public int Gold
        {
            get => gold;

            set
            {
                if (value <= 0)
                {
                    value = 0;

                    // Collapse
                    GameServer.Singleton.Destroy(this);
                }

                gold = value;
            }
        }

        private int gold = 10000;
        private Vector2 obstructorSize;
        private readonly List<Point> pathGrids = new();

        public float RotationZ;

        /// <summary>
        /// Gets or sets the spawn point for the goldmine
        /// </summary>
        public Vector3 SpawnPoint;

        public Goldmine(GameWorld world) : base(world, "Goldmine")
        {
            Spotted = true;
            SelectionAreaRadius = 30;
        }

        public override void Deserialize(XmlElement xml)
        {
            string value;

            // Read in rotation
            if ((value = xml.GetAttribute("Rotation")) != "")
            {
                RotationZ = MathHelper.ToRadians(float.Parse(value));
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, RotationZ);

                // Don't forget to remove the Rotation attribute from the xml,
                // otherwise our base will parse it as a Quaternion.
                xml.RemoveAttribute("Rotation");
            }

            // Read in obstructor & spawn point
            if ((_ = xml.GetAttribute("ObstructorSize")) != "")
            {
                obstructorSize = Helper.StringToVector2(xml.GetAttribute("ObstructorSize")) / 2;
            }

            if ((_ = xml.GetAttribute("SpawnPoint")) != "")
            {
                SpawnPoint = new Vector3(Helper.StringToVector2(xml.GetAttribute("SpawnPoint")), 0);
            }

            base.Deserialize(xml);
        }

        public override void Serialize(XmlElement xml)
        {
            base.Serialize(xml);

            // Overwrite rotation
            xml.SetAttribute("Rotation", MathHelper.ToDegrees(RotationZ).ToString());
        }

        private int minimapIcon = -1;

        public override void OnCreate()
        {
            SpawnPoint = new Vector3(Math2D.LocalToWorld(
                         new Vector2(SpawnPoint.X, SpawnPoint.Y),
                             Vector2.Zero, RotationZ), 0);

            pathGrids.AddRange(World.PathManager.EnumerateGridsInOutline(Outline));
            World.PathManager.Mark(pathGrids);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Start(GameWorld world)
        {
            minimapIcon = GameUI.Singleton.Minimap.AddGoldmine(Position);
        }

        public override void OnDestroy()
        {
            World.PathManager.Unmark(pathGrids);

            if (minimapIcon >= 0)
            {
                GameUI.Singleton.Minimap.RemoveGoldmine(minimapIcon);
            }

            base.OnDestroy();
        }

        protected override void UpdateOutline(Outline outline)
        {
            Vector2 position;
            position.X = Position.X;
            position.Y = Position.Y;

            outline.SetRectangle(-obstructorSize, obstructorSize, position, RotationZ);
        }
    }

    public class BoxOfPandora : GameObject
    {
        public BoxOfPandora(GameWorld world)
            : base(world, "BoxOfPandora")
        {
            VisibleInFogOfWar = false;
        }

        public override void OnCreate()
        {
            // Make sure the position is valid
            Vector2 position;

            position.X = Position.X;
            position.Y = Position.Y;
            position = World.PathManager.FindValidPosition(position, null);

            Position = new Vector3(position, 0);
            Fall();

            Update(new GameTime());

            base.OnCreate();
        }

        public override void Update(GameTime gameTime)
        {
            // Checks if anyone hits me
            foreach (IWorldObject wo in World.GetNearbyObjects(Position, 20))
            {
                if (wo is Charactor o && o.Owner != null &&
                    o.Outline.DistanceTo(Outline.Position) < 5)
                {
                    // Open the box of pandora
                    if (Helper.Random.Next(4) == 0)
                    {
                        // Sudden death
                        o.Health -= 200;
                        if (o.Visible && o.Owner is LocalPlayer)
                        {
                            GameUI.Singleton.ShowMessage("-200",
                                TopCenter, MessageType.None, MessageStyle.BubbleUp, Color.Red);
                            Audios.Play("Badluck");
                        }
                    }
                    else if (Helper.Random.Next(2) == 0)
                    {
                        o.Owner.Lumber += 100;
                        if (o.Visible && o.Owner is LocalPlayer)
                        {
                            GameUI.Singleton.ShowMessage("+100",
                                TopCenter, MessageType.None, MessageStyle.BubbleUp, Color.Green);
                            Audios.Play("Treasure");
                        }
                    }
                    else
                    {
                        o.Owner.Gold += 100;
                        if (o.Visible && o.Owner is LocalPlayer)
                        {
                            GameUI.Singleton.ShowMessage("+100",
                                TopCenter, MessageType.None, MessageStyle.BubbleUp, Color.Gold);
                            Audios.Play("Treasure");
                        }
                    }

                    GameServer.Singleton.Destroy(this);
                    return;
                }
            }

            base.Update(gameTime);
        }
    }

    public class RuinedLand
    {
        private const int TextureSize = 128;
        private readonly float width;
        private readonly float height;
        private readonly FogMask fogOfWar;
        private readonly GameWorld world;
        private readonly Texture2D ruinedTexture;
        private Texture2D finalTexture;
        private Texture2D currentTexture;
        private readonly Texture2D glowTexture;
        private readonly RenderTarget2D currentCanvas;
        private readonly RenderTarget2D finalCanvas;
        private readonly DepthStencilBuffer depthBuffer;
        private BaseLandscape.Layer layer;
        private readonly GraphicsDevice graphics;
        private readonly Graphics2D graphics2D;
        private readonly SpriteBatch sprite;
        private Rectangle textureRectangle;
        private readonly VertexPositionTexture[] vertices;
        private readonly VertexDeclaration declaration;

        private struct Region
        {
            public float Radius;
            public Vector2 Position;
        }

        private readonly List<Region> ruinedAreas = new();

        /// <summary>
        /// Overlapped points
        /// </summary>
        private readonly bool[] overlapped;

        /// <summary>
        /// Gets the whether the specified point is in the fog of war
        /// </summary>
        public bool Contains(float x, float y)
        {
            return x > 0 && y > 0 && x < width && y < height
&& overlapped[TextureSize * (int)(TextureSize * y / height) + (int)(TextureSize * x / width)];
        }

        /// <summary>
        /// There's only one ruinedland right now
        /// </summary>
        public static RuinedLand Create(BaseGame game, GameWorld world, FogMask fogOfWar)
        {
            if (land == null)
            {
                land = new RuinedLand(game, world, fogOfWar);
            }
            else if (world != land.world || fogOfWar != land.fogOfWar)
            {
                throw new ArgumentException();
            }

            return land;
        }

        public static RuinedLand Singleton => land;

        private static RuinedLand land;

        /// <summary>
        /// Creates a new ruined land
        /// </summary>
        private RuinedLand(BaseGame game, GameWorld world, FogMask fogOfWar)
        {
            if (game == null || world == null || fogOfWar == null)
            {
                throw new ArgumentNullException();
            }

            // Store the values
            this.fogOfWar = fogOfWar;
            this.world = world;

            // Initialization
            graphics2D = game.Graphics2D;
            graphics = game.GraphicsDevice;
            width = world.Landscape.Size.X;
            height = world.Landscape.Size.Y;
            overlapped = new bool[TextureSize * TextureSize];
            sprite = new SpriteBatch(graphics);
            textureRectangle = new Rectangle(0, 0, TextureSize, TextureSize);

            // Create textures & render targets
            ruinedTexture = game.ZipContent.Load<Texture2D>("Landscapes/Ruined");
            glowTexture = game.ZipContent.Load<Texture2D>("Textures/Glow");
            currentCanvas = new RenderTarget2D(game.GraphicsDevice, TextureSize, TextureSize, 0, SurfaceFormat.Color);
            finalCanvas = new RenderTarget2D(game.GraphicsDevice, TextureSize, TextureSize, 0, SurfaceFormat.Color);
            depthBuffer = new DepthStencilBuffer(game.GraphicsDevice, TextureSize, TextureSize,
                                                 game.GraphicsDevice.DepthStencilBuffer.Format);

            declaration = new VertexDeclaration(game.GraphicsDevice, VertexPositionTexture.VertexElements);
            vertices = new VertexPositionTexture[4]
            {
                new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, 1)),
                new VertexPositionTexture(new Vector3(-1, 1, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(1, 1, 0), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(1, -1, 0), new Vector2(1, 1)),
            };

            // Refresh
            Refresh();

            // Draw the current texture on to the final texture
            Draw(new GameTime());
        }

        /// <summary>
        /// Adds a new ruined region
        /// </summary>
        public void AddRuinedRegion(float radius, float x, float y)
        {
            Region entry;

            entry.Radius = radius;
            entry.Position.X = x;
            entry.Position.Y = y;

            ruinedAreas.Add(entry);
        }

        public void Refresh()
        {
            if (layer == null || !world.Landscape.Layers.Contains(layer))
            {
                // Create a new layer on the landscape
                layer = new BaseLandscape.Layer
                {
                    ColorTexture = ruinedTexture,
                    AlphaTexture = finalTexture
                };
                world.Landscape.Layers.Add(layer);
            }

            foreach (Player player in Player.AllPlayers)
            {
                if (player.Race == Race.Steamer)
                {
                    foreach (GameObject o in player.EnumerateObjects())
                    {
                        if (o is Building)
                        {
                            AddRuinedRegion(o.ViewDistance * 0.8f, o.Position.X,
                                                                   o.Position.Y);
                        }
                    }
                }
            }

            UpdateOverlapped();
            RefreshTexture();
        }

        /// <summary>
        /// Refresh the ruined region. All regions added previously are removed.
        /// </summary>
        public void RefreshTexture()
        {
            DepthStencilBuffer prevDepth = graphics.DepthStencilBuffer;

            graphics.DepthStencilBuffer = depthBuffer;

            // Draw all ruined regions
            graphics.SetRenderTarget(0, currentCanvas);
            graphics.Clear(Color.Black);

            // Draw glows
            sprite.Begin(SpriteBlendMode.Additive);

            Rectangle destination;
            foreach (Region entry in ruinedAreas)
            {
                destination.X = (int)(TextureSize * (entry.Position.X - entry.Radius) / width);
                destination.Y = (int)(TextureSize * (entry.Position.Y - entry.Radius) / height);
                destination.Width = (int)(TextureSize * entry.Radius * 2 / width);
                destination.Height = (int)(TextureSize * entry.Radius * 2 / height);

                // Draw the glow texture
                sprite.Draw(glowTexture, destination, Color.White);
            }

            sprite.End();

            // Draw discovered area texture without clearing it
            graphics.SetRenderTarget(0, null);
            graphics.DepthStencilBuffer = prevDepth;

            // Retrieve new alpha texture
            currentTexture = currentCanvas.GetTexture();

            // Clear visible areas
            ruinedAreas.Clear();
        }

        /// <summary>
        /// Draw the landscape
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            if (fogOfWar == null || fogOfWar.Current == null)
            {
                return;
            }

            DepthStencilBuffer prevDepth = graphics.DepthStencilBuffer;

            graphics.DepthStencilBuffer = depthBuffer;

            // Draw all ruined regions
            graphics.SetRenderTarget(0, finalCanvas);
            graphics.Clear(Color.Black);

            if (finalTexture != null)
            {
                sprite.Begin(SpriteBlendMode.None);
                sprite.Draw(finalTexture, textureRectangle, Color.White);
                sprite.End();
            }

            graphics.RenderState.AlphaBlendEnable = true;

            // Draw final textures
            Effect effect = graphics2D.Effect;

            effect.Parameters["BasicTexture"].SetValue(currentTexture);
            effect.Parameters["OverlayTexture"].SetValue(fogOfWar.Current);
            effect.CurrentTechnique = effect.Techniques["RuinedLand"];
            effect.Begin();
            effect.CurrentTechnique.Passes[0].Begin();

            graphics.VertexDeclaration = declaration;
            graphics.DrawUserPrimitives(PrimitiveType.TriangleFan,
                                                                         vertices, 0, 2);

            effect.CurrentTechnique.Passes[0].End();
            effect.End();

            graphics.RenderState.AlphaBlendEnable = false;

            graphics.SetRenderTarget(0, null);
            graphics.DepthStencilBuffer = prevDepth;

            // Retrieve new alpha texture
            finalTexture = finalCanvas.GetTexture();
            layer.AlphaTexture = finalTexture;
        }

        private void UpdateOverlapped()
        {
            for (var i = 0; i < overlapped.Length; i++)
            {
                overlapped[i] = false;
            }

            var CellWidth = width / TextureSize;
            var CellHeight = height / TextureSize;

            foreach (Region entry in ruinedAreas)
            {
                var minX = (int)(TextureSize * (entry.Position.X - entry.Radius) / width);
                var minY = (int)(TextureSize * (entry.Position.Y - entry.Radius) / height);
                var maxX = (int)(TextureSize * (entry.Position.X + entry.Radius) / width) + 1;
                var maxY = (int)(TextureSize * (entry.Position.Y + entry.Radius) / height) + 1;

                if (minX < 0)
                {
                    minX = 0;
                }

                if (minY < 0)
                {
                    minY = 0;
                }

                if (maxX >= TextureSize)
                {
                    maxX = TextureSize - 1;
                }

                if (maxY >= TextureSize)
                {
                    maxY = TextureSize - 1;
                }

                for (var y = minY; y <= maxY; y++)
                {
                    for (var x = minX; x <= maxX; x++)
                    {
                        Vector2 v;

                        v.X = x * CellWidth + CellWidth / 2 - entry.Position.X;
                        v.Y = y * CellHeight + CellHeight / 2 - entry.Position.Y;

                        if (v.LengthSquared() <= entry.Radius * entry.Radius)
                        {
                            overlapped[y * TextureSize + x] = true;
                        }
                    }
                }
            }
        }
    }

    public interface IProjectile
    {
        event EventHandler Hit;

        IWorldObject Target { get; }
    }

    public class Missile : BaseEntity, IProjectile
    {
        public event EventHandler Hit;

        public IWorldObject Target => target;

        private readonly IWorldObject target;
        private readonly GameModel ammo;
        private Vector3 velocity;
        private readonly float scaling;

        public float MaxSpeed = 100;
        public float MaxForce = 500;
        public float Mass = 0.5f;

        /// <summary>
        /// Creates a new missile
        /// </summary>
        public Missile(GameWorld world, GameModel ammo, IWorldObject target)
            : base(world)
        {
            if (ammo == null || target == null)
            {
                throw new ArgumentNullException();
            }

            this.ammo = ammo.ShadowCopy();
            this.target = target;

            // Compute position
            Position = ammo.Transform.Translation;

            // Compute scaling
            Matrix mx = ammo.Transform;
            mx.Translation = Vector3.Zero;
            var unitY = Vector3.Transform(Vector3.UnitY, mx);
            scaling = unitY.Length();

            // Compute speed
            //velocity = Vector3.Normalize(unitY);
            //velocity *= MaxSpeed;
            velocity = GetTargetPosition() - Position;
            velocity.Z = 0;
            velocity.Z = (float)(velocity.Length() * Math.Tan(MathHelper.ToRadians(30)));
            velocity.Normalize();
            velocity *= MaxSpeed;
        }

        public override void Update(GameTime gameTime)
        {
            Vector3 destination = GetTargetPosition();

            // Creates a force that steer the emitter towards the target position
            Vector3 desiredVelocity = destination - Position;

            desiredVelocity.Normalize();
            desiredVelocity *= MaxSpeed;

            Vector3 force = desiredVelocity - velocity;

            if (force.Length() > MaxForce)
            {
                force.Normalize();
                force *= MaxForce;
            }

            // Update velocity & position
            var elapsedSecond = (float)gameTime.ElapsedGameTime.TotalSeconds;

            velocity += elapsedSecond / Mass * force;
            Position += elapsedSecond * velocity;

            // Hit test
            Vector2 toTarget, facing;

            toTarget.X = destination.X - Position.X;
            toTarget.Y = destination.Y - Position.Y;

            facing.X = velocity.X;
            facing.Y = velocity.Y;

            if (Vector2.Dot(toTarget, facing) <= 0)
            {
                Hit?.Invoke(this, null);

                GameServer.Singleton.Destroy(this);
            }

            // Update ammo transform
            var normalizedVelocity = Vector3.Normalize(velocity);
            var rotationAxis = Vector3.Cross(Vector3.UnitZ, normalizedVelocity);
            var angle = (float)Math.Acos(Vector3.Dot(normalizedVelocity, Vector3.UnitZ));

            var rotation = Matrix.CreateFromAxisAngle(rotationAxis, angle);
            var translation = Matrix.CreateTranslation(Position);

            if (ammo != null)
            {
                ammo.Transform = Matrix.CreateScale(scaling) * rotation * translation;
            }
        }

        private Vector3 GetTargetPosition()
        {
            Vector3 destination;

            destination.X = target.Position.X;
            destination.Y = target.Position.Y;
            destination.Z = (target.BoundingBox.Max.Z + target.Position.Z) / 2;
            return destination;
        }

        public override void Draw(GameTime gameTime)
        {
            // FIXME: Consider fog of war...
            if (ammo != null)
            {
                ammo.Draw(gameTime);
            }
        }
    }

    public class Decoration : Entity
    {
        public Decoration(GameWorld world)
            : base(world) { }

        public Decoration(GameWorld world, string modelFile)
            : base(world, new GameModel(modelFile)) { }

        public override bool IsInteractive => false;

        public override void Deserialize(XmlElement xml)
        {
            base.Deserialize(xml);

            string value;

            if ((value = xml.GetAttribute("Position")) != "")
            {
                Position = Helper.StringToVector3(value);
            }
        }
    }

}

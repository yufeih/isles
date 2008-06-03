//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.UI;
using Isles.Engine;
using Isles.Graphics;

namespace Isles
{
    #region Icon
    /// <summary>
    /// A rectangle on a texture.
    /// Currently all icons are placed in the same texture for simplicity.
    /// </summary>
    public struct Icon
    {
        /// <summary>
        /// Gets or sets the rectangle region on the texture
        /// </summary>
        public Rectangle Region;

        /// <summary>
        /// Gets or sets the icon texture
        /// </summary>
        public Texture2D Texture;

        /// <summary>
        /// For easy creation of icons
        /// </summary>
        public Icon(Texture2D texture)
        {
            Texture = texture;
            Region = new Rectangle(0, 0, texture.Width, texture.Height);
        }

        public Icon(Texture2D texture, Rectangle region)
        {
            Texture = texture;
            Region = region;
        }

        /// <summary>
        /// Static stuff
        /// </summary>
        public static Texture2D DefaultTexture
        {
            get
            {
                if (texture == null || texture.IsDisposed)
                    texture = BaseGame.Singleton.ZipContent.Load<Texture2D>("UI/Icons");
                return texture;
            }
        }

        static Texture2D texture;


        const int XCount = 8;
        const int YCount = 8;

        public static Icon FromTiledTexture(int n)
        {
            return FromTiledTexture(n, XCount, YCount, DefaultTexture);
        }

        public static int IndexFromRectangle(Rectangle rectangle)
        {
            return rectangle.Y * YCount / DefaultTexture.Height * XCount +
                   rectangle.X * XCount / DefaultTexture.Width;
        }

        public static Rectangle RectangeFromIndex(int n)
        {
            int x = n % XCount;
            int y = n / XCount;
            int w = DefaultTexture.Width / XCount;
            int h = DefaultTexture.Height / YCount;
            return new Rectangle(x * w, y * h, w, h);
        }

        public static Icon FromTiledTexture(
            int n, int xCount, int yCount, Texture2D texture)
        {
            int x = n % xCount;
            int y = n / xCount;
            int w = texture.Width / xCount;
            int h = texture.Height / yCount;
            return new Icon(texture, new Rectangle(x * w, y * h, w, h));
        }
    }
    #endregion

    #region Spell
    /// <summary>
    /// Base class for all game spells.
    /// A new spell instance is created whenever a spell is been casted
    /// </summary>
    public abstract class Spell : IEventListener
    {
        #region Static Stuff
        /// <summary>
        /// Delegation used to create a spell
        /// </summary>
        public delegate Spell Creator(GameWorld world);

        /// <summary>
        /// Spell creators
        /// </summary>
        static Dictionary<string, Creator> creators = new Dictionary<string, Creator>();

        /// <summary>
        /// Register a new spell
        /// </summary>
        /// <param name="spellType"></param>
        /// <param name="creator"></param>
        public static void RegisterCreator(Type spellType, Creator creator)
        {
            creators.Add(spellType.Name, creator);
        }

        /// <summary>
        /// Register a new spell creator
        /// </summary>
        public static void RegisterCreator(string spellName, Creator creator)
        {
            creators.Add(spellName, creator);
        }

        public static Spell Create(Type spellType, GameWorld world)
        {
            return Create(spellType.Name, world);
        }

        /// <summary>
        /// Create a new game spell
        /// </summary>
        /// <param name="spellTypeName"></param>
        /// <param name="world"></param>
        /// <returns></returns>
        public static Spell Create(string spellTypeName, GameWorld world)
        {
            Creator creator;

            if (!creators.TryGetValue(spellTypeName, out creator))
                throw new Exception("Failed to create spell, unknown spell type: " + spellTypeName);

            return creator(world);
        }

        /// <summary>
        /// Cast a spell
        /// </summary>
        public static void Cast(Spell spell)
        {
            if (currentSpell == null && spell.Trigger())
                currentSpell = spell;
        }

        /// <summary>
        /// Stops casting the spell
        /// </summary>
        public static void EndSpell()
        {
            currentSpell = null;
        }

        /// <summary>
        /// Gets the active spell
        /// </summary>
        public static Spell CurrentSpell
        {
            get { return currentSpell; }
        }

        static Spell currentSpell;
        #endregion

        protected GameWorld world;


        public GameObject Owner
        {
            get { return owner; }
            set { owner = value; OnOwnerChanged(); }
        }

        protected virtual void OnOwnerChanged() { }

        GameObject owner;

        public bool Enable
        {
            get { return enable; }

            set
            {
                enable = value;
                if (spellButton != null)
                    spellButton.Visible = value;
            }
        }

        bool enable = true;

        public Spell(GameWorld world)
        {
            this.world = world;
        }

        public Spell(GameWorld world, string classID)
        {
            this.world = world;

            // Initialize from xml element
            XmlElement xml;
            if (GameDefault.Singleton.WorldObjectDefaults.TryGetValue(classID, out xml))
                Deserialize(xml);
            if (GameDefault.Singleton.SpellDefaults.TryGetValue(classID, out xml))
                Deserialize(xml);
        }

        public virtual void Deserialize(XmlElement xml)
        {
            Name = xml.GetAttribute("Name");
            Description = xml.GetAttribute("Description");
            Description.Replace('\t', ' ');

            string value;

            if ((value = xml.GetAttribute("Icon")) != "")
                Icon = Icon.FromTiledTexture(int.Parse(value));

            if ((value = xml.GetAttribute("Hotkey")) != "")
                Hotkey = (Keys)Enum.Parse(typeof(Keys), value);

            if ((value = xml.GetAttribute("CoolDown")) != "")
                CoolDown = float.Parse(value);

            if ((value = xml.GetAttribute("AutoCast")) != "")
                AutoCast = bool.Parse(value);
        }

        /// <summary>
        /// Gets or sets the name of the spell
        /// </summary>
        public string Name;

        /// <summary>
        /// Gets or sets the description of the spell
        /// </summary>
        public string Description;

        /// <summary>
        /// Gets or sets the spell icon.
        /// </summary>
        public Icon Icon = Icon.FromTiledTexture(0);

        /// <summary>
        /// Gets or sets the hot key for the spell
        /// </summary>
        public Keys Hotkey;

        /// <summary>
        /// Gets whether the spell is ready for cast
        /// </summary>
        public virtual bool Ready
        {
            get { return CoolDownElapsed >= CoolDown; }
        }

        /// <summary>
        /// Gets the cool down time after the spell has been casted
        /// </summary>
        public virtual float CoolDown
        {
            get { return coolDown; }

            set
            {
                if (value < 0)
                    value = 0;

                coolDown = value;
                coolDownElapsed = coolDown;
            }
        }

        float coolDown;

        /// <summary>
        /// Gets how many seconds elapsed after the spell has been casted
        /// </summary>
        public virtual float CoolDownElapsed
        {
            get { return coolDownElapsed; }
            set { coolDownElapsed = value; }
        }

        float coolDownElapsed;

        /// <summary>
        /// Gets or sets whether auto cast is enable for this spell
        /// </summary>
        public virtual bool AutoCast
        {
            get { return autoCast; }
            set { autoCast = value; }
        }

        bool autoCast;

        /// <summary>
        /// Gets the button for this spell
        /// </summary>
        public virtual IUIElement Button
        {
            get
            {
                if (spellButton == null || spellButton.Texture == null ||
                    spellButton.Texture.IsDisposed)
                {
                    spellButton = CreateUIElement(GameUI.Singleton);
                    spellButton.Visible = Enable;
                }

                return spellButton;
            }
        }

        protected SpellButton spellButton;
        TipBox spellTip;

        /// <summary>
        /// Creates an UIElement for the spell
        /// </summary>
        protected virtual SpellButton CreateUIElement(GameUI ui)
        {
            if (ui == null)
                throw new ArgumentNullException();

            int baseIcon = Icon.IndexFromRectangle(Icon.Region);
            spellButton = new SpellButton();
            spellButton.Texture = Icon.Texture == null ? Icon.DefaultTexture : Icon.Texture;
            spellButton.SourceRectangle = Icon.Region;
            spellButton.Pressed = Icon.RectangeFromIndex(baseIcon + 1);
            spellButton.Hovered = Icon.RectangeFromIndex(baseIcon + 2);
            spellButton.HotKey = Hotkey;
            spellButton.Tag = this;

            spellButton.Click += new EventHandler(delegate(object sender, EventArgs e)
            {
                Spell spell = (sender as Button).Tag as Spell;

                if (spell == null)
                    throw new InvalidOperationException();

                // Cast the spell
                Spell.Cast(spell);
            });


            spellButton.Enter += new EventHandler(delegate(object sender, EventArgs e)
            {
                GameDefault gameDefault = GameDefault.Singleton;

                spellTip = CreateTipBox();

                GameUI.Singleton.TipBoxContainer.Add(spellTip);
            });

            spellButton.Leave += new EventHandler(delegate(object sender, EventArgs e)
            {
                if (spellTip != null)
                    GameUI.Singleton.TipBoxContainer.Remove(spellTip);
            });

            return spellButton;
        }

        protected virtual TipBox CreateTipBox()
        {
            TextField content = null;
            TextField title = new TextField(this.Name, 16f / 23, Color.Gold, 
                                             new Rectangle(6, 6, 210, 30));
            if (Description != null && Description != "")
                content = new TextField(this.Description, 15f/23, Color.White,
                                        new Rectangle(6, 30, 210, 100));
            TipBox spellTip = new TipBox(222, title.RealHeight +
                                        (content != null ? content.RealHeight : 0) + 20);
            spellTip.Add(title);
            if (content != null)
                spellTip.Add(content);
            return spellTip;
        }


        public virtual void Update(GameTime gameTime)
        {
            // Update spell cooldown
            if (coolDown > 0)
            {
                coolDownElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (coolDownElapsed > coolDown)
                    coolDownElapsed = coolDown;

                if (spellButton != null)
                    spellButton.Percentage = 100 * coolDownElapsed / coolDown;
            }
        }

        /// <summary>
        /// Called every frame when this spell have become the active spell
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void UpdateCast(GameTime gameTime) { }

        /// <summary>
        /// Draw the spell
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Draw(GameTime gameTime) { }

        /// <summary>
        /// IEventListener
        /// </summary>
        public virtual EventResult HandleEvent(EventType type, object sender, object tag)
        {
            return EventResult.Unhandled;
        }

        /// <summary>
        /// Called when the user clicked the spell icon
        /// (or pressed the spell hot key).
        /// </summary>
        /// <returns>
        /// Whether the spell wants to receive BeginCast event
        /// </returns>
        public virtual bool Trigger()
        {
            return false;
        }

        /// <summary>
        /// Perform the actual cast
        /// </summary>
        public virtual void Cast()
        {
            if (Ready)
                coolDownElapsed = 0;
        }

        /// <summary>
        /// Cast the spell to a specified position
        /// </summary>
        public virtual void Cast(Vector3 position)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Cast the spell to the target
        /// </summary>
        public virtual void Cast(Entity target)
        {
            throw new InvalidOperationException();
        }
    }
    #endregion

    #region SpellButton
    public class SpellButton : Button
    {
        #region Fields
        /// <summary>
        /// Gets or sets the number in the top left corner
        /// </summary>
        public int Count
        {
            get { return count; }
            set { count = value; }
        }

        int count;

        /// <summary>
        /// Gets or sets the percentage drawed over the button
        /// </summary>
        public float Percentage
        {
            get { return percentage; }
            set { percentage = value; }
        }

        float percentage = 0;

        public bool ShowCount = true;
        public bool ShowPercentage = true;

        bool autoCast = false;

        /// <summary>
        /// Gets or sets whether the spell is autoReleasable
        /// </summary>
        public bool AutoCast
        {
            get { return autoCast; }
            set { autoCast = value; }
        }
        #endregion

        #region Draw Fade
        /// <summary>
        /// The fade mask is divided into 5 parts to draw
        /// </summary>
        /// <param name="sprite"></param>
        void DrawFade()
        {
            if (percentage <= 0)
                return;

            double baseAngle = Math.Atan2(DestinationRectangle.Width, DestinationRectangle.Height);

            Color color = new Color(0, 0, 0, 85);
            VertexPositionColor[] vertices = new VertexPositionColor[3]
            {
                new VertexPositionColor(new Vector3
                            (DestinationRectangle.Left + DestinationRectangle.Width / 2, 
                            DestinationRectangle.Top + DestinationRectangle.Height/2, 0), color),
                new VertexPositionColor(new Vector3
                            (DestinationRectangle.Left, DestinationRectangle.Top, 0), color),
                new VertexPositionColor(new Vector3
                            (DestinationRectangle.Left + DestinationRectangle.Width / 2,
                            DestinationRectangle.Top, 0), color),
            };

            ushort[] indices = new ushort[3] { 0, 1, 2 };
            double angle = (100 - percentage) / 100 * Math.PI * 2;

            // Part one: 0 ~ baseAngle
            if (angle > baseAngle)
            {
                Graphics2D.DrawPrimitive(vertices, indices);
            }
            else
            {
                vertices[1].Position.X = DestinationRectangle.Left + DestinationRectangle.Width / 2 -
                            (float)(DestinationRectangle.Height / 2 * Math.Tan(angle));
                Graphics2D.DrawPrimitive(vertices, indices);
                return;
            }

            // Part two: baseAngle ~ PI - baseAngle
            vertices[1].Position.X = DestinationRectangle.Left;
            vertices[1].Position.Y = DestinationRectangle.Bottom;
            vertices[2].Position.X = DestinationRectangle.Left;
            vertices[2].Position.Y = DestinationRectangle.Top;
            if (angle > Math.PI - baseAngle)
            {
                Graphics2D.DrawPrimitive(vertices, indices);
            }
            else
            {
                vertices[1].Position.Y = DestinationRectangle.Top + DestinationRectangle.Height / 2 +
                            (float)(Math.Tan(angle - Math.PI / 2) * DestinationRectangle.Width / 2);
                Graphics2D.DrawPrimitive(vertices, indices);
                return;
            }

            // Part three: PI - baseAngle ~ PI + baseAngle
            vertices[1].Position.X = DestinationRectangle.Right;
            vertices[1].Position.Y = DestinationRectangle.Bottom;
            vertices[2].Position.X = DestinationRectangle.Left;
            vertices[2].Position.Y = DestinationRectangle.Bottom;
            if (angle > Math.PI + baseAngle)
            {
                Graphics2D.DrawPrimitive(vertices, indices);
            }
            else
            {
                vertices[1].Position.X = DestinationRectangle.Left + DestinationRectangle.Width / 2 +
                            (float)(Math.Tan(angle) * DestinationRectangle.Height / 2);
                Graphics2D.DrawPrimitive(vertices, indices);
                return;
            }

            // Part four: PI + baseAngle ~ 2*PI - baseAngle
            vertices[1].Position.X = DestinationRectangle.Right;
            vertices[1].Position.Y = DestinationRectangle.Top;
            vertices[2].Position.X = DestinationRectangle.Right;
            vertices[2].Position.Y = DestinationRectangle.Bottom;
            if (angle > 2 * Math.PI - baseAngle)
            {
                Graphics2D.DrawPrimitive(vertices, indices);
            }
            else
            {
                vertices[1].Position.Y = DestinationRectangle.Top + DestinationRectangle.Height / 2 -
                            (float)(Math.Tan(angle - 3 * Math.PI / 2) * DestinationRectangle.Width / 2);
                Graphics2D.DrawPrimitive(vertices, indices);
                return;
            }

            // Part five: 2*PI - baseAngle ~ 2*PI
            vertices[1].Position.X = DestinationRectangle.Left + DestinationRectangle.Width / 2;
            vertices[1].Position.Y = DestinationRectangle.Top;
            vertices[2].Position.X = DestinationRectangle.Right;
            vertices[2].Position.Y = DestinationRectangle.Top;
            if (angle >= 2 * Math.PI)
            {
                Graphics2D.DrawPrimitive(vertices, indices);
            }
            else
            {
                vertices[1].Position.X = DestinationRectangle.Left + DestinationRectangle.Width / 2 +
                            (float)(Math.Tan(Math.PI * 2 - angle) * DestinationRectangle.Height / 2);
                Graphics2D.DrawPrimitive(vertices, indices);
                return;
            }
        }
        #endregion

        /// <summary>
        /// Draw
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="sprite"></param>
        public override void Draw(GameTime gameTime, SpriteBatch sprite)
        {
            base.Draw(gameTime, sprite);

            if (ShowPercentage)
                DrawFade();

            if (count > 1 && ShowCount)
            {
                Graphics2D.DrawShadowedString(  count.ToString(), 15f / 23,
                                                new Vector2(DestinationRectangle.X + DestinationRectangle.Width / 15, 
                                                            DestinationRectangle.Y + DestinationRectangle.Height / 30), 
                                                Color.White, Color.Black);
            }
        }
    }
    #endregion

    #region SpellTraining
    public class SpellTraining : Spell
    {
        /// <summary>
        /// Gets the type of unit that's going to be trained
        /// </summary>
        public string Type
        {
            get { return type; }
        }

        string type;

        Building ownerBuilding;
        public bool ShowCount = true;
        public delegate void UpgradeEventHandler(Spell spell, Building owner);
        public event UpgradeEventHandler Complete;

        protected override void OnOwnerChanged()
        {
            ownerBuilding = Owner as Building;

            if (ownerBuilding == null)
                throw new ArgumentException();
        }

        /// <summary>
        /// Gets or sets the number of units that going to be trained
        /// </summary>
        public int Count
        {
            get { return spellButton != null ? spellButton.Count : 0; }
            set { if (spellButton != null) spellButton.Count = value; }
        }

        public SpellTraining(GameWorld world, string type, Building owner)
            : base(world)
        {
            if (type == null)
                throw new ArgumentNullException();

            this.type = type;
            this.ownerBuilding = owner;
        }

        public override void Deserialize(XmlElement xml)
        {
            if (xml.HasAttribute("TrainingTime"))
                CoolDown = float.Parse(xml.GetAttribute("TrainingTime"));

            base.Deserialize(xml);
        }

        protected override SpellButton CreateUIElement(GameUI ui)
        {
            SpellButton button = base.CreateUIElement(ui);

            // Add right click event handler
            button.RightClick += new EventHandler(delegate(object sender, EventArgs e)
            {
                if (ownerBuilding != null)
                {
                    ownerBuilding.CancelTraining(this);
                    Audios.Play("OK");
                }
            });

            return button;
        }

        public override bool Trigger()
        {
            if (Player.LocalPlayer != null &&
                Player.LocalPlayer.CurrentGroup != null &&
                Player.LocalPlayer.CurrentGroup.Count > 0 &&
                Player.LocalPlayer.CurrentGroup[0] is Building)
            {
                // Find a building with the minimum number of pending request
                int min = int.MaxValue;
                Building building = null;

                foreach (GameObject o in Player.LocalPlayer.CurrentGroup)
                {
                    Building b = o as Building;

                    if (b != null && b.QueuedSpells.Count < min)
                    {
                        min = b.QueuedSpells.Count;
                        building = b;
                    }
                }

                if (building != null && building.TrainUnit(type))
                {
                    Audios.Play("OK");

                    if (Player.LocalPlayer.CurrentGroup.Count > 1 && !building.CanTrain(type))
                        Enable = false;
                }
                else Audios.Play("Invalid");
            }

            return false;
        }

        protected override TipBox CreateTipBox()
        {
            GameDefault def = GameDefault.Singleton;
            int height = 6;
            TextField gold = null, lumber = null, food = null;
            TextField title = new TextField(this.Name + "  [" + this.Hotkey.ToString() + "]",
                                            16f/23, Color.Gold, new Rectangle(6, 6, 210, 30));
            height += 30;
            TextField content = new TextField(this.Description, 15f/23, Color.White,
                                                new Rectangle(6, 30, 210, 100));
            height += content.RealHeight + 10;
            if (def.GetGold(Type) != 0)
            {
                gold = new TextField("Gold:         " + def.GetGold(Type).ToString(),
                                            15f / 23, Color.Gold,
                                            new Rectangle(6, height, 210, 29));
                height += gold.RealHeight;
            }
            if (def.GetLumber(Type) != 0)
            {
                lumber = new TextField("Lumber:      " + def.GetLumber(Type).ToString(),
                                            15f / 23, Color.Green,
                                            new Rectangle(6, height, 210, 29));
                height += lumber.RealHeight;
            }
            if (def.GetFood(Type) != 0)
            {
                food = new TextField("Food:         " + def.GetFood(Type).ToString(),
                                            15f / 23, Color.Magenta, 
                                            new Rectangle(6, height, 210, 19));
                height += food.RealHeight;
            }
            TipBox spellTip = new TipBox(220, height + 10);
            spellTip.Add(title);
            spellTip.Add(content);
            if(gold != null)
                spellTip.Add(gold);
            if(lumber != null)
                spellTip.Add(lumber);
            if(food != null)
                spellTip.Add(food);
            return spellTip;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Show count...
            if (ownerBuilding != null && ownerBuilding.Owner is LocalPlayer)
            {
                bool multipleBuildings = false;
                if (ShowCount)
                {
                    if (Player.LocalPlayer != null &&
                        Player.LocalPlayer.CurrentGroup != null &&
                        Player.LocalPlayer.CurrentGroup.Count > 1 &&
                        Player.LocalPlayer.CurrentGroup[0] is Building)
                    {
                        if (spellButton != null)
                        {
                            spellButton.ShowCount = false;
                            spellButton.ShowPercentage = false;
                            multipleBuildings = true;
                        }
                    }
                    else
                    {
                        if (spellButton != null)
                        {
                            spellButton.ShowCount = true;
                            spellButton.ShowPercentage = true;
                        }
                    }
                }
                else spellButton.ShowCount = false;

                // Update enable states
                if (ownerBuilding != null)
                {
                    if (multipleBuildings)
                        Enable = ownerBuilding.CanTrain(type);
                    else
                        Enable = ownerBuilding.CanTrain(type) || !Ready ||
                                 ownerBuilding.QueuedSpells.Contains(this);
                }
            }

            // Unit trained
            if (Ready && ownerBuilding.QueuedSpells.Count > 0 &&
                         ownerBuilding.QueuedSpells.Peek() == this)
            {
                ownerBuilding.QueuedSpells.Dequeue();
                Count--;

                // Start next spell?
                if (ownerBuilding.QueuedSpells.Count > 0)
                    if (ownerBuilding.QueuedSpells.Peek() is SpellTraining)
                        (ownerBuilding.QueuedSpells.Peek() as SpellTraining).CoolDownElapsed = 0;

                if (Complete != null)
                    Complete(this, ownerBuilding);

                OnComplete();
            }
        }

        protected virtual void OnComplete()
        {
            Charactor c = world.Create(type) as Charactor;

            if (c != null)
            {
                if (ownerBuilding.Owner != null)
                    ownerBuilding.Owner.UnmarkFutureObject(type);

                c.Position = ownerBuilding.Position + ownerBuilding.SpawnPoint;
                c.Owner = ownerBuilding.Owner;
                c.Owner.Food -= c.Food;
                c.Fall();
                world.Add(c);

                if (c.IsHero && spellButton != null)
                    spellButton.Visible = false;

                foreach (object o in ownerBuilding.RallyPoints)
                {
                    if (o is Entity)
                        c.PerformAction(o as Entity, true);
                    else if (o is Vector3)
                        c.PerformAction((Vector3)o, true);
                }
            }
        }
    }
    #endregion

    #region SpellUpgrades
    public class SpellUpgrade : SpellTraining
    {
        public SpellUpgrade(GameWorld world, string type)
            : base(world, type, null)
        {
            GameDefault.Singleton.SetUnique(Type);
        }

        public SpellUpgrade(GameWorld world, string type, UpgradeEventHandler onComplete)
            : base(world, type, null)
        {
            GameDefault.Singleton.SetUnique(Type);
            Complete += onComplete;
        }

        protected override void OnComplete()
        {
            if (Owner.Owner is LocalPlayer)
                Audios.Play("UpgradeComplete");
        }
    }

    /// <summary>
    /// Handler for all upgrades
    /// </summary>
    public static class Upgrades
    {
        public static void LiveOfNature(Spell spell, Building owner)
        {
            if (owner != null && owner.Owner != null)
            {
                foreach (GameObject o in owner.Owner.EnumerateObjects("Lumbermill"))
                {
                    if (o is Lumbermill)
                        (o as Lumbermill).LiveOfNatureResearched();
                }

                owner.Owner.MarkAvailable("LiveOfNature");
            }
        }

        public static void PunishOfNature(Spell spell, Building owner)
        {
            if (owner != null && owner.Owner != null)
            {
                owner.Owner.MarkAvailable("PunishOfNatureUpgrade");

                foreach (GameObject o in owner.Owner.EnumerateObjects("FireSorceress"))
                {
                    o.AddSpell("PunishOfNature");
                }
            }
        }

        public static void Attack(Spell spell, Building owner)
        {
            if (owner != null && owner.Owner != null)
            {
                owner.Owner.AttackPoint = 20;
                owner.Owner.MarkAvailable("AttackUpgrade");

                foreach (GameObject o in owner.Owner.EnumerateObjects())
                {
                    if (o.AttackPoint.X > 0)
                    {
                        o.AttackPoint.X += 20;
                        o.AttackPoint.Y += 20;
                    }
                }
            }
        }

        public static void Defense(Spell spell, Building owner)
        {
            if (owner != null && owner.Owner != null)
            {
                owner.Owner.DefensePoint = 20;
                owner.Owner.MarkAvailable("DefenseUpgrade");

                foreach (GameObject o in owner.Owner.EnumerateObjects())
                {
                    if (o.DefensePoint.X > 0)
                    {
                        o.DefensePoint.X += 20;
                        o.DefensePoint.Y += 20;
                    }
                }
            }
        }
    }
    #endregion

    #region SpellConstruct
    /// <summary>
    /// An interface for the construct spell to interact with other entities.
    /// A placeable must also be a derived class of Entity
    /// </summary>
    public interface IPlaceable
    {
        /// <summary>
        /// Called before the user chooses where to place the object
        /// </summary>
        bool BeginPlace();

        /// <summary>
        /// Gets whether the current position if placable
        /// </summary>
        bool IsLocationPlacable();

        /// <summary>
        /// Place the entity
        /// </summary>
        void Place();

        /// <summary>
        /// Called when the construct request was canceled
        /// </summary>
        void CancelPlace();
    }

    /// <summary>
    /// A spell encapsulating the behavior of constructing a building
    /// </summary>
    public class SpellConstruct : Spell
    {
        public bool AutoReactivate = false;

        int step = 0;
        string entityType;
        Input input;
        Entity entity;
        BaseEntity baseEntity;
        IPlaceable placeable;

        public SpellConstruct(GameWorld world, string entityType)
            : base(world)
        {
            this.input = BaseGame.Singleton.Input;
            this.entityType = entityType;
        }

        public SpellConstruct(GameWorld world, BaseEntity baseEntity)
            : base(world)
        {
            if (null == baseEntity || null == world)
                throw new ArgumentNullException();

            this.input = BaseGame.Singleton.Input;
            this.baseEntity = baseEntity;
            this.entityType = baseEntity.ClassID;
        }

        public override bool Trigger()
        {
            hasCasted = false;
            step = 0;

            if (baseEntity == null)
                baseEntity = world.Create(entityType) as BaseEntity;

            if (baseEntity != null)
            {
                entity = baseEntity as Entity;
                placeable = baseEntity as IPlaceable;

                // Set owner if it's a building
                Building building = baseEntity as Building;
                if (Player.LocalPlayer != null && building != null)
                    building.Owner = Player.LocalPlayer;

                if (placeable != null && !placeable.BeginPlace())
                {
                    Spell.EndSpell();
                    return false;
                }
            }

            Audios.Play("OK");
            return baseEntity != null;
        }

        protected override TipBox CreateTipBox()
        {
            GameDefault def = GameDefault.Singleton;
            int height = 6;
            TextField title = new TextField(this.Name + "  [" + this.Hotkey.ToString() + "]",
                                            16f / 23, Color.Gold, new Rectangle(6, 6, 210, 30));
            height += 30;
            TextField content = new TextField(this.Description, 15f/23, Color.White,
                                                new Rectangle(6, 30, 210, 100));
            height += content.RealHeight + 10;
            TextField gold = null, lumber = null;
            if (def.GetGold(entityType) != 0)
            {
                gold = new TextField("Gold:         " + def.GetGold(entityType).ToString(),
                                            15f / 23, Color.Gold, new Rectangle(6, height, 210, 29));
                height += gold.RealHeight;
            }
            if (def.GetLumber(entityType) != 0)
            {
                lumber = new TextField("Lumber:      " + def.GetLumber(entityType).ToString(),
                                            15f / 23, Color.Green, new Rectangle(6, height, 210, 29));
                height += lumber.RealHeight;
            }
            TipBox spellTip = new TipBox(220, height + 10);
            spellTip.Add(title);
            spellTip.Add(content);
            if(gold != null)
                spellTip.Add(gold);
            if(lumber != null)
                spellTip.Add(lumber);
            return spellTip;
        }

        public override void UpdateCast(GameTime gameTime)
        {
            // Update entity position
            if (step == 0)
            {
                if (world.TargetPosition.HasValue)
                    baseEntity.Position = world.TargetPosition.Value;
            }
            // Adjusting entity rotation
            else if (step == 1 && input.Mouse.LeftButton == ButtonState.Pressed && entity != null)
            {
                float rotation = beginDropRotation + MathHelper.PiOver2 + (float)Math.Atan2(
                                -(double)(input.MousePosition.Y - beginDropPosition.Y),
                                 (double)(input.MousePosition.X - beginDropPosition.X));

                entity.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rotation);

                if (entity is Building)
                    (entity as Building).RotationZ = rotation;
                else if (entity is Goldmine)
                    (entity as Goldmine).RotationZ = rotation;
            }

            baseEntity.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            baseEntity.Draw(gameTime);
        }

        Point beginDropPosition;
        float beginDropRotation;

        bool hasCasted = false;
        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            // Cancel build if right clicked for Esc is pressed
            Keys key = (tag is Keys?) ? (tag as Keys?).Value : Keys.None;
            if ((type == EventType.KeyUp && key == Keys.Escape) ||
                 type == EventType.RightButtonUp || type == EventType.RightButtonDown ||
                (hasCasted && type == EventType.KeyUp && (key == Keys.LeftShift) || key == Keys.RightShift))
            {
                if (placeable != null)
                    placeable.CancelPlace();

                input.Uncapture();
                Spell.EndSpell();
                return EventResult.Handled;
            }

            // Start build
            if (type == EventType.LeftButtonDown && step == 0)
            {
                // Click on the minimap to change location
                if (GameUI.Singleton.Minimap.MapToWorld(input.MousePosition).HasValue)
                    return EventResult.Unhandled;

                // Check if the current position if placable
                if (placeable != null && !placeable.IsLocationPlacable())
                {
                    Audios.Play("CannotBuild", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Can not build there!", MessageType.Unavailable, Color.White);
                    return EventResult.Handled;
                }

                step = 1;
                input.Capture(this);

                if (entity != null)
                {
                    beginDropRotation = 0;
                    beginDropPosition = input.MousePosition;
                    beginDropPosition.Y -= 10;
                }

                return EventResult.Handled;
            }

            // End build
            if (type == EventType.LeftButtonUp && step == 1)
            {
                // Make sure we uncapture the input and end the spell
                input.Uncapture();

                if (placeable != null && !placeable.IsLocationPlacable())
                {
                    step = 0;
                    Audios.Play("CannotBuild", Audios.Channel.Interface, null);
                    GameUI.Singleton.PushMessage("Can not build there...", MessageType.Unavailable, Color.White);
                    return EventResult.Handled;
                }

                // Get builder
                if (baseEntity is Building)
                {
                    Worker builder = null;
                    Worker firstBuilder = null;

                    foreach (GameObject o in Player.LocalPlayer.CurrentGroup)
                    {
                        if (firstBuilder == null)
                            firstBuilder = o as Worker;

                        if (o is Worker && !(o.State is StateConstruct))
                        {
                            builder = o as Worker;
                            break;
                        }
                    }

                    // If no idle builder is found, use the first builder
                    if (builder == null)
                        builder = firstBuilder;

                    (baseEntity as Building).Builder = builder;
                }

                if (placeable != null)
                    placeable.Place();

                world.Add(baseEntity);

                // Reset everything
                entity = null;
                baseEntity = null;
                placeable = null;
                Spell.EndSpell();

                // Auto reactivate
                if (AutoReactivate || input.IsShiftPressed)
                {
                    Spell.Cast(this);
                    hasCasted = true;
                }

                return EventResult.Handled;
            }

            return EventResult.Unhandled;
        }
    }
    #endregion

    #region SpellAttack
    public class SpellAttack : Spell
    {
        /// <summary>
        /// Creates a new team attack
        /// </summary>
        public SpellAttack(GameWorld world)
            : base(world)
        {
            Name = "Attack";
            Hotkey = Keys.A;
            Icon = Icon.FromTiledTexture(11);
        }

        public override bool Trigger()
        {
            if (Helper.Random.Next(20) == 0)
                Audios.Play("Hawk");
            else
                Audios.Play("OK");
            world.Game.Cursor = Cursors.TargetRed;
            return true;
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (type == EventType.LeftButtonDown)
            {
                Input input = sender as Input;
                // Checks if we have clicked the minimap
                Vector3? minimapPoint = GameUI.Singleton.Minimap.MapToWorld(input.MousePosition);

                if (minimapPoint.HasValue)
                {
                    Player.LocalPlayer.AttackTo(minimapPoint.Value);
                    world.Game.Cursor = Cursors.Default;
                    Spell.EndSpell();
                    return EventResult.Handled;
                }

                // Checks if we've cliked the UI
                if (GameUI.Singleton.Overlaps(input.MousePosition))
                    return EventResult.Handled;

                // Attack entity
                GameObject picked = world.Pick() as GameObject;

                if (picked != null && world.FogOfWar != null &&
                                      world.FogOfWar.Contains(picked.Position.X, picked.Position.Y))
                {
                    picked = null;
                }                        

                if (picked != null)
                {
                    if (picked.IsAlive)
                        //Player.LocalPlayer.GetRelation(picked.Owner) == PlayerRelation.Opponent)
                    {
                        Player.LocalPlayer.AttackTo(picked);
                        world.Game.Cursor = Cursors.Default;
                        Spell.EndSpell();
                    }
                    else Audios.Play("Invalid");
                    return EventResult.Handled;
                }

                // Attack to position
                Vector3? point = world.Landscape.Pick();

                if (point.HasValue)
                {
                    Player.LocalPlayer.AttackTo(point.Value);
                    world.Game.Cursor = Cursors.Default;
                    Spell.EndSpell();

                    Vector3 location = point.Value;
                    location.Z = world.Landscape.GetHeight(location.X, location.Y);
                    GameUI.Singleton.SetCursorFocus(location, Color.Red);
                }

                return EventResult.Handled;
            }

            if (type == EventType.RightButtonDown ||
               (type == EventType.KeyDown && (tag is Keys?) && (tag as Keys?).Value == Keys.Escape))
            {
                // Cancel attack
                world.Game.Cursor = Cursors.Default;
                Spell.EndSpell();
                return EventResult.Handled;
            }

            return EventResult.Unhandled;
        }
    }
    #endregion

    #region SpellMove
    public class SpellMove : Spell
    {
        public SpellMove(GameWorld world)
            : base(world)
        {
            Name = "Move";
            Hotkey = Keys.M;
            Icon = Icon.FromTiledTexture(8);
        }

        public override bool Trigger()
        {
            Audios.Play("OK");
            world.Game.Cursor = Cursors.TargetGreen;
            return true;
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (type == EventType.LeftButtonDown)
            {
                Input input = sender as Input;
                // Checks if we have clicked the minimap
                Vector3? minimapPoint = GameUI.Singleton.Minimap.MapToWorld(input.MousePosition);

                if (minimapPoint.HasValue)
                {
                    Player.LocalPlayer.MoveTo(minimapPoint.Value);
                    world.Game.Cursor = Cursors.Default;
                    Spell.EndSpell();
                    return EventResult.Handled;
                }

                // Checks if we've cliked the UI
                if (GameUI.Singleton.Overlaps(input.MousePosition))
                    return EventResult.Handled;

                // Attack entity
                GameObject picked = world.Pick() as GameObject;

                if (picked != null && world.FogOfWar != null &&
                                      world.FogOfWar.Contains(picked.Position.X, picked.Position.Y))
                {
                    picked = null;
                }                        

                if (picked != null && picked.IsAlive)
                {
                    Player.LocalPlayer.MoveTo(picked);
                    world.Game.Cursor = Cursors.Default;
                    Spell.EndSpell();
                    return EventResult.Handled;
                }

                // Attack to position
                Vector3? point = world.Landscape.Pick();

                if (point.HasValue)
                {
                    Player.LocalPlayer.MoveTo(point.Value);
                    world.Game.Cursor = Cursors.Default;
                    Spell.EndSpell();

                    Vector3 location = point.Value;
                    location.Z = world.Landscape.GetHeight(location.X, location.Y);
                    GameUI.Singleton.SetCursorFocus(location, Color.Green);
                }

                return EventResult.Handled;
            }

            if (type == EventType.RightButtonDown ||
               (type == EventType.KeyDown && (tag is Keys?) && (tag as Keys?).Value == Keys.Escape))
            {
                // Cancel attack
                world.Game.Cursor = Cursors.Default;
                Spell.EndSpell();
                return EventResult.Handled;
            }

            return EventResult.Unhandled;
        }
    }
    #endregion

    #region SpellCombat
    /// <summary>
    /// Any spell againest units or buildings should derive from
    /// SpellCombat and use StateAttack to trigger the event.
    /// </summary>
    public class SpellCombat : Spell
    {
        /// <summary>
        /// Common stuff
        /// </summary>
        public GameObject Target;

        /// <summary>
        /// Gets or sets the minimum value of the attack range
        /// </summary>
        public float MinimumRange = 0;

        /// <summary>
        /// Gets or sets the maximum value of the attack range
        /// </summary>
        public float MaximumRange = 8;

        /// <summary>
        /// Creates a new combat spell
        /// </summary>
        public SpellCombat(GameWorld world, GameObject owner)
            : base(world)
        {
            if (owner == null)
                throw new ArgumentNullException();

            Owner = owner;
        }

        protected override void OnOwnerChanged()
        {
            if (Owner == null)
                throw new ArgumentNullException();

            this.MinimumRange = Owner.AttackRange.X;
            this.MaximumRange = Owner.AttackRange.Y;
            this.CoolDown = Owner.AttackDuration;
        }

        /// <summary>
        /// Gets whether the target is within the attack range
        /// </summary>
        public virtual bool TargetWithinRange(Entity target)
        {
            Vector2 position;
            position.X = Owner.Position.X;
            position.Y = Owner.Position.Y;

            float distance = target.Outline.DistanceTo(position);
            distance -= Owner.Outline.Radius;
            if (distance < 0)
                distance = 0;
            return distance >= MinimumRange && distance <= MaximumRange;
        }

        /// <summary>
        /// Gets whether the target can be attacked by this spell
        /// </summary>
        public virtual bool CanAttakTarget(GameObject target)
        {
            return target != null && target.IsAlive &&
                   target.Visible && !target.InFogOfWar && target != Owner;// && Owner.IsOpponent(target);
        }

        public override void Cast(Entity target)
        {
            GameObject gameObject = target as GameObject;

            if (Ready && CanAttakTarget(gameObject) && TargetWithinRange(target))
            {
                if (gameObject != null && gameObject.Owner is LocalPlayer)
                    (gameObject.Owner as LocalPlayer).AddAttacker(Owner);

                Owner.TriggerAttack(target);
                base.Cast();
            }
        }
    }
    #endregion

    #region SpellSummon
    public class SpellSummon : Spell
    {
        string type;

        public SpellSummon(GameWorld world, string type)
            : base(world, type) 
        {
            this.type = type;
        }

        public override void Update(GameTime gameTime)
        {
            Enable = true;
            if (Owner != null && Owner.Owner.IsAvailable(type))
                Enable = false;

            base.Update(gameTime);
        }

        public override bool Trigger()
        {
            Cast();
            return false;
        }

        public override void Cast()
        {
            if (Owner != null)
            {
                IWorldObject wo = world.Create(type);
                wo.Position = Owner.Position;

                if (wo is GameObject)
                    (wo as GameObject).Owner = Owner.Owner;

                if (wo is Charactor && Owner is Charactor)
                    (wo as Charactor).Facing = (Owner as Charactor).Facing;

                world.Add(wo);

                // Add some effect
                float radius = 10;

                if (wo is GameObject)
                    radius = (wo as GameObject).SelectionAreaRadius;
                
                EffectSpawn effect = new EffectSpawn(world, wo.Position, radius, "Summon");

                world.Add(effect);

                Audios.Play("Hellfire");
            }
            base.Cast();
        }
    }
    #endregion

    #region SpellDrawPathOccluder
    public class SpellDrawPathOcculder : Spell
    {
        bool? drawing;
        Input input;
        bool[,] pathOcculders;

        public float BrushRadius;

        public SpellDrawPathOcculder(GameWorld world)
            : base(world)
        {
            input = world.Game.Input;
            PathGraph graph = world.PathManager.Graph;
            pathOcculders = new bool[graph.EntryWidth, graph.EntryHeight];
        }

        public override bool Trigger()
        {
            world.Game.Cursor = Cursors.TargetNeutral;
            return true;
        }

        public override void UpdateCast(GameTime gameTime)
        {
            if (drawing.HasValue)
            {
                Vector3? location = world.Landscape.Pick();

                if (location.HasValue)
                {
                    IEnumerable<Point> points =
                        world.PathManager.EnumerateGridsInCircle(
                            new Vector2(location.Value.X, location.Value.Y), BrushRadius); 

                    PathGraph graph = world.PathManager.Graph;

                    if (drawing.Value)
                    {
                        // Add
                        foreach (Point p in points)
                        {
                            if (p.X >= 0 && p.X < graph.EntryWidth &&
                                p.Y >= 0 && p.Y < graph.EntryHeight &&
                                !pathOcculders[p.X, p.Y])
                            {
                                pathOcculders[p.X, p.Y] = true;
                                graph.Mark(p.X, p.Y);
                            }
                        }
                    }
                    else
                    {
                        // Erase
                        foreach (Point p in points)
                        {
                            if (p.X >= 0 && p.X < graph.EntryWidth &&
                                p.Y >= 0 && p.Y < graph.EntryHeight &&
                                pathOcculders[p.X, p.Y])
                            {
                                pathOcculders[p.X, p.Y] = false;
                                graph.Unmark(p.X, p.Y);
                            }
                        }
                    }
                }
            }
        }

        public override EventResult HandleEvent(EventType type, object sender, object tag)
        {
            if (type == EventType.KeyUp && (tag as Keys?).Value == Keys.Escape)
            {
                world.Game.Cursor = Cursors.Default;
                input.Uncapture();
                Spell.EndSpell();
                return EventResult.Handled;
            }

            if (type == EventType.LeftButtonDown)
            {
                drawing = true;
                input.Capture(this);
                return EventResult.Handled;
            }

            if (type == EventType.LeftButtonUp)
            {
                drawing = null;
                input.Uncapture();
                return EventResult.Handled;
            }

            if (type == EventType.RightButtonDown)
            {
                drawing = false;
                input.Capture(this);
                return EventResult.Handled;
            }

            if (type == EventType.RightButtonUp)
            {
                drawing = null;
                input.Uncapture();
                return EventResult.Handled;
            }

            return EventResult.Unhandled;
        }

        public void Save(Stream stream)
        {
            PathGraph graph = world.PathManager.Graph;

            StreamWriter writer = new StreamWriter(stream);

            int counter = 0;
            for (int y = 0; y < graph.EntryHeight; y++)
                for (int x = 0; x < graph.EntryWidth; x++)
                    if (pathOcculders[x, y])
                    {
                        writer.Write(x + " " + y + " ");
                        if (++counter > 10)
                        {
                            counter = 0;
                            writer.Write(writer.NewLine);
                        }
                    }
        }
    }
    #endregion

    #region SpellPunishOfNature
    public class SpellPunishOfNature : Spell
    {
        const float Duration = 60;

        float elapsedTime;

        EffectPunishOfNature effect;

        public SpellPunishOfNature(GameWorld world)
            : base(world, "PunishOfNature")
        {
        }

        public override bool Trigger()
        {
            if (Ready)
            {
                Audios.Play("OK");
                Audios.Play("Punish");
                Cast();
            }
            else
            {
                Audios.Play("Invalid");
            }

            return false;
        }

        public override void Cast()
        {
            if (Owner != null)
            {
                effect = new EffectPunishOfNature(world, Owner.Position);
                elapsedTime = 0;
                base.Cast();
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (effect != null)
            {
                effect.Update(gameTime);

                float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
                elapsedTime += elapsedSeconds;

                foreach (IWorldObject wo in
                    world.GetNearbyObjectsPrecise(effect.Position, EffectPunishOfNature.Radius))
                {
                    Charactor o = wo as Charactor;

                    if (o == null || o.Owner == null)
                        continue;

                    if (Owner.IsOpponent(o))
                    {
                        float damage = (0.2f + o.Owner.EnvironmentLevel * 0.6f) * 30;
                        o.Health -= damage * elapsedSeconds;
                    }
                    else if (Owner.IsAlly(o))
                    {
                        float health = (0.2f + (1 - o.Owner.EnvironmentLevel) * 0.6f) * 40;
                        o.Health += health * elapsedSeconds;
                        if (o.Health < o.MaximumHealth)
                            o.ShowGlow = true;
                    }
                }

                if (elapsedTime >= Duration)
                {
                    effect = null;
                }
            }

            base.Update(gameTime);
        }
    }
    #endregion

    #region Fireball
    /// <summary>
    /// Fireball entity
    /// </summary>
    public class Fireball : BaseEntity
    {
        /// <summary>
        /// Gets or sets the velocity of the fireball
        /// </summary>        
        public override Vector3 Velocity
        {
            get { return velocity; }
        }

        Vector3 velocity;

        /// <summary>
        /// Whether the fireball is exploding
        /// </summary>
        bool explode = false;

        /// <summary>
        /// Fire ball animation texture
        /// </summary>
        Texture2D[] texture;

        /// <summary>
        /// Number of texture frames
        /// </summary>
        const int FireBallTextureFrames = 23;

        /// <summary>
        /// Animation speed
        /// </summary>
        const double FrameRate = 30;

        /// <summary>
        /// Current frame
        /// </summary>
        double frame;

        /// <summary>
        /// Create a fireball entity
        /// </summary>
        /// <param name="screen"></param>
        public Fireball(GameWorld world, Vector3 initialVelocity)
            : base(world)
        {
            velocity = initialVelocity;

            texture = new Texture2D[FireBallTextureFrames];

            for (int i = 0; i < FireBallTextureFrames; i++)
            {
                texture[i] = world.Content.Load<Texture2D>(
                    "Spells/Fireball/areaeffect_" + (i + 1));
            }

            BaseGame.Singleton.Audio.Play("cast", this);
        }

        public override void Update(GameTime gameTime)
        {
            if (explode)
            {
                // Explode
                frame += gameTime.ElapsedGameTime.TotalSeconds * FrameRate;
            }
            else
            {
                // Add a little gravity, since this is fire ball, we
                // reduce the effect of gravity.
                //velocity += World.GameLogic.Gravity * 0.08f *
                //    (float)gameTime.ElapsedGameTime.TotalSeconds;

                Position += velocity;

                // Destroy anyway if we're too far away
                if (Position.LengthSquared() > 1e8)
                    GameServer.Singleton.Destroy(this);
            }

            // Hit test
            float height = World.Landscape.GetHeight(Position.X, Position.Y);
            if (height > Position.Z)
            {
                //Position.Z = height;
                explode = true;
            }

            // Destroy
            if (explode && (int)frame >= FireBallTextureFrames)
            {
                frame = 0;
                GameServer.Singleton.Destroy(this);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            // When exploding, perform an ray test with the terrain,
            // if we can't see the fireball, then nothing will be drawed since
            // we turned off depth buffer
            //if (explode)
            //{
            //    Ray ray;

            //    ray.Position = screen.Game.Eye;
            //    Vector3 v = Position - ray.Position;
            //    ray.Direction = Vector3.Normalize(v);

            //    Vector3? result = screen.Landscape.Pick();

            //    // The billboard won't be drawed if we can't see it
            //    if (result.HasValue &&
            //        result.Value.LengthSquared() < v.LengthSquared())
            //    {
            //        return;
            //    }
            //}

            //screen.Game.PointSprite.Draw(texture[(int)frame], Position, 128);

            // It's not accurate to use point sprite to draw the fireball,
            // so use center oriented billboard instead.
            Billboard billboard = new Billboard();

            billboard.Texture = texture[(int)frame];
            billboard.Normal = Vector3.Zero;
            billboard.Position = Position;
            billboard.Size.X = billboard.Size.Y = explode ? 64 : 128;
            billboard.SourceRectangle = Billboard.DefaultSourceRectangle;
            billboard.Type = BillboardType.CenterOriented;

            // Turn off depth buffer when exploding
            if (!explode)
                billboard.Type |= BillboardType.DepthBufferEnable;

            billboard.Draw();
        }
    }
    #endregion
}

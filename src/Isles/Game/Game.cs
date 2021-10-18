//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Isles.Engine;

namespace Isles
{
    #region GameIsles
    /// <summary>
    /// Game Isles
    /// </summary>
    public class GameIsles : BaseGame
    {
        private const string ConfigFile = "Settings.xml";

        /// <summary>
        /// Game screen
        /// </summary>
        private GameScreen gameScreen;

        /// <summary>
        /// Gets game screen
        /// </summary>
        public GameScreen GameScreen => gameScreen;

        /// <summary>
        /// Gets settins stream
        /// </summary>
        private static Stream SettingsStream => File.Exists(ConfigFile) ? new FileStream(ConfigFile, FileMode.Open) : null;

        public GameIsles()
            : base(Settings.CreateDefaultSettings(SettingsStream)) { }

        private TitleScreen titleScreen;

        /// <summary>
        /// Initialize everything here
        /// </summary>
        protected override void FirstTimeInitialize()
        {
            // Register everything
            Register();

            // Register screens
            AddScreen("GameScreen", gameScreen = new GameScreen());
            AddScreen("TitleScreen", titleScreen = new TitleScreen(gameScreen));
            
            // Start new level
            //using (Stream stream = new FileStream("Content/Levels/World.xml", FileMode.Open))
            //{
            //    gameScreen.StartLevel(stream);
            //}

            //StartScreen(gameScreen);
            if (Settings.DirectEnter)
            {
                gameScreen.StartLevel("Content/Levels/World.xml");

                StartScreen(gameScreen);
            }
            else
            {
                StartScreen(titleScreen);
            }

            // Start game screen
            //StartScreen(new TestScreen());

            // Handle editors
            //StartEditor(new ShadowEditor(gameScreen.World));
            //StartEditor(new BloomEditor(Bloom));
            //StartEditor(new WorldEditor(gameScreen));
        }

        protected override void Update(GameTime gameTime)
        {
            // Update game
            base.Update(gameTime);

            // Update audios
            Audios.Update(gameTime);
        }
        
        /// <summary>
        /// Starts a new editor control
        /// </summary>
        public void StartEditor(System.Windows.Forms.Form editorForm)
        {
            editorForm.Show(System.Windows.Forms.Control.FromHandle(Window.Handle));
            editorForm.Location = new System.Drawing.Point(
                Window.ClientBounds.X + Window.ClientBounds.Width / 2,
                Window.ClientBounds.Y + Window.ClientBounds.Height / 2);
            Log.Write("Editor Started: " + editorForm.Text);
        }

        private void Register()
        {
            // Register world object creators
            GameWorld.RegisterCreator("Decoration", delegate(GameWorld world) { return new Decoration(world); });
            GameWorld.RegisterCreator("Tree", delegate(GameWorld world) { return new Tree(world); });
            GameWorld.RegisterCreator("Goldmine", delegate(GameWorld world) { return new Goldmine(world); });
            GameWorld.RegisterCreator("BoxOfPandora", delegate(GameWorld world) { return new BoxOfPandora(world); });
            
            // Islander architectures
            GameWorld.RegisterCreator("Townhall", delegate(GameWorld world) { return new Building(world, "Townhall"); });
            GameWorld.RegisterCreator("Farmhouse", delegate(GameWorld world) { return new Building(world, "Farmhouse"); });
            GameWorld.RegisterCreator("Lumbermill", delegate(GameWorld world) { return new Lumbermill(world, "Lumbermill"); });
            GameWorld.RegisterCreator("Tower", delegate(GameWorld world) { return new Tower(world, "Tower"); });
            GameWorld.RegisterCreator("Barracks", delegate(GameWorld world) { return new Building(world, "Barracks"); });
            GameWorld.RegisterCreator("Altar", delegate(GameWorld world) { return new Building(world, "Altar"); });

            Spell.RegisterCreator("Townhall", delegate(GameWorld world) { return new SpellConstruct(world, "Townhall"); });
            Spell.RegisterCreator("Farmhouse", delegate(GameWorld world) { return new SpellConstruct(world, "Farmhouse"); });
            Spell.RegisterCreator("Lumbermill", delegate(GameWorld world) { return new SpellConstruct(world, "Lumbermill"); });
            Spell.RegisterCreator("Tower", delegate(GameWorld world) { return new SpellConstruct(world, "Tower"); });
            Spell.RegisterCreator("Barracks", delegate(GameWorld world) { return new SpellConstruct(world, "Barracks"); });
            Spell.RegisterCreator("Altar", delegate(GameWorld world) { return new SpellConstruct(world, "Altar"); });

            Player.RegisterBuilding("Townhall");
            Player.RegisterBuilding("Farmhouse");
            Player.RegisterBuilding("Lumbermill");
            Player.RegisterBuilding("Tower");
            Player.RegisterBuilding("Barracks");
            Player.RegisterBuilding("Altar");

            
            // Islander units
            GameWorld.RegisterCreator("Follower", delegate(GameWorld world) { return new Worker(world, "Follower"); });
            GameWorld.RegisterCreator("Militia", delegate(GameWorld world) { return new Charactor(world, "Militia"); });
            GameWorld.RegisterCreator("Hunter", delegate(GameWorld world) { return new Hunter(world, "Hunter"); });
            GameWorld.RegisterCreator("FireSorceress", delegate(GameWorld world) { return new FireSorceress(world, "FireSorceress"); });
            GameWorld.RegisterCreator("Hellfire", delegate(GameWorld world) { return new Hellfire(world, "Hellfire"); });

            Spell.RegisterCreator("Follower", delegate(GameWorld world) { return new SpellTraining(world, "Follower", null); });
            Spell.RegisterCreator("Militia", delegate(GameWorld world) { return new SpellTraining(world, "Militia", null); });
            Spell.RegisterCreator("Hunter", delegate(GameWorld world) { return new SpellTraining(world, "Hunter", null); });
            Spell.RegisterCreator("FireSorceress", delegate(GameWorld world) { return new SpellTraining(world, "FireSorceress", null); });
            Spell.RegisterCreator("Hellfire", delegate(GameWorld world) { return new SpellTraining(world, "Hellfire", null); });

            Player.RegisterCharactor("Follower");
            Player.RegisterCharactor("Militia");
            Player.RegisterCharactor("Hunter");
            Player.RegisterCharactor("FireSorceress");
            Player.RegisterCharactor("Hellfire");

            
            // Steamer architectures
            GameWorld.RegisterCreator("SteamFort", delegate(GameWorld world) { return new Building(world, "SteamFort"); });
            GameWorld.RegisterCreator("Steamhouse", delegate(GameWorld world) { return new Building(world, "Steamhouse"); });
            GameWorld.RegisterCreator("Regenerator", delegate(GameWorld world) { return new Lumbermill(world, "Regenerator"); });
            GameWorld.RegisterCreator("SteamCannon", delegate(GameWorld world) { return new Tower(world, "SteamCannon"); });
            GameWorld.RegisterCreator("TrainingCenter", delegate(GameWorld world) { return new Building(world, "TrainingCenter"); });
            GameWorld.RegisterCreator("SteamFactory", delegate(GameWorld world) { return new Building(world, "SteamFactory"); });

            Spell.RegisterCreator("SteamFort", delegate(GameWorld world) { return new SpellConstruct(world, "SteamFort"); });
            Spell.RegisterCreator("Steamhouse", delegate(GameWorld world) { return new SpellConstruct(world, "Steamhouse"); });
            Spell.RegisterCreator("Regenerator", delegate(GameWorld world) { return new SpellConstruct(world, "Regenerator"); });
            Spell.RegisterCreator("SteamCannon", delegate(GameWorld world) { return new SpellConstruct(world, "SteamCannon"); });
            Spell.RegisterCreator("TrainingCenter", delegate(GameWorld world) { return new SpellConstruct(world, "TrainingCenter"); });
            Spell.RegisterCreator("SteamFactory", delegate(GameWorld world) { return new SpellConstruct(world, "SteamFactory"); });

            Player.RegisterBuilding("SteamFort");
            Player.RegisterBuilding("Steamhouse");
            Player.RegisterBuilding("Regenerator");
            Player.RegisterBuilding("SteamCannon");
            Player.RegisterBuilding("TrainingCenter");
            Player.RegisterBuilding("SteamFactory");

            // Steamer units
            GameWorld.RegisterCreator("Miner", delegate(GameWorld world) { return new Worker(world, "Miner"); });
            GameWorld.RegisterCreator("Swordman", delegate(GameWorld world) { return new Charactor(world, "Swordman"); });
            GameWorld.RegisterCreator("Rifleman", delegate(GameWorld world) { return new Charactor(world, "Rifleman"); });
            GameWorld.RegisterCreator("Steambot", delegate(GameWorld world) { return new FireSorceress(world, "Steambot"); });

            Spell.RegisterCreator("Miner", delegate(GameWorld world) { return new SpellTraining(world, "Miner", null); });
            Spell.RegisterCreator("Swordman", delegate(GameWorld world) { return new SpellTraining(world, "Swordman", null); });
            Spell.RegisterCreator("Rifleman", delegate(GameWorld world) { return new SpellTraining(world, "Rifleman", null); });
            Spell.RegisterCreator("Steambot", delegate(GameWorld world) { return new SpellTraining(world, "Steambot", null); });
            
            Player.RegisterCharactor("Miner");
            Player.RegisterCharactor("Swordman");
            Player.RegisterCharactor("Rifleman");
            Player.RegisterCharactor("Steambot");

            GameWorld.RegisterCreator("Arrow", delegate(GameWorld world) { return new Arrow(world); });

            // Register spells
            Spell.RegisterCreator("LiveOfNature", delegate(GameWorld world) { return new SpellUpgrade(world, "LiveOfNature", Upgrades.LiveOfNature); });
            Spell.RegisterCreator("PunishOfNatureUpgrade", delegate(GameWorld world) { return new SpellUpgrade(world, "PunishOfNatureUpgrade", Upgrades.PunishOfNature); });
            Spell.RegisterCreator("AttackUpgrade", delegate(GameWorld world) { return new SpellUpgrade(world, "AttackUpgrade", Upgrades.Attack); });
            Spell.RegisterCreator("DefenseUpgrade", delegate(GameWorld world) { return new SpellUpgrade(world, "DefenseUpgrade", Upgrades.Defense); });            
            Spell.RegisterCreator("PunishOfNature", delegate(GameWorld world) { return new SpellPunishOfNature(world); });
            Spell.RegisterCreator("SummonHellfire", delegate(GameWorld world) { return new SpellSummon(world, "Hellfire"); });      
        }
    }
    #endregion

    #region Audios
    public static class Audios
    {
        public enum Channel
        {
            Building, Unit, Interface, UnderAttack
        }

        public static AudioManager Audio
        {
            get
            {
                if (audio == null)
                {
                    audio = BaseGame.Singleton.Audio;
                }

                return audio;
            }
        }

        private static AudioManager audio;
        private static Cue music;
        private static Cue building;
        private static Cue unit;
        private static Cue ui;
        private static Cue underAttack;

        // There seems to be some bugs with XACT, so we have to
        // use fixed time intervals.
        //static float buildingTimer = BuildingDuration;
        //static float unitTimer = UnitDuration;
        //static float uiTimer = UIDuration;
        private static float underAttackTimer = UnderAttackDuration;
        private const float BuildingDuration = 1.5f;
        private const float UnitDuration = 1.5f;
        private const float UIDuration = 2;
        private const float UnderAttackDuration = 30;
        private static float preValue;
        private static float postValue;
        private static float preTimer;
        private static float postTimer;
        private static int musicState;
        private static bool loopMusic;
        private static string musicName;
        public static int Counter = 0;

        public static void Play(string name)
        {
            Audio.Sound.PlayCue(name);
        }

        public static void Play(string name, IAudioEmitter emitter)
        {
            Audio.Play(name, emitter);
        }

        public static void Play(string name, Channel channel, IAudioEmitter emitter)
        {
            if (++Counter > 5)
            {
                if (channel == Channel.Building &&
                    (building == null || (building != null && (building.IsDisposed || building.IsStopped))))
                {
                    if (building != null && !building.IsDisposed)
                    {
                        //building.Dispose();
                        building = null;
                    }

                    //buildingTimer = 0;
                    building = Audio.Play(name, emitter);
                }
                else if (channel == Channel.Unit &&
                    (unit == null || (unit != null && (unit.IsDisposed || unit.IsStopped))))
                {
                    if (unit != null && !unit.IsDisposed)
                    {
                        //unit.Dispose();
                        unit = null;
                    }

                    //unitTimer = 0;                        
                    unit = Audio.Play(name, emitter);
                }
                else if (channel == Channel.Interface &&
                    (ui == null || (ui != null && (ui.IsDisposed || ui.IsStopped))))
                {
                    if (ui != null && !ui.IsDisposed)
                    {
                        //ui.Dispose();
                        ui = null;
                    }

                    //uiTimer = 0;
                    ui = Audio.Play(name);
                }
                else if (channel == Channel.UnderAttack &&
                    underAttackTimer >= UnderAttackDuration &&
                    (ui == null || (ui != null && (ui.IsDisposed || ui.IsStopped))) &&
                    (underAttack == null || (underAttack != null &&
                    (underAttack.IsDisposed || underAttack.IsStopped))))
                {
                    if (underAttack != null && !underAttack.IsDisposed)
                    {
                        //underAttack.Dispose();
                        underAttack = null;
                    }

                    underAttackTimer = 0;
                    underAttack = Audio.Play(name);
                }
            }
        }

        public static void PlayMusic(string name, bool loop, float pre, float post)
        {
            loopMusic = loop;
            preValue = pre;
            postValue = post;
            preTimer = 0;
            postTimer = 0;
            musicState = 0;
            if (music != null)
            {
                music.Stop(AudioStopOptions.AsAuthored);
            }

            music = Audio.Sound.GetCue(name);
            musicName = name;
        }

        public static void Update(GameTime gameTime)
        {
            var elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (music != null)
            {
                if (musicState == 0)
                {
                    preTimer += elapsed;

                    if (preTimer >= preValue)
                    {
                        musicState = 1;
                        music = Audio.Play(musicName);
                    }
                }
                else if (musicState == 2)
                {
                    postTimer += elapsed;

                    if (postTimer >= postValue)
                    {
                        // Choose a random background music
                        musicName = (Helper.Random.Next(2) == 0 ? "Steamer" : "Islander");
                        musicState = 0;
                    }
                }
                else if (musicState == 1 && music.IsStopped && loopMusic)
                {
                    musicState = 2;
                }
            }

            //if (building != null && buildingTimer < BuildingDuration)
            //    buildingTimer += elapsed;

            //if (unit != null && unitTimer < UnitDuration)
            //    unitTimer += elapsed;

            //if (ui != null && uiTimer < UIDuration)
            //    uiTimer += elapsed;

            if (underAttack != null && underAttackTimer < UnderAttackDuration)
            {
                underAttackTimer += elapsed;
            }
        }
    }
    #endregion
}

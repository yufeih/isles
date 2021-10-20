// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Isles
{
    /// <summary>
    /// Game Isles.
    /// </summary>
    public class GameIsles : BaseGame
    {
        /// <summary>
        /// Gets game screen.
        /// </summary>
        public GameScreen GameScreen { get; private set; }

        private TitleScreen titleScreen;

        /// <summary>
        /// Initialize everything here.
        /// </summary>
        protected override void FirstTimeInitialize()
        {
            // Register everything
            Register();

            // Register screens
            AddScreen("GameScreen", GameScreen = new GameScreen());
            AddScreen("TitleScreen", titleScreen = new TitleScreen(GameScreen));

            StartScreen(titleScreen);
        }

        protected override void Update(GameTime gameTime)
        {
            // Update game
            base.Update(gameTime);

            // Update audios
            Audios.Update(gameTime);
        }

        private void Register()
        {
            // Register world object creators
            GameWorld.RegisterCreator("Decoration", world => { return new Decoration(world); });
            GameWorld.RegisterCreator("Tree", world => { return new Tree(world); });
            GameWorld.RegisterCreator("Goldmine", world => { return new Goldmine(world); });
            GameWorld.RegisterCreator("BoxOfPandora", world => { return new BoxOfPandora(world); });

            // Islander architectures
            GameWorld.RegisterCreator("Townhall", world => { return new Building(world, "Townhall"); });
            GameWorld.RegisterCreator("Farmhouse", world => { return new Building(world, "Farmhouse"); });
            GameWorld.RegisterCreator("Lumbermill", world => { return new Lumbermill(world, "Lumbermill"); });
            GameWorld.RegisterCreator("Tower", world => { return new Tower(world, "Tower"); });
            GameWorld.RegisterCreator("Barracks", world => { return new Building(world, "Barracks"); });
            GameWorld.RegisterCreator("Altar", world => { return new Building(world, "Altar"); });

            Spell.RegisterCreator("Townhall", world => { return new SpellConstruct(world, "Townhall"); });
            Spell.RegisterCreator("Farmhouse", world => { return new SpellConstruct(world, "Farmhouse"); });
            Spell.RegisterCreator("Lumbermill", world => { return new SpellConstruct(world, "Lumbermill"); });
            Spell.RegisterCreator("Tower", world => { return new SpellConstruct(world, "Tower"); });
            Spell.RegisterCreator("Barracks", world => { return new SpellConstruct(world, "Barracks"); });
            Spell.RegisterCreator("Altar", world => { return new SpellConstruct(world, "Altar"); });

            Player.RegisterBuilding("Townhall");
            Player.RegisterBuilding("Farmhouse");
            Player.RegisterBuilding("Lumbermill");
            Player.RegisterBuilding("Tower");
            Player.RegisterBuilding("Barracks");
            Player.RegisterBuilding("Altar");

            // Islander units
            GameWorld.RegisterCreator("Follower", world => { return new Worker(world, "Follower"); });
            GameWorld.RegisterCreator("Militia", world => { return new Charactor(world, "Militia"); });
            GameWorld.RegisterCreator("Hunter", world => { return new Hunter(world, "Hunter"); });
            GameWorld.RegisterCreator("FireSorceress", world => { return new FireSorceress(world, "FireSorceress"); });
            GameWorld.RegisterCreator("Hellfire", world => { return new Hellfire(world, "Hellfire"); });

            Spell.RegisterCreator("Follower", world => { return new SpellTraining(world, "Follower", null); });
            Spell.RegisterCreator("Militia", world => { return new SpellTraining(world, "Militia", null); });
            Spell.RegisterCreator("Hunter", world => { return new SpellTraining(world, "Hunter", null); });
            Spell.RegisterCreator("FireSorceress", world => { return new SpellTraining(world, "FireSorceress", null); });
            Spell.RegisterCreator("Hellfire", world => { return new SpellTraining(world, "Hellfire", null); });

            Player.RegisterCharactor("Follower");
            Player.RegisterCharactor("Militia");
            Player.RegisterCharactor("Hunter");
            Player.RegisterCharactor("FireSorceress");
            Player.RegisterCharactor("Hellfire");

            // Steamer architectures
            GameWorld.RegisterCreator("SteamFort", world => { return new Building(world, "SteamFort"); });
            GameWorld.RegisterCreator("Steamhouse", world => { return new Building(world, "Steamhouse"); });
            GameWorld.RegisterCreator("Regenerator", world => { return new Lumbermill(world, "Regenerator"); });
            GameWorld.RegisterCreator("SteamCannon", world => { return new Tower(world, "SteamCannon"); });
            GameWorld.RegisterCreator("TrainingCenter", world => { return new Building(world, "TrainingCenter"); });
            GameWorld.RegisterCreator("SteamFactory", world => { return new Building(world, "SteamFactory"); });

            Spell.RegisterCreator("SteamFort", world => { return new SpellConstruct(world, "SteamFort"); });
            Spell.RegisterCreator("Steamhouse", world => { return new SpellConstruct(world, "Steamhouse"); });
            Spell.RegisterCreator("Regenerator", world => { return new SpellConstruct(world, "Regenerator"); });
            Spell.RegisterCreator("SteamCannon", world => { return new SpellConstruct(world, "SteamCannon"); });
            Spell.RegisterCreator("TrainingCenter", world => { return new SpellConstruct(world, "TrainingCenter"); });
            Spell.RegisterCreator("SteamFactory", world => { return new SpellConstruct(world, "SteamFactory"); });

            Player.RegisterBuilding("SteamFort");
            Player.RegisterBuilding("Steamhouse");
            Player.RegisterBuilding("Regenerator");
            Player.RegisterBuilding("SteamCannon");
            Player.RegisterBuilding("TrainingCenter");
            Player.RegisterBuilding("SteamFactory");

            // Steamer units
            GameWorld.RegisterCreator("Miner", world => { return new Worker(world, "Miner"); });
            GameWorld.RegisterCreator("Swordman", world => { return new Charactor(world, "Swordman"); });
            GameWorld.RegisterCreator("Rifleman", world => { return new Charactor(world, "Rifleman"); });
            GameWorld.RegisterCreator("Steambot", world => { return new FireSorceress(world, "Steambot"); });

            Spell.RegisterCreator("Miner", world => { return new SpellTraining(world, "Miner", null); });
            Spell.RegisterCreator("Swordman", world => { return new SpellTraining(world, "Swordman", null); });
            Spell.RegisterCreator("Rifleman", world => { return new SpellTraining(world, "Rifleman", null); });
            Spell.RegisterCreator("Steambot", world => { return new SpellTraining(world, "Steambot", null); });

            Player.RegisterCharactor("Miner");
            Player.RegisterCharactor("Swordman");
            Player.RegisterCharactor("Rifleman");
            Player.RegisterCharactor("Steambot");

            GameWorld.RegisterCreator("Arrow", world => { return new Arrow(world); });

            // Register spells
            Spell.RegisterCreator("LiveOfNature", world => { return new SpellUpgrade(world, "LiveOfNature", Upgrades.LiveOfNature); });
            Spell.RegisterCreator("PunishOfNatureUpgrade", world => { return new SpellUpgrade(world, "PunishOfNatureUpgrade", Upgrades.PunishOfNature); });
            Spell.RegisterCreator("AttackUpgrade", world => { return new SpellUpgrade(world, "AttackUpgrade", Upgrades.Attack); });
            Spell.RegisterCreator("DefenseUpgrade", world => { return new SpellUpgrade(world, "DefenseUpgrade", Upgrades.Defense); });
            Spell.RegisterCreator("PunishOfNature", world => { return new SpellPunishOfNature(world); });
            Spell.RegisterCreator("SummonHellfire", world => { return new SpellSummon(world, "Hellfire"); });
        }
    }

    public static class Audios
    {
        public enum Channel
        {
            Building,
            Unit,
            Interface,
            UnderAttack,
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
        // static float buildingTimer = BuildingDuration;
        // static float unitTimer = UnitDuration;
        // static float uiTimer = UIDuration;
        private static float underAttackTimer = UnderAttackDuration;
        private const float UnderAttackDuration = 30;
        private static float preValue;
        private static float postValue;
        private static float preTimer;
        private static float postTimer;
        private static int musicState;
        private static bool loopMusic;
        private static string musicName;
        public static int Counter;

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
                        // building.Dispose();
                        building = null;
                    }

                    // buildingTimer = 0;
                    building = Audio.Play(name, emitter);
                }
                else if (channel == Channel.Unit &&
                    (unit == null || (unit != null && (unit.IsDisposed || unit.IsStopped))))
                {
                    if (unit != null && !unit.IsDisposed)
                    {
                        // unit.Dispose();
                        unit = null;
                    }

                    // unitTimer = 0;
                    unit = Audio.Play(name, emitter);
                }
                else if (channel == Channel.Interface &&
                    (ui == null || (ui != null && (ui.IsDisposed || ui.IsStopped))))
                {
                    if (ui != null && !ui.IsDisposed)
                    {
                        // ui.Dispose();
                        ui = null;
                    }

                    // uiTimer = 0;
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
                        // underAttack.Dispose();
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
                        musicName = Helper.Random.Next(2) == 0 ? "Steamer" : "Islander";
                        musicState = 0;
                    }
                }
                else if (musicState == 1 && music.IsStopped && loopMusic)
                {
                    musicState = 2;
                }
            }

            // if (building != null && buildingTimer < BuildingDuration)
            //    buildingTimer += elapsed;

            // if (unit != null && unitTimer < UnitDuration)
            //    unitTimer += elapsed;

            // if (ui != null && uiTimer < UIDuration)
            //    uiTimer += elapsed;
            if (underAttack != null && underAttackTimer < UnderAttackDuration)
            {
                underAttackTimer += elapsed;
            }
        }
    }
}

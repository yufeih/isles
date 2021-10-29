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
        protected override void FirstTimeInitialize()
        {
            Register();

            if (!string.IsNullOrEmpty(EnvironmentVariables.StartupLevel))
            {
                StartScreen(new GameScreen(EnvironmentVariables.StartupLevel));
            }
            else
            {
                StartScreen(new TitleScreen());
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            Audios.Update(gameTime);
        }

        private void Register()
        {
            // Register world object creators
            GameWorld.RegisterCreator("Decoration", world => new Decoration(world));
            GameWorld.RegisterCreator("Tree", world => new Tree(world));
            GameWorld.RegisterCreator("Goldmine", world => new Goldmine(world));
            GameWorld.RegisterCreator("BoxOfPandora", world => new BoxOfPandora(world));

            // Islander architectures
            GameWorld.RegisterCreator("Townhall", world => new Building(world, "Townhall"));
            GameWorld.RegisterCreator("Farmhouse", world => new Building(world, "Farmhouse"));
            GameWorld.RegisterCreator("Lumbermill", world => new Lumbermill(world, "Lumbermill"));
            GameWorld.RegisterCreator("Tower", world => new Tower(world, "Tower"));
            GameWorld.RegisterCreator("Barracks", world => new Building(world, "Barracks"));
            GameWorld.RegisterCreator("Altar", world => new Building(world, "Altar"));

            Spell.RegisterCreator("Townhall", world => new SpellConstruct(world, "Townhall"));
            Spell.RegisterCreator("Farmhouse", world => new SpellConstruct(world, "Farmhouse"));
            Spell.RegisterCreator("Lumbermill", world => new SpellConstruct(world, "Lumbermill"));
            Spell.RegisterCreator("Tower", world => new SpellConstruct(world, "Tower"));
            Spell.RegisterCreator("Barracks", world => new SpellConstruct(world, "Barracks"));
            Spell.RegisterCreator("Altar", world => new SpellConstruct(world, "Altar"));

            Player.RegisterBuilding("Townhall");
            Player.RegisterBuilding("Farmhouse");
            Player.RegisterBuilding("Lumbermill");
            Player.RegisterBuilding("Tower");
            Player.RegisterBuilding("Barracks");
            Player.RegisterBuilding("Altar");

            // Islander units
            GameWorld.RegisterCreator("Follower", world => new Worker(world, "Follower"));
            GameWorld.RegisterCreator("Militia", world => new Charactor(world, "Militia"));
            GameWorld.RegisterCreator("Hunter", world => new Hunter(world, "Hunter"));
            GameWorld.RegisterCreator("FireSorceress", world => new FireSorceress(world, "FireSorceress"));
            GameWorld.RegisterCreator("Hellfire", world => new Hellfire(world, "Hellfire"));

            Spell.RegisterCreator("Follower", world => new SpellTraining(world, "Follower", null));
            Spell.RegisterCreator("Militia", world => new SpellTraining(world, "Militia", null));
            Spell.RegisterCreator("Hunter", world => new SpellTraining(world, "Hunter", null));
            Spell.RegisterCreator("FireSorceress", world => new SpellTraining(world, "FireSorceress", null));
            Spell.RegisterCreator("Hellfire", world => new SpellTraining(world, "Hellfire", null));

            Player.RegisterCharactor("Follower");
            Player.RegisterCharactor("Militia");
            Player.RegisterCharactor("Hunter");
            Player.RegisterCharactor("FireSorceress");
            Player.RegisterCharactor("Hellfire");

            // Register spells
            Spell.RegisterCreator("LiveOfNature", world => new SpellUpgrade(world, "LiveOfNature", Upgrades.LiveOfNature));
            Spell.RegisterCreator("PunishOfNatureUpgrade", world => new SpellUpgrade(world, "PunishOfNatureUpgrade", Upgrades.PunishOfNature));
            Spell.RegisterCreator("AttackUpgrade", world => new SpellUpgrade(world, "AttackUpgrade", Upgrades.Attack));
            Spell.RegisterCreator("DefenseUpgrade", world => new SpellUpgrade(world, "DefenseUpgrade", Upgrades.Defense));
            Spell.RegisterCreator("PunishOfNature", world => new SpellPunishOfNature(world));
            Spell.RegisterCreator("SummonHellfire", world => new SpellSummon(world, "Hellfire"));
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

            if (underAttack != null && underAttackTimer < UnderAttackDuration)
            {
                underAttackTimer += elapsed;
            }
        }
    }
}

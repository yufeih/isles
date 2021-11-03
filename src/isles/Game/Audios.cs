// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Isles
{
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
                        ui = null;
                    }
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

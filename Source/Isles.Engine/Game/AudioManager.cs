#region File Description
//-----------------------------------------------------------------------------
// AudioManager.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
#endregion

namespace Isles.Engine
{
    /// <summary>
    /// Interface used by the AudioManager to look up the position
    /// and velocity of entities that can emit 3D sounds.
    /// </summary>
    public interface IAudioEmitter
    {
        Vector3 Position { get; }
        Vector3 Forward { get; }
        Vector3 Up { get; }
        Vector3 Velocity { get; }
    }

    /// <summary>
    /// Audio manager keeps track of what 3D sounds are playing, updating
    /// their settings as the camera and entities move around the world,
    /// and automatically disposing cue instances after they finish playing.
    /// </summary>
    public class AudioManager : Microsoft.Xna.Framework.GameComponent
    {
        #region Fields
        ZipContentManager content;

        // XACT objects.
        AudioEngine audioEngine;
        WaveBank waveBank;
        SoundBank soundBank;

        public AudioEngine Audio
        {
            get { return audioEngine; }
        }

        public WaveBank Wave
        {
            get { return waveBank; }
        }

        public SoundBank Sound
        {
            get { return soundBank; }
        }

        // The listener describes the ear which is hearing 3D sounds.
        // This is usually set to match the camera.
        public AudioListener Listener
        {
            get { return listener; }
        }

        AudioListener listener = new AudioListener();


        // The emitter describes an entity which is making a 3D sound.
        AudioEmitter emitter = new AudioEmitter();


        // Keep track of all the 3D sounds that are currently playing.
        List<Cue3D> activeCues = new List<Cue3D>();


        // Keep track of spare Cue3D instances, so we can reuse them.
        // Otherwise we would have to allocate new instances each time
        // a sound was played, which would create unnecessary garbage.
        Stack<Cue3D> cuePool = new Stack<Cue3D>();


        #endregion


        public AudioManager(Game game, ZipContentManager content)
            : base(game)
        {
            if (content == null)
                throw new ArgumentException();
            this.content = content;
        }


        /// <summary>
        /// Loads the XACT data.
        /// </summary>
        public override void Initialize()
        {
            audioEngine = content.LoadAudioEngine("Content/Audios/Isles.xgs");
            waveBank = content.LoadWaveBank(audioEngine, "Content/Audios/Isles.xwb");
            soundBank = content.LoadSoundBank(audioEngine, "Content/Audios/Isles.xsb");

            base.Initialize();
        }


        /// <summary>
        /// Unloads the XACT data.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (soundBank != null)
                        soundBank.Dispose();

                    if (waveBank != null)
                        waveBank.Dispose();

                    if (audioEngine != null)
                        audioEngine.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        
        /// <summary>
        /// Updates the state of the 3D audio system.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Loop over all the currently playing 3D sounds.
            int index = 0;

            while (index < activeCues.Count)
            {
                Cue3D cue3D = activeCues[index];

                if (!cue3D.Cue.IsDisposed && cue3D.Cue.IsStopped)
                {
                    // If the cue has stopped playing, dispose it.
                    cue3D.Cue.Dispose();

                    // Store the Cue3D instance for future reuse.
                    cuePool.Push(cue3D);

                    // Remove it from the active list.
                    activeCues.RemoveAt(index);
                }
                else
                {
                    // If the cue is still playing, update its 3D settings.
                    Apply3D(cue3D);

                    index++;
                }
            }

            // Update the XACT engine.
            // Some bugs with the audio engine. Sometimes it causes the game to stuck :(
            //audioEngine.Update();

            // Update background musc
            if (backgroundMusic != null && (delayBeforePlaying || backgroundMusic.IsStopped))
            {
                delayedLoopTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (delayedLoopTimer >= delayedLoopSeconds)
                {
                    delayBeforePlaying = false;
                    PlayBackground(backgroundMusicName, delayedLoopSeconds);
                    delayedLoopTimer = 0;
                }
            }

            base.Update(gameTime);
        }


        /// <summary>
        /// Triggers a new sound
        /// </summary>
        /// <param name="cueName"></param>
        /// <returns></returns>
        public Cue Play(string cueName)
        {
            Cue cue = soundBank.GetCue(cueName);
            cue.Play();
            return cue;
        }


        /// <summary>
        /// Triggers a new 3D sound.
        /// </summary>
        public Cue Play(string cueName, IAudioEmitter emitter)
        {
            if (emitter == null)
                return Play(cueName);

            Cue3D cue3D;

            if (cuePool.Count > 0)
            {
                // If possible, reuse an existing Cue3D instance.
                cue3D = cuePool.Pop();
            }
            else
            {
                // Otherwise we have to allocate a new one.
                cue3D = new Cue3D();
            }

            // Fill in the cue and emitter fields.
            cue3D.Cue = soundBank.GetCue(cueName);
            cue3D.Emitter = emitter;

            // Set the 3D position of this cue, and then play it.
            Apply3D(cue3D);

            cue3D.Cue.Play();

            // Remember that this cue is now active.
            activeCues.Add(cue3D);

            return cue3D.Cue;
        }

        /// <summary>
        /// Play a background music
        /// </summary>
        public Cue PlayBackground(string cueName, float delayedLoopSeconds)
        {
            return PlayBackground(cueName, delayedLoopSeconds, false);
        }

        public Cue PlayBackground(string cueName, float delayedLoopSeconds, bool delayBeforePlaying)
        {
            this.delayBeforePlaying = delayBeforePlaying;
            this.delayedLoopSeconds = delayedLoopSeconds;

            Cue cue = soundBank.GetCue(cueName);

            if (backgroundMusic != null && !backgroundMusicName.Equals(cueName))
                backgroundMusic.Stop(AudioStopOptions.AsAuthored);

            if (!delayBeforePlaying)
            {
                if (!backgroundMusicName.Equals(cueName))
                {
                    cue.Play();
                }
                else if (!backgroundMusic.IsPlaying)
                {
                    cue.Play();
                }
            }

            backgroundMusicName = cueName;
            return backgroundMusic = cue;
        }

        float delayedLoopTimer;
        float delayedLoopSeconds;
        Cue backgroundMusic;
        string backgroundMusicName = "";
        bool delayBeforePlaying;

        /// <summary>
        /// Updates the position and velocity settings of a 3D cue.
        /// </summary>
        private void Apply3D(Cue3D cue3D)
        {
            if (!cue3D.Cue.IsDisposed)
            {
                emitter.Position = cue3D.Emitter.Position;
                emitter.Forward = cue3D.Emitter.Forward;
                emitter.Up = cue3D.Emitter.Up;
                emitter.Velocity = cue3D.Emitter.Velocity;

                cue3D.Cue.Apply3D(listener, emitter);
            }
        }


        /// <summary>
        /// Internal helper class for keeping track of an active 3D cue,
        /// and remembering which emitter object it is attached to.
        /// </summary>
        private struct Cue3D
        {
            public Cue Cue;
            public IAudioEmitter Emitter;
        }
    }
}

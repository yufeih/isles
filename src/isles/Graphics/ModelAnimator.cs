// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Microsoft.Xna.Framework;

namespace Isles.Graphics
{
    public class ModelAnimator
    {
        private readonly Model _model;
        private readonly Matrix[] _matrices;

        public ModelAnimator(Model model)
        {
            _model = model;
            _matrices = new Matrix[model.Nodes.Length];
        }

        public Matrix[] GetWorldTransforms(string animationName, float time, bool loop)
        {
            if (_model.Animations.TryGetValue(animationName, out var animation) && loop)
            {
                time %= animation.Duration;
            }

            // Calculate local transforms
            for (var node = 0; node < _model.Nodes.Length; node++)
            {
                var scale = Vector3.One;
                var rotation = Quaternion.Identity;
                var translation = Vector3.Zero;

                if (animation is null || !animation.Channels.TryGetValue(node, out var channel))
                {
                    scale = _model.Nodes[node].Scale;
                    rotation = _model.Nodes[node].Rotation;
                    translation = _model.Nodes[node].Translation;
                }
                else
                {
                    if (channel.Scale.Times != null)
                    {
                        var (i, lerpAmount) = FindStartIndexAndLerpAmount(time, channel.Scale.Times);
                        scale = Vector3.Lerp(channel.Scale.Values[i], channel.Scale.Values[i + 1], lerpAmount);
                    }

                    if (channel.Rotation.Times != null)
                    {
                        var (i, lerpAmount) = FindStartIndexAndLerpAmount(time, channel.Rotation.Times);
                        rotation = Quaternion.Lerp(channel.Rotation.Values[i], channel.Rotation.Values[i + 1], lerpAmount);
                    }

                    if (channel.Translation.Times != null)
                    {
                        var (i, lerpAmount) = FindStartIndexAndLerpAmount(time, channel.Translation.Times);
                        translation = Vector3.Lerp(channel.Translation.Values[i], channel.Translation.Values[i + 1], lerpAmount);
                    }
                }

                _matrices[node] = Matrix.CreateScale(scale) *
                                  Matrix.CreateFromQuaternion(rotation) *
                                  Matrix.CreateTranslation(translation);
            }

            // Local transforms to world transforms
            for (var node = 1; node < _model.Nodes.Length; node++)
            {
                _matrices[node] *= _matrices[_model.Nodes[node].ParentIndex];
            }

            return _matrices;
        }

        private static (int, float) FindStartIndexAndLerpAmount(float time, float[] times)
        {
            var i = Array.BinarySearch(times, time);
            if (i < 0)
            {
                i = -i - 1;
            }
            if (i >= times.Length - 1)
            {
                i = times.Length - 2;
            }

            var lerpAmount = (time - times[i]) / (times[i + 1] - times[i]);
            return (i, MathHelper.Clamp(lerpAmount, 0, 1));
        }
    }

    /// <summary>
    /// The animation player is in charge of decoding bone position
    /// matrices from an animation clip.
    /// </summary>
    public class AnimationPlayer
    {
        private readonly Model _model;
        private readonly ModelAnimator _animator;

        private string CurrentClip = "";

        private TimeSpan currentTimeValue;

        public (float percent, Action)[] Triggers;
        public EventHandler Complete;
        public bool Loop;

        public AnimationPlayer(Model model)
        {
            _model = model;
            _animator = new ModelAnimator(model);
        }

        public void StartClip(string clip)
        {
            CurrentClip = clip ?? "";
            currentTimeValue = TimeSpan.Zero;
        }

        public void Update(TimeSpan time, bool relativeToCurrentTime)
        {
            UpdateBoneTransforms(time, relativeToCurrentTime);
        }

        private void UpdateBoneTransforms(TimeSpan time, bool relativeToCurrentTime)
        {
            var duration = TimeSpan.FromSeconds( _model.Animations[CurrentClip].Duration);

            if (currentTimeValue >= duration && !Loop)
            {
                return;
            }

            // Update triggers
            if (Triggers != null)
            {
                foreach (var (percent, trigger) in Triggers)
                {
                    var key = duration.TotalSeconds * percent;
                    if (currentTimeValue.TotalSeconds < key &&
                        currentTimeValue.TotalSeconds + time.TotalSeconds > key)
                    {
                        trigger();
                    }
                }
            }

            // Update the animation position.
            if (relativeToCurrentTime)
            {
                time += currentTimeValue;

                // If we reached the end, loop back to the start.
                while (time >= duration)
                {
                    // Trigger complete event
                    Complete?.Invoke(null, EventArgs.Empty);

                    if (Loop)
                    {
                        time -= duration;
                    }
                    else
                    {
                        currentTimeValue = duration;
                        time = currentTimeValue;
                        break;
                    }
                }
            }

            currentTimeValue = time;
        }

        public Matrix[] GetWorldTransforms()
        {
            return _animator.GetWorldTransforms(CurrentClip, (float)currentTimeValue.TotalSeconds, Loop);
        }
    }
}
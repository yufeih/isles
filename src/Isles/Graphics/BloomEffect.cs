// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    /// <summary>
    /// Class holds all the settings used to tweak the bloom effect.
    /// Code grabbed from XNA creators club samples :).
    /// </summary>
    public class BloomSettings
    {
        // Name of a preset bloom setting, for display to the user.
        public string Name { get; set; }

        // Controls how bright a pixel needs to be before it will bloom.
        // Zero makes everything bloom equally, while higher values select
        // only brighter colors. Somewhere between 0.25 and 0.5 is good.
        public float BloomThreshold { get; set; }

        // Controls how much blurring is applied to the bloom image.
        // The typical range is from 1 up to 10 or so.
        public float BlurAmount { get; set; }

        // Controls the amount of the bloom and base images that
        // will be mixed into the final scene. Range 0 to 1.
        public float BloomIntensity { get; set; }
        public float BaseIntensity { get; set; }

        // Independently control the color saturation of the bloom and
        // base images. Zero is totally desaturated, 1.0 leaves saturation
        // unchanged, while higher values increase the saturation level.
        public float BloomSaturation { get; set; }
        public float BaseSaturation { get; set; }

        public BloomSettings()
        {
        }

        /// <summary>
        /// Constructs a new bloom settings descriptor.
        /// </summary>
        public BloomSettings(string name, float bloomThreshold, float blurAmount,
                             float bloomIntensity, float baseIntensity,
                             float bloomSaturation, float baseSaturation)
        {
            Name = name;
            BloomThreshold = bloomThreshold;
            BlurAmount = blurAmount;
            BloomIntensity = bloomIntensity;
            BaseIntensity = baseIntensity;
            BloomSaturation = bloomSaturation;
            BaseSaturation = baseSaturation;
        }

        public static BloomSettings Lerp(BloomSettings settings1, BloomSettings settings2, float amount)
        {
            var settings = new BloomSettings
            {
                BaseIntensity = MathHelper.Lerp(settings1.BaseIntensity, settings2.BaseIntensity, amount),
                BaseSaturation = MathHelper.Lerp(settings1.BaseSaturation, settings2.BaseSaturation, amount),
                BloomIntensity = MathHelper.Lerp(settings1.BloomIntensity, settings2.BloomIntensity, amount),
                BloomSaturation = MathHelper.Lerp(settings1.BloomSaturation, settings2.BloomSaturation, amount),
                BloomThreshold = MathHelper.Lerp(settings1.BloomThreshold, settings2.BloomThreshold, amount),
                BlurAmount = MathHelper.Lerp(settings1.BlurAmount, settings2.BlurAmount, amount),
            };

            return settings;
        }

        /// <summary>
        /// Table of preset bloom settings, used by the sample program.
        /// </summary>
        public static BloomSettings[] PresetSettings =
        {
            // Name           Thresh  Blur Bloom  Base  BloomSat BaseSat
            new BloomSettings("Saturated",   0.25f,  4,   2,     1,    2,       0),
            new BloomSettings("Soft",        0,      3,   1,     1,    1,       1),
            new BloomSettings("Default",     0.25f,  0.02f,   1.25f, 1,    1,       1),
            new BloomSettings("Desaturated", 0.5f,   8,   2,     1,    0,       1),
            new BloomSettings("Blurry",      0,      2,   1,     0.1f, 1,       1),
            new BloomSettings("Subtle",      0.5f,   2,   1,     1,    1,       1),
        };
    }

    public class BloomEffect : DrawableGameComponent
    {
        private readonly ContentManager content;
        private SpriteBatch spriteBatch;
        private Effect bloomExtractEffect;
        private Effect bloomCombineEffect;
        private Effect gaussianBlurEffect;
        private RenderTarget2D resolveTarget;
        private RenderTarget2D renderTarget1;
        private RenderTarget2D renderTarget2;

        // Choose what display settings the bloom should use.
        public BloomSettings Settings { get; set; } = BloomSettings.PresetSettings[0];

        // Optionally displays one of the intermediate buffers used
        // by the bloom postprocess, so you can see exactly what is
        // being drawn into each rendertarget.
        public enum IntermediateBuffer
        {
            PreBloom,
            BlurredHorizontally,
            BlurredBothWays,
            FinalResult,
        }

        public IntermediateBuffer ShowBuffer { get; set; } = IntermediateBuffer.FinalResult;

        public BloomEffect(Game game, ContentManager content)
            : base(game)
        {
            if (game == null)
            {
                throw new ArgumentNullException("game");
            }

            this.content = content;
        }

        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            bloomExtractEffect = content.Load<Effect>("Effects/BloomExtract");
            bloomCombineEffect = content.Load<Effect>("Effects/BloomCombine");
            gaussianBlurEffect = content.Load<Effect>("Effects/GaussianBlur");

            // Look up the resolution and format of our main backbuffer.
            PresentationParameters pp = GraphicsDevice.PresentationParameters;

            var width = pp.BackBufferWidth;
            var height = pp.BackBufferHeight;

            // Create a texture for reading back the backbuffer contents.
            resolveTarget = new RenderTarget2D(GraphicsDevice, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat);

            // Create two rendertargets for the bloom processing. These are half the
            // size of the backbuffer, in order to minimize fillrate costs. Reducing
            // the resolution in this way doesn't hurt quality, because we are going
            // to be blurring the bloom images in any case.
            width /= 2;
            height /= 2;

            renderTarget1 = new RenderTarget2D(GraphicsDevice, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat);
            renderTarget2 = new RenderTarget2D(GraphicsDevice, width, height, false, pp.BackBufferFormat, pp.DepthStencilFormat);
        }

        /// <summary>
        /// Unload your graphics content.
        /// </summary>
        protected override void UnloadContent()
        {
            resolveTarget.Dispose();
            renderTarget1.Dispose();
            renderTarget2.Dispose();
        }

        public void BeginDraw()
        {
            GraphicsDevice.SetRenderTarget(resolveTarget);
        }

        /// <summary>
        /// This is where it all happens. Grabs a scene that has already been rendered,
        /// and uses postprocess magic to add a glowing bloom effect over the top of it.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // Resolve the scene into a texture, so we can
            // use it as input data for the bloom processing.
            GraphicsDevice.SetRenderTarget(null);

            // Pass 1: draw the scene into rendertarget 1, using a
            // shader that extracts only the brightest parts of the image.
            bloomExtractEffect.Parameters["BloomThreshold"].SetValue(
                Settings.BloomThreshold);

            DrawFullscreenQuad(resolveTarget, renderTarget1,
                               bloomExtractEffect,
                               IntermediateBuffer.PreBloom);

            // Pass 2: draw from rendertarget 1 into rendertarget 2,
            // using a shader to apply a horizontal gaussian blur filter.
            SetBlurEffectParameters(1.0f / renderTarget1.Width, 0);

            DrawFullscreenQuad(renderTarget1, renderTarget2,
                               gaussianBlurEffect,
                               IntermediateBuffer.BlurredHorizontally);

            // Pass 3: draw from rendertarget 2 back into rendertarget 1,
            // using a shader to apply a vertical gaussian blur filter.
            SetBlurEffectParameters(0, 1.0f / renderTarget1.Height);

            DrawFullscreenQuad(renderTarget2, renderTarget1,
                               gaussianBlurEffect,
                               IntermediateBuffer.BlurredBothWays);

            // Pass 4: draw both rendertarget 1 and the original scene
            // image back into the main backbuffer, using a shader that
            // combines them to produce the final bloomed result.
            GraphicsDevice.SetRenderTarget(null);

            EffectParameterCollection parameters = bloomCombineEffect.Parameters;

            parameters["BloomIntensity"].SetValue(Settings.BloomIntensity);
            parameters["BaseIntensity"].SetValue(Settings.BaseIntensity);
            parameters["BloomSaturation"].SetValue(Settings.BloomSaturation);
            parameters["BaseSaturation"].SetValue(Settings.BaseSaturation);

            GraphicsDevice.Textures[1] = resolveTarget;

            Viewport viewport = GraphicsDevice.Viewport;

            DrawFullscreenQuad(renderTarget1,
                               viewport.Width, viewport.Height,
                               bloomCombineEffect,
                               IntermediateBuffer.FinalResult);
        }

        /// <summary>
        /// Helper for drawing a texture into a rendertarget, using
        /// a custom shader to apply postprocessing effects.
        /// </summary>
        private void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget,
                                Effect effect, IntermediateBuffer currentBuffer)
        {
            GraphicsDevice.SetRenderTarget(renderTarget);

            DrawFullscreenQuad(texture,
                               renderTarget.Width, renderTarget.Height,
                               effect, currentBuffer);

            GraphicsDevice.SetRenderTarget(null);
        }

        /// <summary>
        /// Helper for drawing a texture into the current rendertarget,
        /// using a custom shader to apply postprocessing effects.
        /// </summary>
        private void DrawFullscreenQuad(Texture2D texture, int width, int height,
                                Effect effect, IntermediateBuffer currentBuffer)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);

            // Begin the custom effect, if it is currently enabled. If the user
            // has selected one of the show intermediate buffer options, we still
            // draw the quad to make sure the image will end up on the screen,
            // but might need to skip applying the custom pixel shader.
            if (ShowBuffer >= currentBuffer)
            {
                effect.CurrentTechnique.Passes[0].Apply();
            }

            // Draw the quad.
            spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Computes sample weightings and texture coordinate offsets
        /// for one pass of a separable gaussian blur filter.
        /// </summary>
        private void SetBlurEffectParameters(float dx, float dy)
        {
            // Look up the sample weight and offset effect parameters.
            EffectParameter weightsParameter, offsetsParameter;

            weightsParameter = gaussianBlurEffect.Parameters["SampleWeights"];
            offsetsParameter = gaussianBlurEffect.Parameters["SampleOffsets"];

            // Look up how many samples our gaussian blur effect supports.
            var sampleCount = weightsParameter.Elements.Count;

            // Create temporary arrays for computing our filter settings.
            var sampleWeights = new float[sampleCount];
            var sampleOffsets = new Vector2[sampleCount];

            // The first sample always has a zero offset.
            sampleWeights[0] = ComputeGaussian(0);
            sampleOffsets[0] = new Vector2(0);

            // Maintain a sum of all the weighting values.
            var totalWeights = sampleWeights[0];

            // Add pairs of additional sample taps, positioned
            // along a line in both directions from the center.
            for (var i = 0; i < sampleCount / 2; i++)
            {
                // Store weights for the positive and negative taps.
                var weight = ComputeGaussian(i + 1);

                sampleWeights[i * 2 + 1] = weight;
                sampleWeights[i * 2 + 2] = weight;

                totalWeights += weight * 2;

                // To get the maximum amount of blurring from a limited number of
                // pixel shader samples, we take advantage of the bilinear filtering
                // hardware inside the texture fetch unit. If we position our texture
                // coordinates exactly halfway between two texels, the filtering unit
                // will average them for us, giving two samples for the price of one.
                // This allows us to step in units of two texels per sample, rather
                // than just one at a time. The 1.5 offset kicks things off by
                // positioning us nicely in between two texels.
                var sampleOffset = i * 2 + 1.5f;

                Vector2 delta = new Vector2(dx, dy) * sampleOffset;

                // Store texture coordinate offsets for the positive and negative taps.
                sampleOffsets[i * 2 + 1] = delta;
                sampleOffsets[i * 2 + 2] = -delta;
            }

            // Normalize the list of sample weightings, so they will always sum to one.
            for (var i = 0; i < sampleWeights.Length; i++)
            {
                sampleWeights[i] /= totalWeights;
            }

            // Tell the effect about our new filter settings.
            weightsParameter.SetValue(sampleWeights);
            offsetsParameter.SetValue(sampleOffsets);
        }

        /// <summary>
        /// Evaluates a single point on the gaussian falloff curve.
        /// Used for setting up the blur filter weightings.
        /// </summary>
        private float ComputeGaussian(float n)
        {
            var theta = Settings.BlurAmount;

            return (float)(1.0 / Math.Sqrt(2 * Math.PI * theta) *
                           Math.Exp(-(n * n) / (2 * theta * theta)));
        }
    }
}

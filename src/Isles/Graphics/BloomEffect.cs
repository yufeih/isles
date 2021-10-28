// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class BloomSettings
    {
        // Controls how bright a pixel needs to be before it will bloom.
        // Zero makes everything bloom equally, while higher values select
        // only brighter colors. Somewhere between 0.25 and 0.5 is good.
        public float Threshold { get; set; } = 0.25f;

        // Controls how much blurring is applied to the bloom image.
        // The typical range is from 1 up to 10 or so.
        public float Blur { get; set; } = 2;

        // Controls the amount of the bloom and base images that
        // will be mixed into the final scene. Range 0 to 1.
        public float BloomIntensity { get; set; } = 1;
        public float BaseIntensity { get; set; } = 1;

        // Independently control the color saturation of the bloom and
        // base images. Zero is totally desaturated, 1.0 leaves saturation
        // unchanged, while higher values increase the saturation level.
        public float BloomSaturation { get; set; } = 2;
        public float BaseSaturation { get; set; }

        public static BloomSettings Lerp(BloomSettings settings1, BloomSettings settings2, float amount)
        {
            var settings = new BloomSettings
            {
                BaseIntensity = MathHelper.Lerp(settings1.BaseIntensity, settings2.BaseIntensity, amount),
                BaseSaturation = MathHelper.Lerp(settings1.BaseSaturation, settings2.BaseSaturation, amount),
                BloomIntensity = MathHelper.Lerp(settings1.BloomIntensity, settings2.BloomIntensity, amount),
                BloomSaturation = MathHelper.Lerp(settings1.BloomSaturation, settings2.BloomSaturation, amount),
                Threshold = MathHelper.Lerp(settings1.Threshold, settings2.Threshold, amount),
                Blur = MathHelper.Lerp(settings1.Blur, settings2.Blur, amount),
            };

            return settings;
        }
    }

    public class BloomEffect
    {
        private readonly GraphicsDevice GraphicsDevice;

        private readonly SpriteBatch spriteBatch;
        private readonly Effect bloomExtractEffect;
        private readonly Effect bloomCombineEffect;
        private readonly Effect gaussianBlurEffect;
        private readonly RenderTarget2D resolveTarget;
        private readonly RenderTarget2D renderTarget1;
        private readonly RenderTarget2D renderTarget2;

        // Choose what display settings the bloom should use.
        public BloomSettings Settings { get; set; } = new();

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

        public BloomEffect(GraphicsDevice graphics, ShaderLoader shaderLoader)
        {
            GraphicsDevice = graphics;

            spriteBatch = new SpriteBatch(GraphicsDevice);

            bloomExtractEffect = shaderLoader.LoadShader("shaders/BloomExtract.cso");
            bloomCombineEffect = shaderLoader.LoadShader("shaders/BloomCombine.cso");
            gaussianBlurEffect = shaderLoader.LoadShader("shaders/GaussianBlur.cso");

            // Look up the resolution and format of our main backbuffer.
            PresentationParameters pp = GraphicsDevice.PresentationParameters;

            var width = pp.BackBufferWidth;
            var height = pp.BackBufferHeight;

            SurfaceFormat format = pp.BackBufferFormat;

            // Create a texture for reading back the backbuffer contents.
            resolveTarget = new RenderTarget2D(GraphicsDevice, width, height, 1,
                format);

            // Create two rendertargets for the bloom processing. These are half the
            // size of the backbuffer, in order to minimize fillrate costs. Reducing
            // the resolution in this way doesn't hurt quality, because we are going
            // to be blurring the bloom images in any case.
            width /= 2;
            height /= 2;

            renderTarget1 = new RenderTarget2D(GraphicsDevice, width, height, 1,
                format);
            renderTarget2 = new RenderTarget2D(GraphicsDevice, width, height, 1,
                format);
        }

        public void BeginDraw()
        {
            GraphicsDevice.SetRenderTarget(resolveTarget);
        }

        public void EndDraw()
        {
            // Resolve the scene into a texture, so we can
            // use it as input data for the bloom processing.
            GraphicsDevice.SetRenderTarget(null);

            // Pass 1: draw the scene into rendertarget 1, using a
            // shader that extracts only the brightest parts of the image.
            bloomExtractEffect.Parameters["BloomThreshold"].SetValue(
                Settings.Threshold);

            DrawFullscreenQuad(resolveTarget.GetTexture(), renderTarget1,
                               bloomExtractEffect,
                               IntermediateBuffer.PreBloom);

            // Pass 2: draw from rendertarget 1 into rendertarget 2,
            // using a shader to apply a horizontal gaussian blur filter.
            SetBlurEffectParameters(1.0f / renderTarget1.Width, 0);

            DrawFullscreenQuad(renderTarget1.GetTexture(), renderTarget2,
                               gaussianBlurEffect,
                               IntermediateBuffer.BlurredHorizontally);

            // Pass 3: draw from rendertarget 2 back into rendertarget 1,
            // using a shader to apply a vertical gaussian blur filter.
            SetBlurEffectParameters(0, 1.0f / renderTarget1.Height);

            DrawFullscreenQuad(renderTarget2.GetTexture(), renderTarget1,
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

            GraphicsDevice.Textures[1] = resolveTarget.GetTexture();

            Viewport viewport = GraphicsDevice.Viewport;

            DrawFullscreenQuad(renderTarget1.GetTexture(),
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
                effect.Begin();
                effect.CurrentTechnique.Passes[0].Begin();
            }

            // Draw the quad.
            spriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            spriteBatch.End();

            // End the custom effect.
            if (ShowBuffer >= currentBuffer)
            {
                effect.CurrentTechnique.Passes[0].End();
                effect.End();
            }
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
            var theta = Settings.Blur;

            return (float)(1.0 / Math.Sqrt(2 * Math.PI * theta) *
                           Math.Exp(-(n * n) / (2 * theta * theta)));
        }
    }
}

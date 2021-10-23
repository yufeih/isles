// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics
{
    public enum BlendState
    {
        Opaque,
        AlphaBlend,
        Additive,
    }

    public enum DepthStencilState
    {
        None,
        Default,
        DepthRead,
    }

    public enum RasterizerState
    {
        CullNone,
        CullCounterClockwise,
    }

    public static class Xna4Extensions
    {
        private static readonly Stack<(RenderTarget2D, DepthStencilBuffer)> _renderTargetStack = new();

        public static void SetRenderTarget(this GraphicsDevice graphicsDevice, RenderTarget2D renderTarget)
        {
            graphicsDevice.SetRenderTarget(0, renderTarget);
        }

        public static void PushRenderTarget(this GraphicsDevice graphicsDevice, RenderTarget2D renderTarget, DepthStencilBuffer depthStencilBuffer = null)
        {
            _renderTargetStack.Push(((RenderTarget2D)graphicsDevice.GetRenderTarget(0), graphicsDevice.DepthStencilBuffer));
            graphicsDevice.SetRenderTarget(0, renderTarget);
            graphicsDevice.DepthStencilBuffer = depthStencilBuffer;
        }

        public static void PopRenderTarget(this GraphicsDevice graphicsDevice)
        {
            if (_renderTargetStack.Count> 0)
            {
                var (renderTarget, depthStencilBuffer) = _renderTargetStack.Pop();
                graphicsDevice.SetRenderTarget(0, renderTarget);
                graphicsDevice.DepthStencilBuffer = depthStencilBuffer;
            }
        }

        public static void Begin(this SpriteBatch spriteBatch, SpriteSortMode spriteSortMode, BlendState blendState)
        {
            var blendMode = blendState switch
            {
                BlendState.AlphaBlend => SpriteBlendMode.AlphaBlend,
                BlendState.Additive => SpriteBlendMode.Additive,
                _ => SpriteBlendMode.None,
            };

            spriteBatch.Begin(blendMode, spriteSortMode, SaveStateMode.None);
        }

        public static void SetRenderState(
            this GraphicsDevice graphicsDevice,
            BlendState blendState = BlendState.AlphaBlend,
            DepthStencilState depthStencilState = DepthStencilState.Default,
            RasterizerState rasterizerState = RasterizerState.CullCounterClockwise)
        {
            graphicsDevice.SetBlendState(blendState);
            graphicsDevice.SetDepthStencilState(depthStencilState);
            graphicsDevice.SetRasterizerStateState(rasterizerState);
        }

        private static void SetBlendState(this GraphicsDevice graphicsDevice, BlendState blendState)
        {
            switch (blendState)
            {
                case BlendState.AlphaBlend:
                    graphicsDevice.RenderState.AlphaBlendEnable = true;
                    graphicsDevice.RenderState.SourceBlend = Blend.SourceColor;
                    graphicsDevice.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
                    graphicsDevice.RenderState.AlphaDestinationBlend = Blend.InverseSourceAlpha;
                    graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
                    graphicsDevice.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
                    break;

                case BlendState.Additive:
                    graphicsDevice.RenderState.AlphaBlendEnable = true;
                    graphicsDevice.RenderState.AlphaSourceBlend = Blend.SourceAlpha;
                    graphicsDevice.RenderState.AlphaDestinationBlend = Blend.One;
                    graphicsDevice.RenderState.SourceBlend = Blend.SourceAlpha;
                    graphicsDevice.RenderState.DestinationBlend = Blend.One;
                    break;

                case BlendState.Opaque:
                    graphicsDevice.RenderState.AlphaBlendEnable = false;
                    break;
            }
        }

        private static void SetDepthStencilState(this GraphicsDevice graphicsDevice, DepthStencilState depthStencilState)
        {
            switch (depthStencilState)
            {
                case DepthStencilState.None:
                    graphicsDevice.RenderState.DepthBufferEnable = false;
                    graphicsDevice.RenderState.DepthBufferWriteEnable = false;
                    break;

                case DepthStencilState.Default:
                    graphicsDevice.RenderState.DepthBufferEnable = true;
                    graphicsDevice.RenderState.DepthBufferWriteEnable = true;
                    break;

                case DepthStencilState.DepthRead:
                    graphicsDevice.RenderState.DepthBufferEnable = true;
                    graphicsDevice.RenderState.DepthBufferWriteEnable = false;
                    break;
            }
        }

        private static void SetRasterizerStateState(this GraphicsDevice graphicsDevice, RasterizerState rasterizerStateState)
        {
            switch (rasterizerStateState)
            {
                case RasterizerState.CullNone:
                    graphicsDevice.RenderState.CullMode = CullMode.None;
                    break;

                case RasterizerState.CullCounterClockwise:
                    graphicsDevice.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
                    break;
            }
        }
    }
}

// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xna.Framework.Graphics
{
    public enum BlendState
    {
        Opaque,
        AlphaBlend,
        Additive,
    }

    public static class Xna4Extensions
    {
        public static void SetRenderTarget(this GraphicsDevice graphicsDevice, RenderTarget2D renderTarget)
        {
            graphicsDevice.SetRenderTarget(0, renderTarget);
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

        public static void SetBlendState(this GraphicsDevice graphicsDevice, BlendState blendState)
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
    }
}

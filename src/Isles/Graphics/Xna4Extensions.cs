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
    }
}

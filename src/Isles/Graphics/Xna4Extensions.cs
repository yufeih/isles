// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Xna.Framework.Graphics
{
    public static class Xna4Extensions
    {
        private static readonly Stack<RenderTarget2D> _renderTargetStack = new(new RenderTarget2D[] { null });

        public static void PushRenderTarget(this GraphicsDevice graphicsDevice, RenderTarget2D renderTarget)
        {
            _renderTargetStack.Push(renderTarget);
            graphicsDevice.SetRenderTarget(renderTarget);
        }

        public static void PopRenderTarget(this GraphicsDevice graphicsDevice)
        {
            _renderTargetStack.Pop();
            graphicsDevice.SetRenderTarget(_renderTargetStack.Peek());
        }

        public static void SetBlendState(this GraphicsDevice graphicsDevice, BlendState blendState)
        {
            graphicsDevice.BlendState = blendState;
        }

        public static void SetDepthStencilState(this GraphicsDevice graphicsDevice, DepthStencilState depthStencilState)
        {
            graphicsDevice.DepthStencilState = depthStencilState;
        }

        public static void SetRasterizerStateState(this GraphicsDevice graphicsDevice, RasterizerState rasterizerState)
        {
            graphicsDevice.RasterizerState = rasterizerState;
        }
    }
}

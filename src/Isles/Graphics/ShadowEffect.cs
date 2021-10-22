// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Isles.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    /// <summary>
    /// Use shadow mapping technique to generate shadows.
    /// </summary>
    /// <example>
    /// Here is a code snippet demonstrating how to make use of this class:
    ///
    /// // Initialization
    /// ShadowEffect shadow = new ShadowEffect(game);
    ///
    /// // Draw
    /// if (shadow.Begin())
    /// {
    ///     // Draw all shadow casters using shadow map generation effect
    ///     shadow.End();
    ///
    ///     // Draw all shadow receivers using the shadow map generated in
    ///     // the previous step.
    /// }.
    /// </example>
    public class ShadowEffect : IDisposable
    {
        public const int ShadowMapSize = 1024;
        private readonly BaseGame game;
        private DepthStencilBuffer depthStencil;
        private RenderTarget2D renderTarget;

        /// <summary>
        /// Gets or sets the direction of the light that create the shadow.
        /// </summary>
        public Vector3 LightDirection
        {
            get => lightDirection;
            set => lightDirection = value;
        }

        private Vector3 lightDirection;

        /// <summary>
        /// Gets the view projection matrix used to draw the shadow map.
        /// </summary>
        public Matrix ViewProjection
        {
            get => viewProjection;
            set => viewProjection = value;
        }

        private Matrix viewProjection;

        /// <summary>
        /// Gets the texture generated by shadow mapping.
        /// </summary>
        public Texture2D ShadowMap { get; private set; }

        /// <summary>
        /// Gets the effect used to generate the shadow map.
        /// </summary>
        public Effect Effect { get; }

        /// <summary>
        /// Gets or sets the target bounds that will be shadowed.
        /// </summary>
        public BoundingSphere TargetBounds
        {
            get => targetBounds;
            set => targetBounds = value;
        }

        private BoundingSphere targetBounds = new(Vector3.Zero, 100);

        public ShadowEffect(BaseGame game)
        {
            this.game = game;

            // Init effect
            Effect = game.Content.Load<Effect>("Effects/ShadowMap");

            CreateRenderTarget();
        }

        private void CreateRenderTarget()
        {
            try
            {
                // Create a stencil buffer in case our screen is not large
                // enough to hold the render target.
                depthStencil = new DepthStencilBuffer(
                    game.GraphicsDevice, ShadowMapSize, ShadowMapSize,
                    game.GraphicsDevice.DepthStencilBuffer.Format);

                // Create textures
                renderTarget = new RenderTarget2D(
                    game.GraphicsDevice, ShadowMapSize, ShadowMapSize, 1, SurfaceFormat.Single);
            }
            catch (Exception e)
            {
                try
                {
                    e.ToString();
                    renderTarget = new RenderTarget2D(
                        game.GraphicsDevice, ShadowMapSize, ShadowMapSize, 1, SurfaceFormat.Color);
                }
                catch (Exception ex)
                {
                    // Some device may not support 32-bit floating point texture
                    Log.Write("Failed creating Shadow mapping effect: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Begins a shadow mapping generation process.
        /// </summary>
        public bool Begin()
        {
            // Return false if the shadow mapping effect is not initialized
            if (Effect == null || renderTarget == null || depthStencil == null)
            {
                return false;
            }

            if (renderTarget.IsDisposed || renderTarget.IsContentLost)
            {
                CreateRenderTarget();
            }

            // Set shadow mapping targets
            game.GraphicsDevice.PushRenderTarget(renderTarget, depthStencil);

            game.GraphicsDevice.Clear(Color.White);

            return true;
        }

        /// <summary>
        /// Ends shadow mapping generation and produce the generated shadow map.
        /// </summary>
        /// <returns>
        /// Shadow map created.
        /// </returns>
        public Texture2D End()
        {
            game.GraphicsDevice.PopRenderTarget();

            return ShadowMap = renderTarget.GetTexture();
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (depthStencil != null)
                {
                    depthStencil.Dispose();
                }

                if (renderTarget != null)
                {
                    renderTarget.Dispose();
                }

                if (ShadowMap != null)
                {
                    ShadowMap.Dispose();
                }

                if (Effect != null)
                {
                    Effect.Dispose();
                }
            }
        }
    }
}

// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Graphics
{
    public class ShadowEffect : IDisposable
    {
        private const int ShadowMapSize = 2048;

        /// <summary>
        /// Constant values use for shadow matrix calculation.
        /// </summary>
        private static readonly float[] s_shadowMatrixDistance = new[] { 245.0f, 734.0f, 1225.0f };
        private static readonly float[] s_shadowMatrixNear = new[] { 10.0f, 10.0f, 10.0f };
        private static readonly float[] s_shadowMatrixFar = new[] { 500.0f, 1022.0f, 1614.0f };

        private readonly GraphicsDevice _graphics;
        private readonly Effect _effect;
        private readonly DepthStencilBuffer _depthStencil;
        private readonly RenderTarget2D _renderTarget;

        public Matrix LightViewProjection { get; private set; }

        public Texture2D ShadowMap { get; private set; }

        public ShadowEffect(GraphicsDevice graphics, ShaderLoader shaderLoader)
        {
            _graphics = graphics;
            _effect = shaderLoader.LoadShader("shaders/ShadowMap.cso");
            _depthStencil = new DepthStencilBuffer(_graphics, ShadowMapSize, ShadowMapSize, _graphics.DepthStencilBuffer.Format);
            _renderTarget = new RenderTarget2D(_graphics, ShadowMapSize, ShadowMapSize, 1, SurfaceFormat.Single);
        }

        public void Begin(Vector3 eye, Vector3 facing)
        {
            LightViewProjection = CalculateShadowMatrix(eye, facing);

            _graphics.PushRenderTarget(_renderTarget, _depthStencil);
            _graphics.Clear(Color.White);
        }

        public void End()
        {
            _graphics.PopRenderTarget();
            ShadowMap = _renderTarget.GetTexture();
        }

        public void Dispose()
        {
            _depthStencil.Dispose();
            _renderTarget.Dispose();
            _effect.Dispose();
        }

        private static Matrix CalculateShadowMatrix(Vector3 eye, Vector3 facing)
        {
            // This is a little tricky, I never want to look into it again...
            //
            // These values are found out through experiments,
            // they might be the most suitable values for our scene.
            //
            // { Distance, Near, Far }
            // { 245.0f, 50, 500 }
            // { 734.0f, 300, 1022 }
            // { 1225.0f, 766, 1614 }
            //
            // Adjust light view and projection matrix based on current
            // camera position.
            var eyeDistance = -eye.Z / facing.Z;
            Vector3 target = eye + facing * eyeDistance;

            // Make it closer to the eye
            const float ClosenessToEye = 0.1f;
            target.X = eye.X * ClosenessToEye + target.X * (1 - ClosenessToEye);
            target.Y = eye.Y * ClosenessToEye + target.Y * (1 - ClosenessToEye);

            // Compute shadow area size based on eye distance
            const float MinDistance = 250.0f;
            const float MaxDistance = 1200.0f;
            const float MaxEyeDistance = 2000.0f;
            var distance = MathHelper.Lerp(MinDistance, MaxDistance, eyeDistance / MaxEyeDistance);

            // We only have two lines to lerp
            var index = distance > s_shadowMatrixDistance[1] ? 1 : 0;
            var amount = (distance - s_shadowMatrixDistance[index]) /
                           (s_shadowMatrixDistance[index + 1] - s_shadowMatrixDistance[index]);
            var near = MathHelper.Lerp(s_shadowMatrixNear[index], s_shadowMatrixNear[index + 1], amount);
            var far = MathHelper.Lerp(s_shadowMatrixFar[index], s_shadowMatrixFar[index + 1], amount);

            if (near < 1)
            {
                near = 1;
            }

            if (far < 1)
            {
                far = 1;
            }

            var lightDirection = Vector3.Normalize(new Vector3(1, 1, -2));
            var view = Matrix.CreateLookAt(target - lightDirection * (distance + 50), target, Vector3.UnitZ);
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.Pi * 0.3f, 1, near, far);

            return view * projection;
        }
    }
}

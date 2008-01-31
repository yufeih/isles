//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    public partial class Landscape
    {
        TextureCube skyTexture;
        Effect skyEffect;
        Model skyModel;

        void ReadSkyContent(ContentReader input)
        {
            skyEffect = input.ContentManager.Load<Effect>("Effects/Sky");
            skyModel = input.ContentManager.Load<Model>("Models/Cube");
            skyTexture = input.ReadExternalReference<TextureCube>();
        }

        void InitializeSky()
        {
        }

        void DrawSky(GameTime gameTime)
        {
            // Don't use or write to the z buffer
            graphics.RenderState.DepthBufferEnable = false;
            graphics.RenderState.DepthBufferWriteEnable = false;
            graphics.RenderState.CullMode = CullMode.None;

            // Also don't use any kind of blending.
            graphics.RenderState.AlphaBlendEnable = false;

            skyEffect.Parameters["CubeTexture"].SetValue(skyTexture);
            skyEffect.Parameters["View"].SetValue(game.View);
            skyEffect.Parameters["Projection"].SetValue(game.Projection);

            // Override model's effect and render
            skyModel.Meshes[0].MeshParts[0].Effect = skyEffect;
            skyModel.Meshes[0].Draw();

            // Reset previous render states
            graphics.RenderState.DepthBufferEnable = true;
            graphics.RenderState.DepthBufferWriteEnable = true;
        }
        
        /// <summary>
        /// Dispose
        /// </summary>
        void DisposeSky()
        {
            if (skyTexture != null)
                skyTexture.Dispose();
            if (skyEffect != null)
                skyEffect.Dispose();
        }
    }
}

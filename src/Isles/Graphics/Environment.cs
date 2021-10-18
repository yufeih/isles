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
    /// <summary>
    /// Graphics Atmosphere Settings
    /// </summary>
    public class Atmosphere
    {
        public Vector3 LightPosition;
        public Vector3 LightDirection;
        public Vector4 LightColor;
        public Vector4 AmbientColor;

        /// <summary>
        /// Creates a new environment
        /// </summary>
        public Atmosphere() { }

        /// <summary>
        /// Creates a new environment with some initial values
        /// </summary>
        public Atmosphere(
            Vector3 lightPosition,
            Vector3 lightDirection,
            Vector4 lightColor,
            Vector4 ambient)
        {
            this.LightPosition = lightPosition;
            this.LightDirection = lightDirection;
            this.LightColor = lightColor;
            this.AmbientColor = ambient;
        }

        /// <summary>
        /// Blend between environments.
        /// This method creates smooth transitions between different environment settings.
        /// </summary>
        /// <param name="env"></param>
        /// <param name="lerpAmount"></param>
        public static Atmosphere Blend(Atmosphere env1, Atmosphere env2, float lerpAmount)
        {
            Atmosphere env = new Atmosphere();

            // Clamp lerp amount
            lerpAmount = MathHelper.Clamp(lerpAmount, 0.0f, 1.0f);

            // Lerp individual entries
            env.AmbientColor = Vector4.Lerp(env1.AmbientColor, env2.AmbientColor, lerpAmount);
            env.LightColor = Vector4.Lerp(env1.LightColor, env2.LightColor, lerpAmount);
            env.LightDirection = Vector3.Lerp(env1.LightDirection, env2.LightDirection, lerpAmount);
            env.LightPosition = Vector3.Lerp(env1.LightPosition, env2.LightPosition, lerpAmount);

            return env;
        }

        /// <summary>
        /// Pre-defined environments
        /// </summary>
        public static Atmosphere Day
        {
            get { return new Atmosphere(); }
        }

        public static Atmosphere Night
        {
            get { return new Atmosphere(); }
        }

        public static Atmosphere Storm
        {
            get { return new Atmosphere(); }
        }
    }
}

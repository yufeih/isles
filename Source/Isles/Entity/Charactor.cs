//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;
namespace Isles
{
    /// <summary>
    /// Base class for all game charactors
    /// </summary>
    public class Charactor : BaseAgent
    {
        public Charactor(GameWorld world, string classID) : base(world)
        {
            XmlElement xml;

            if (GameDefault.Singleton.WorldObjectDefaults.TryGetValue(classID, out xml))
            {
                Deserialize(xml);
            }
        }

        public override bool VisibilityTest(Matrix viewProjection)
        {
            return true;
        }
    }
}

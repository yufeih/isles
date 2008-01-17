//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;

namespace Isles.Graphics
{
    public class LandscapeReader : ContentTypeReader<Landscape>
    {
        /// <summary>
        /// Content loader for loading Landscape object from XNB file
        /// </summary>
        protected override Landscape Read(ContentReader input, Landscape existingInstance)
        {
            return new Landscape(input);
        }
    }
}

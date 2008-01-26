//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;

namespace Isles
{
    public class SpellPlantTree : Spell
    {
        public SpellPlantTree(GameWorld world)
            : base(world)
        {
        }

        public override string Name
        {
            get { return "Plant a tree"; }
        }

        public override string Description
        {
            get { return "Plant a tree"; }
        }

        public override Keys Hotkey
        {
            get { return Keys.T; }
        }

        public override bool Trigger(Hand hand)
        {
            if (hand.StopActions())
            {
                hand.Drag(world.Create("Tree") as Entity);
            }
            return false;
        }
    }
}

//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Isles;
using Isles.Engine;
using Isles.Graphics;

namespace Isles.Editor
{
    public partial class ShadowEditor : Form
    {
        GameWorld world;        

        public ShadowEditor(GameWorld gameWorld)
        {
            world = gameWorld;

            InitializeComponent();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            world.Distance = (float)trackBar1.Value;
            label1.Text = "Distance " + trackBar1.Value;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            world.Near = (float)trackBar2.Value;
            label2.Text = "Near " + trackBar2.Value;
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            world.Far = (float)trackBar3.Value;
            label3.Text = "Far " + trackBar3.Value;
        }
    }
}
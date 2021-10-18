using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Isles.Engine;

namespace Isles.Editor
{
    public partial class WorldEditor : Form
    {
        GameScreen screen;

        public WorldEditor(GameScreen screen)
        {
            this.screen = screen;

            InitializeComponent();

            // Add object types
            foreach (KeyValuePair<string, GameWorld.Creator> pair in GameWorld.Creators)
            {
                objectList.Items.Add(pair.Key);
            }
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (Stream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    XmlDocument doc = new XmlDocument();
                    screen.World.Save(doc, null);
                    doc.Save(stream);
                }
            }
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                screen.StartLevel(openFileDialog.FileName);
            }
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (Player.LocalPlayer != null)
            {
                foreach (GameObject o in Player.LocalPlayer.Selected)
                    screen.World.Destroy(o);
            }
        }

        private void buttonCreate_Click(object sender, EventArgs e)
        {
            string type = objectList.SelectedItem as string;

            if (type != null)
            {
                Spell.EndSpell();
                SpellConstruct construct = new SpellConstruct(screen.World, type);
                construct.AutoReactivate = true;
                Spell.Cast(construct);                
            }
        }

        SpellDrawPathOcculder drawPathOccluders;

        private void buttonDrawPath_Click(object sender, EventArgs e)
        {
            if (drawPathOccluders == null)
            {
                drawPathOccluders = new SpellDrawPathOcculder(screen.World);
                trackBarPathBrushSize_Scroll(null, null);
            }

            Spell.EndSpell();
            Spell.Cast(drawPathOccluders);
        }

        private void buttonSavePath_Click(object sender, EventArgs e)
        {
            if (drawPathOccluders != null && saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (Stream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    drawPathOccluders.Save(stream);
                }
            }
        }

        private void trackBarPathBrushSize_Scroll(object sender, EventArgs e)
        {
            if (drawPathOccluders != null)
                drawPathOccluders.BrushRadius = 2.5f * trackBarPathBrushSize.Value;
        }
    }
}
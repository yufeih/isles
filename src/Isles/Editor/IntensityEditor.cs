using System;
using System.Windows.Forms;
using Isles.Engine;

namespace Isles.Editor
{
    public partial class IntensityEditor : Form
    {
        private readonly IWorldObject o;

        public IntensityEditor(GameWorld world)
        {
            InitializeComponent();

            o = world.ObjectFromName("Ruined");
        }

        private void IntensityEditor_Load(object sender, EventArgs e)
        {
        }

        private void TrackBar_Scroll(object sender, EventArgs e)
        {
            o.Position = new Microsoft.Xna.Framework.Vector3(1024, 1024,
                400.0f * trackBar1.Value / 1000);
        }
    }
}
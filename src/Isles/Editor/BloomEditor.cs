//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using Isles.Graphics;

namespace Isles.Editor
{
    public partial class BloomEditor : Form
    {
        private readonly BloomEffect bloom;

        public BloomEditor(BloomEffect bloom)
        {
            this.bloom = bloom ?? throw new ArgumentNullException();
            InitializeComponent();

            // Initialize control values
            trackBarBaseIntensity.Value = (int)(bloom.Settings.BaseIntensity / 0.05f);
            trackBarBaseSaturation.Value = (int)(bloom.Settings.BaseSaturation / 0.02f);
            trackBarBlur.Value = (int)(bloom.Settings.BlurAmount / 0.05f);
            trackBarIntensity.Value = (int)(bloom.Settings.BloomIntensity / 0.05f);
            trackBarSaturation.Value = (int)(bloom.Settings.BloomSaturation / 0.02f);
            trackBarThreshold.Value = (int)(bloom.Settings.BloomThreshold / 0.01f);
        }

        private void trackBarBlur_Scroll(object sender, EventArgs e)
        {
            bloom.Settings.BlurAmount = trackBarBlur.Value * 0.05f;
        }

        private void trackBarThreshold_Scroll(object sender, EventArgs e)
        {
            bloom.Settings.BloomThreshold = trackBarThreshold.Value * 0.01f;
        }

        private void trackBarIntensity_Scroll(object sender, EventArgs e)
        {
            bloom.Settings.BloomIntensity = trackBarIntensity.Value * 0.05f;
        }

        private void trackBarBaseIntensity_Scroll(object sender, EventArgs e)
        {
            bloom.Settings.BaseIntensity = trackBarBaseIntensity.Value * 0.05f;
        }

        private void trackBarSaturation_Scroll(object sender, EventArgs e)
        {
            bloom.Settings.BloomSaturation = trackBarSaturation.Value * 0.02f;
        }

        private void trackBarBaseSaturation_Scroll(object sender, EventArgs e)
        {
            bloom.Settings.BaseSaturation = trackBarBaseSaturation.Value * 0.02f;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Open
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var file = new FileStream(openFileDialog.FileName, FileMode.Open))
                {
                    bloom.Settings = (BloomSettings)
                        new XmlSerializer(typeof(BloomSettings)).Deserialize(file);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Save
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var file = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate))
                {
                    new XmlSerializer(typeof(BloomSettings)).Serialize(file, bloom.Settings);
                }
            }
        }
    }
}
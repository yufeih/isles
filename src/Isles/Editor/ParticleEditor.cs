using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
using Isles.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace Isles.Editor
{
    public partial class ParticleEditor : Form
    {
        private readonly ParticleEffect particleEffect;

        public ParticleEditor(ParticleEffect particleEffect)
        {
            InitializeComponent();
            this.particleEffect = particleEffect ?? throw new ArgumentNullException();

            InitializeSettings(particleEffect.Emission, particleEffect.Particle);

            InitializeTextures();
        }

        private const string TexturePath = "Content/Textures/Effect";
        private const string TextureLoadPath = "Textures/Effect/";

        private void InitializeTextures()
        {
            foreach (var file in Directory.GetFiles(TexturePath))
            {
                comboBoxTexture.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private void InitializeSettings(float emission, ParticleSystem particle)
        {
            // Initialize settings
            trackBarEmission.Value = (int)(emission / 4);
            trackBarDuration.Value = (int)(particle.Settings.Duration * 20);
            trackBarDurationRandomness.Value = (int)(particle.Settings.DurationRandomness * 50);
            trackBarEndVelocity.Value = (int)(particle.Settings.EndVelocity * 20);
            trackBarGravity.Value = (int)(-particle.Settings.Gravity.Z * 10);
            trackBarMaxEndSize.Value = (int)particle.Settings.MaxEndSize;
            trackBarMaxHVelocity.Value = (int)(particle.Settings.MaxHorizontalVelocity * 5);
            trackBarMaxRotationSpeed.Value = (int)(particle.Settings.MaxRotateSpeed * 100);
            trackBarMaxStartSize.Value = (int)particle.Settings.MaxStartSize;
            trackBarMaxVVelocity.Value = (int)(particle.Settings.MaxVerticalVelocity * 5);
            trackBarMinEndSize.Value = (int)particle.Settings.MinEndSize;
            trackBarMinHVelocity.Value = (int)(particle.Settings.MinHorizontalVelocity * 5);
            trackBarMinRotationSpeed.Value = (int)(particle.Settings.MinRotateSpeed * 100);
            trackBarMinStartSize.Value = (int)particle.Settings.MinStartSize;
            trackBarMinVVelocity.Value = (int)(particle.Settings.MinVerticalVelocity * 5);
            trackBarSensitivity.Value = (int)(particle.Settings.EmitterVelocitySensitivity * 20);
        }

        private void trackBarDuration_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.Duration = trackBarDuration.Value / 20.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarDurationRandomness_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.DurationRandomness = trackBarDurationRandomness.Value / 50.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarSensitivity_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.EmitterVelocitySensitivity = trackBarSensitivity.Value / 20.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMaxHVelocity_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MaxHorizontalVelocity = trackBarMaxHVelocity.Value / 5.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMinHVelocity_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MinHorizontalVelocity = trackBarMinHVelocity.Value / 5.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMaxVVelocity_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MaxVerticalVelocity = trackBarMaxVVelocity.Value / 5.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMinVVelocity_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MinVerticalVelocity = trackBarMinVVelocity.Value / 5.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarEndVelocity_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.EndVelocity = trackBarEndVelocity.Value / 20.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarGravity_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.Gravity = new Microsoft.Xna.Framework.Vector3(
                0, 0, -trackBarGravity.Value / 10.0f);
            particleEffect.Particle.Refresh();
        }

        private void trackBarMaxRotationSpeed_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MaxRotateSpeed = trackBarMaxRotationSpeed.Value / 100.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMaxStartSize_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MaxStartSize = trackBarMaxStartSize.Value;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMinRotationSpeed_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MinRotateSpeed = trackBarMinRotationSpeed.Value / 100.0f;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMinStartSize_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MinStartSize = trackBarMinStartSize.Value;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMaxEndSize_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MaxEndSize = trackBarMaxStartSize.Value;
            particleEffect.Particle.Refresh();
        }

        private void trackBarMinEndSize_Scroll(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.MinEndSize = trackBarMinEndSize.Value;
            particleEffect.Particle.Refresh();
        }

        private void trackBarEmission_Scroll(object sender, EventArgs e)
        {
            particleEffect.Emission = trackBarEmission.Value * 4;
        }

        private void cmbParticle_SelectedIndexChanged(object sender, EventArgs e)
        {
            // this.InitializeParticleSettings(this.particles[int.Parse(this.cmbParticle.SelectedValue.ToString())]);
            // this.particleEffect.Particle = this.particles[int.Parse(this.cmbParticle.SelectedValue.ToString())];
        }

        private void cmbEmitter_SelectedIndexChanged(object sender, EventArgs e)
        {
            // this.InitializeEmitterSettings(this.emitters[int.Parse(this.cmbEmitter.SelectedValue.ToString())]);
            // this.SelectedEmitter = this.emitters[int.Parse(this.cmbEmitter.SelectedValue.ToString())];
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (Stream stream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                {
                    new XmlSerializer(typeof(ParticleSettings)).Serialize(stream, particleEffect.Particle.Settings);
                }

                MessageBox.Show("Emission: " + particleEffect.Emission);
            }
        }

        private void buttonMaxColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                particleEffect.Particle.Settings.MaxColor =
                    new Color(
                    colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                particleEffect.Particle.Refresh();
            }
        }

        private void buttonMinColor_Click(object sender, EventArgs e)
        {
            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                particleEffect.Particle.Settings.MinColor =
                    new Color(
                    colorDialog.Color.R, colorDialog.Color.G, colorDialog.Color.B);
                particleEffect.Particle.Refresh();
            }
        }

        private void comboBoxSourceBlend_SelectedIndexChanged(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.SourceBlend = (Blend)Enum.Parse(typeof(Blend), comboBoxSourceBlend.SelectedItem.ToString());
            particleEffect.Particle.Refresh();
        }

        private void comboBoxDestBlend_SelectedIndexChanged(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.DestinationBlend = (Blend)Enum.Parse(typeof(Blend), comboBoxDestBlend.SelectedItem.ToString());
            particleEffect.Particle.Refresh();
        }

        private void comboBoxTexture_SelectedIndexChanged(object sender, EventArgs e)
        {
            particleEffect.Particle.Settings.TextureName = TextureLoadPath + comboBoxTexture.SelectedItem.ToString();
            particleEffect.Particle.Refresh();
        }
    }
}
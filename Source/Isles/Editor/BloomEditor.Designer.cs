namespace Isles.Editor
{
    partial class BloomEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.trackBarBlur = new System.Windows.Forms.TrackBar();
            this.trackBarThreshold = new System.Windows.Forms.TrackBar();
            this.trackBarIntensity = new System.Windows.Forms.TrackBar();
            this.trackBarBaseIntensity = new System.Windows.Forms.TrackBar();
            this.trackBarSaturation = new System.Windows.Forms.TrackBar();
            this.trackBarBaseSaturation = new System.Windows.Forms.TrackBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBlur)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBaseIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarSaturation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBaseSaturation)).BeginInit();
            this.SuspendLayout();
            // 
            // trackBarBlur
            // 
            this.trackBarBlur.Location = new System.Drawing.Point(144, 12);
            this.trackBarBlur.Maximum = 100;
            this.trackBarBlur.Name = "trackBarBlur";
            this.trackBarBlur.Size = new System.Drawing.Size(151, 45);
            this.trackBarBlur.TabIndex = 2;
            this.trackBarBlur.TickFrequency = 5;
            this.trackBarBlur.Scroll += new System.EventHandler(this.trackBarBlur_Scroll);
            // 
            // trackBarThreshold
            // 
            this.trackBarThreshold.Location = new System.Drawing.Point(144, 63);
            this.trackBarThreshold.Maximum = 100;
            this.trackBarThreshold.Name = "trackBarThreshold";
            this.trackBarThreshold.Size = new System.Drawing.Size(151, 45);
            this.trackBarThreshold.TabIndex = 3;
            this.trackBarThreshold.TickFrequency = 5;
            this.trackBarThreshold.Scroll += new System.EventHandler(this.trackBarThreshold_Scroll);
            // 
            // trackBarIntensity
            // 
            this.trackBarIntensity.Location = new System.Drawing.Point(144, 114);
            this.trackBarIntensity.Maximum = 100;
            this.trackBarIntensity.Name = "trackBarIntensity";
            this.trackBarIntensity.Size = new System.Drawing.Size(151, 45);
            this.trackBarIntensity.TabIndex = 4;
            this.trackBarIntensity.TickFrequency = 5;
            this.trackBarIntensity.Scroll += new System.EventHandler(this.trackBarIntensity_Scroll);
            // 
            // trackBarBaseIntensity
            // 
            this.trackBarBaseIntensity.Location = new System.Drawing.Point(144, 165);
            this.trackBarBaseIntensity.Maximum = 100;
            this.trackBarBaseIntensity.Name = "trackBarBaseIntensity";
            this.trackBarBaseIntensity.Size = new System.Drawing.Size(151, 45);
            this.trackBarBaseIntensity.TabIndex = 5;
            this.trackBarBaseIntensity.TickFrequency = 5;
            this.trackBarBaseIntensity.Scroll += new System.EventHandler(this.trackBarBaseIntensity_Scroll);
            // 
            // trackBarSaturation
            // 
            this.trackBarSaturation.Location = new System.Drawing.Point(144, 216);
            this.trackBarSaturation.Maximum = 100;
            this.trackBarSaturation.Name = "trackBarSaturation";
            this.trackBarSaturation.Size = new System.Drawing.Size(151, 45);
            this.trackBarSaturation.TabIndex = 6;
            this.trackBarSaturation.TickFrequency = 5;
            this.trackBarSaturation.Scroll += new System.EventHandler(this.trackBarSaturation_Scroll);
            // 
            // trackBarBaseSaturation
            // 
            this.trackBarBaseSaturation.Location = new System.Drawing.Point(144, 267);
            this.trackBarBaseSaturation.Maximum = 100;
            this.trackBarBaseSaturation.Name = "trackBarBaseSaturation";
            this.trackBarBaseSaturation.Size = new System.Drawing.Size(151, 45);
            this.trackBarBaseSaturation.TabIndex = 7;
            this.trackBarBaseSaturation.TickFrequency = 5;
            this.trackBarBaseSaturation.Scroll += new System.EventHandler(this.trackBarBaseSaturation_Scroll);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 17);
            this.label2.TabIndex = 8;
            this.label2.Text = "Blur";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 17);
            this.label3.TabIndex = 9;
            this.label3.Text = "Threshold";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 114);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(98, 17);
            this.label4.TabIndex = 10;
            this.label4.Text = "Bloom Intensity";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 165);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(88, 17);
            this.label5.TabIndex = 11;
            this.label5.Text = "Base Intensity";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 216);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(109, 17);
            this.label6.TabIndex = 12;
            this.label6.Text = "Bloom Saturation";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 267);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(99, 17);
            this.label7.TabIndex = 13;
            this.label7.Text = "Base Saturation";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(139, 318);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 14;
            this.button1.Text = "Load";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(220, 318);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 15;
            this.button2.Text = "Save";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "xml";
            this.openFileDialog.FileName = "BloomSettings";
            this.openFileDialog.Filter = "XML files|*.xml|All files|*.*";
            this.openFileDialog.Title = "Open Bloom Settings...";
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.Filter = "XML files|*.xml|All files|*.*";
            this.saveFileDialog.Title = "Save Bloom Settings...";
            // 
            // BloomEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(319, 353);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.trackBarBaseSaturation);
            this.Controls.Add(this.trackBarSaturation);
            this.Controls.Add(this.trackBarBaseIntensity);
            this.Controls.Add(this.trackBarIntensity);
            this.Controls.Add(this.trackBarThreshold);
            this.Controls.Add(this.trackBarBlur);
            this.Name = "BloomEditor";
            this.Text = "Bloom Editor";
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBlur)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarIntensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBaseIntensity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarSaturation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarBaseSaturation)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar trackBarBlur;
        private System.Windows.Forms.TrackBar trackBarThreshold;
        private System.Windows.Forms.TrackBar trackBarIntensity;
        private System.Windows.Forms.TrackBar trackBarBaseIntensity;
        private System.Windows.Forms.TrackBar trackBarSaturation;
        private System.Windows.Forms.TrackBar trackBarBaseSaturation;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
    }
}
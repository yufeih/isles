namespace Isles.Editor
{
    partial class WorldEditor
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



        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.objectList = new System.Windows.Forms.ListBox();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.buttonCreate = new System.Windows.Forms.Button();
            this.buttonSave = new System.Windows.Forms.Button();
            this.buttonLoad = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.buttonDrawPath = new System.Windows.Forms.Button();
            this.trackBarPathBrushSize = new System.Windows.Forms.TrackBar();
            this.buttonSavePath = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarPathBrushSize)).BeginInit();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(133, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Create a new object...";
            //
            // objectList
            //
            this.objectList.FormattingEnabled = true;
            this.objectList.ItemHeight = 17;
            this.objectList.Location = new System.Drawing.Point(12, 46);
            this.objectList.Name = "objectList";
            this.objectList.Size = new System.Drawing.Size(274, 395);
            this.objectList.TabIndex = 1;
            //
            // buttonDelete
            //
            this.buttonDelete.Location = new System.Drawing.Point(292, 77);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(108, 25);
            this.buttonDelete.TabIndex = 2;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
            //
            // buttonCreate
            //
            this.buttonCreate.Location = new System.Drawing.Point(292, 46);
            this.buttonCreate.Name = "buttonCreate";
            this.buttonCreate.Size = new System.Drawing.Size(108, 25);
            this.buttonCreate.TabIndex = 3;
            this.buttonCreate.Text = "Create";
            this.buttonCreate.UseVisualStyleBackColor = true;
            this.buttonCreate.Click += new System.EventHandler(this.buttonCreate_Click);
            //
            // buttonSave
            //
            this.buttonSave.Location = new System.Drawing.Point(292, 385);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(108, 25);
            this.buttonSave.TabIndex = 4;
            this.buttonSave.Text = "Save";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
            //
            // buttonLoad
            //
            this.buttonLoad.Location = new System.Drawing.Point(292, 416);
            this.buttonLoad.Name = "buttonLoad";
            this.buttonLoad.Size = new System.Drawing.Size(108, 25);
            this.buttonLoad.TabIndex = 5;
            this.buttonLoad.Text = "Load";
            this.buttonLoad.UseVisualStyleBackColor = true;
            this.buttonLoad.Click += new System.EventHandler(this.buttonLoad_Click);
            //
            // openFileDialog
            //
            this.openFileDialog.Filter = "Xml files|*.xml|All files|*.*";
            //
            // saveFileDialog
            //
            this.saveFileDialog.Filter = "Xml files|*.xml|All files|*.*";
            //
            // buttonDrawPath
            //
            this.buttonDrawPath.Location = new System.Drawing.Point(290, 254);
            this.buttonDrawPath.Name = "buttonDrawPath";
            this.buttonDrawPath.Size = new System.Drawing.Size(108, 25);
            this.buttonDrawPath.TabIndex = 6;
            this.buttonDrawPath.Text = "DrawPath";
            this.buttonDrawPath.UseVisualStyleBackColor = true;
            this.buttonDrawPath.Click += new System.EventHandler(this.buttonDrawPath_Click);
            //
            // trackBarPathBrushSize
            //
            this.trackBarPathBrushSize.Location = new System.Drawing.Point(294, 203);
            this.trackBarPathBrushSize.Minimum = 1;
            this.trackBarPathBrushSize.Name = "trackBarPathBrushSize";
            this.trackBarPathBrushSize.Size = new System.Drawing.Size(104, 45);
            this.trackBarPathBrushSize.TabIndex = 7;
            this.trackBarPathBrushSize.Value = 1;
            this.trackBarPathBrushSize.Scroll += new System.EventHandler(this.trackBarPathBrushSize_Scroll);
            //
            // buttonSavePath
            //
            this.buttonSavePath.Location = new System.Drawing.Point(290, 285);
            this.buttonSavePath.Name = "buttonSavePath";
            this.buttonSavePath.Size = new System.Drawing.Size(108, 25);
            this.buttonSavePath.TabIndex = 8;
            this.buttonSavePath.Text = "SavePath";
            this.buttonSavePath.UseVisualStyleBackColor = true;
            this.buttonSavePath.Click += new System.EventHandler(this.buttonSavePath_Click);
            //
            // WorldEditor
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(410, 450);
            this.Controls.Add(this.buttonSavePath);
            this.Controls.Add(this.trackBarPathBrushSize);
            this.Controls.Add(this.buttonDrawPath);
            this.Controls.Add(this.buttonLoad);
            this.Controls.Add(this.buttonSave);
            this.Controls.Add(this.buttonCreate);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.objectList);
            this.Controls.Add(this.label1);
            this.Name = "WorldEditor";
            this.Text = "WorldEditor";
            ((System.ComponentModel.ISupportInitialize)(this.trackBarPathBrushSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }



        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox objectList;
        private System.Windows.Forms.Button buttonDelete;
        private System.Windows.Forms.Button buttonCreate;
        private System.Windows.Forms.Button buttonSave;
        private System.Windows.Forms.Button buttonLoad;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.Button buttonDrawPath;
        private System.Windows.Forms.TrackBar trackBarPathBrushSize;
        private System.Windows.Forms.Button buttonSavePath;
    }
}
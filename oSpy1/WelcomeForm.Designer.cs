namespace oSpy
{
    partial class WelcomeForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose (bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose ();
            }
            base.Dispose (disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent ()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (WelcomeForm));
            this.showOnStartupCheckBox = new System.Windows.Forms.CheckBox ();
            this.button1 = new System.Windows.Forms.Button ();
            this.donateButton = new System.Windows.Forms.Button ();
            this.label2 = new System.Windows.Forms.Label ();
            this.logoPictureBox = new System.Windows.Forms.PictureBox ();
            this.versionLabel = new System.Windows.Forms.Label ();
            this.copyrightLabel = new System.Windows.Forms.Label ();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit ();
            this.SuspendLayout ();
            // 
            // showOnStartupCheckBox
            // 
            this.showOnStartupCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.showOnStartupCheckBox.AutoSize = true;
            this.showOnStartupCheckBox.Checked = true;
            this.showOnStartupCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.showOnStartupCheckBox.Location = new System.Drawing.Point (12, 320);
            this.showOnStartupCheckBox.Name = "showOnStartupCheckBox";
            this.showOnStartupCheckBox.Size = new System.Drawing.Size (153, 17);
            this.showOnStartupCheckBox.TabIndex = 1;
            this.showOnStartupCheckBox.Text = "&Show this dialog on startup";
            this.showOnStartupCheckBox.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button1.Location = new System.Drawing.Point (293, 314);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size (70, 26);
            this.button1.TabIndex = 2;
            this.button1.Text = "&Close";
            this.button1.UseVisualStyleBackColor = false;
            // 
            // donateButton
            // 
            this.donateButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.donateButton.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.donateButton.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.donateButton.Image = global::oSpy.Properties.Resources.DonateImg;
            this.donateButton.Location = new System.Drawing.Point (241, 227);
            this.donateButton.Name = "donateButton";
            this.donateButton.Size = new System.Drawing.Size (122, 64);
            this.donateButton.TabIndex = 3;
            this.donateButton.UseVisualStyleBackColor = false;
            this.donateButton.Click += new System.EventHandler (this.donateButton_Click);
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.label2.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point (9, 227);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size (226, 64);
            this.label2.TabIndex = 4;
            this.label2.Text = "Please consider donating a small sum to help pay for the development. Anything yo" +
                "u can provide is appreciated. Thanks!";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.logoPictureBox.Image = ((System.Drawing.Image)(resources.GetObject ("logoPictureBox.Image")));
            this.logoPictureBox.Location = new System.Drawing.Point (12, 12);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size (351, 171);
            this.logoPictureBox.TabIndex = 14;
            this.logoPictureBox.TabStop = false;
            // 
            // versionLabel
            // 
            this.versionLabel.AutoSize = true;
            this.versionLabel.Location = new System.Drawing.Point (292, 170);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size (31, 13);
            this.versionLabel.TabIndex = 15;
            this.versionLabel.Text = "1.6.0";
            // 
            // copyrightLabel
            // 
            this.copyrightLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.copyrightLabel.AutoSize = true;
            this.copyrightLabel.Location = new System.Drawing.Point (15, 195);
            this.copyrightLabel.Margin = new System.Windows.Forms.Padding (6, 0, 3, 0);
            this.copyrightLabel.MaximumSize = new System.Drawing.Size (0, 17);
            this.copyrightLabel.Name = "copyrightLabel";
            this.copyrightLabel.Size = new System.Drawing.Size (339, 13);
            this.copyrightLabel.TabIndex = 23;
            this.copyrightLabel.Text = "Copyright © 2006-2009 Ole André Vadla Ravnås <oleavr@gmail.com>";
            // 
            // WelcomeForm
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.button1;
            this.ClientSize = new System.Drawing.Size (375, 352);
            this.ControlBox = false;
            this.Controls.Add (this.copyrightLabel);
            this.Controls.Add (this.versionLabel);
            this.Controls.Add (this.logoPictureBox);
            this.Controls.Add (this.label2);
            this.Controls.Add (this.donateButton);
            this.Controls.Add (this.button1);
            this.Controls.Add (this.showOnStartupCheckBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WelcomeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Welcome";
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit ();
            this.ResumeLayout (false);
            this.PerformLayout ();

        }

        #endregion

        private System.Windows.Forms.CheckBox showOnStartupCheckBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button donateButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox logoPictureBox;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Label copyrightLabel;
    }
}
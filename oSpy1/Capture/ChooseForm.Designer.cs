//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

namespace oSpy.Capture
{
    partial class ChooseForm
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
            this.label1 = new System.Windows.Forms.Label ();
            this.startBtn = new System.Windows.Forms.Button ();
            this.cancelBtn = new System.Windows.Forms.Button ();
            this.processView = new System.Windows.Forms.ListView ();
            this.x64NoteLbl = new System.Windows.Forms.Label ();
            this.SuspendLayout ();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font ("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point (12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size (174, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Choose processes to monitor:";
            // 
            // startBtn
            // 
            this.startBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.startBtn.Location = new System.Drawing.Point (403, 291);
            this.startBtn.Name = "startBtn";
            this.startBtn.Size = new System.Drawing.Size (75, 23);
            this.startBtn.TabIndex = 7;
            this.startBtn.Text = "&Start";
            this.startBtn.UseVisualStyleBackColor = true;
            this.startBtn.Click += new System.EventHandler (this.startBtn_Click);
            // 
            // cancelBtn
            // 
            this.cancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelBtn.Location = new System.Drawing.Point (484, 291);
            this.cancelBtn.Name = "cancelBtn";
            this.cancelBtn.Size = new System.Drawing.Size (75, 23);
            this.cancelBtn.TabIndex = 8;
            this.cancelBtn.Text = "&Cancel";
            this.cancelBtn.UseVisualStyleBackColor = true;
            // 
            // processView
            // 
            this.processView.Activation = System.Windows.Forms.ItemActivation.OneClick;
            this.processView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.processView.CheckBoxes = true;
            this.processView.Location = new System.Drawing.Point (12, 25);
            this.processView.MultiSelect = false;
            this.processView.Name = "processView";
            this.processView.Size = new System.Drawing.Size (547, 260);
            this.processView.TabIndex = 9;
            this.processView.UseCompatibleStateImageBehavior = false;
            this.processView.View = System.Windows.Forms.View.List;
            this.processView.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler (this.anyView_ItemCheck);
            this.processView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler (this.anyView_ItemSelectionChanged);
            // 
            // x64NoteLbl
            // 
            this.x64NoteLbl.AutoSize = true;
            this.x64NoteLbl.Location = new System.Drawing.Point (12, 296);
            this.x64NoteLbl.Name = "x64NoteLbl";
            this.x64NoteLbl.Size = new System.Drawing.Size (248, 13);
            this.x64NoteLbl.TabIndex = 10;
            this.x64NoteLbl.Text = "Note: Only 32 bit processes are currently supported";
            // 
            // ChooseForm
            // 
            this.AcceptButton = this.startBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelBtn;
            this.ClientSize = new System.Drawing.Size (571, 326);
            this.Controls.Add (this.x64NoteLbl);
            this.Controls.Add (this.processView);
            this.Controls.Add (this.cancelBtn);
            this.Controls.Add (this.startBtn);
            this.Controls.Add (this.label1);
            this.MaximizeBox = false;
            this.Name = "ChooseForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Capture";
            this.Shown += new System.EventHandler (this.InjectForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler (this.ChooseForm_FormClosing);
            this.ResumeLayout (false);
            this.PerformLayout ();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button startBtn;
        private System.Windows.Forms.Button cancelBtn;
        private System.Windows.Forms.ListView processView;
        private System.Windows.Forms.Label x64NoteLbl;

    }
}

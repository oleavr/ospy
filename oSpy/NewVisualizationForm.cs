using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace oSpy
{
    public partial class NewVisualizationForm : Form
    {
        public NewVisualizationForm()
        {
            InitializeComponent();

            foreach (SessionVisualizer visualizer in StreamVisualizationManager.Visualizers)
            {
                if (visualizer.Visible)
                {
                    visualizersBox.Items.Add(visualizer);
                }
            }
        }

        public SessionVisualizer[] GetSelectedVisualizers()
        {
            List<SessionVisualizer> visualizers = new List<SessionVisualizer>(2);

            foreach (object obj in visualizersBox.Items)
            {
                if (visualizersBox.GetItemChecked(visualizersBox.Items.IndexOf(obj)))
                {
                    SessionVisualizer vis = obj as SessionVisualizer;
                    visualizers.Add(vis);
                }
            }

            return visualizers.ToArray();
        }

        private void okBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void visualizersBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            int checkCount = (e.NewValue == CheckState.Checked) ? 1 : -1;

            if (checkCount < 1)
            {
                for (int i = 0; i < visualizersBox.Items.Count; i++)
                {
                    if (visualizersBox.GetItemChecked(i))
                        checkCount++;
                }
            }

            okBtn.Enabled = (checkCount > 0);
        }
    }
}
//
// Copyright (c) 2007 Ole André Vadla Ravnås <oleavr@gmail.com>
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

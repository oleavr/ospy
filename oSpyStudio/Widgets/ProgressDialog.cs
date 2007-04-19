using System;

namespace oSpyStudio.Widgets
{
	public partial class ProgressDialog : Gtk.Dialog
	{
		public ProgressDialog(string operation)
		{
		    this.Build();
			
			operationLbl.Text = operation;
		}

		protected virtual void cancelBtn_clicked(object sender, System.EventArgs e)
		{
		    Respond(Gtk.ResponseType.Cancel);
		}
		
		public void UpdateProgress(int percentage)
		{
		    progressbar.Fraction = (float) percentage / 100.0f;
		}
	}
}

using System;

namespace oSpyStudio.Widgets
{
	public class ProgressDialog : Gtk.Dialog
	{
	    protected Gtk.Label operationLbl;
	    protected Gtk.ProgressBar progressbar;

		public ProgressDialog(string operation)
		{
			Stetic.Gui.Build(this, typeof(oSpyStudio.Widgets.ProgressDialog));
			
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

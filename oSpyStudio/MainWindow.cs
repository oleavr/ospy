using System;
using Gtk;

public class MainWindow : Gtk.Window
{
    protected Gtk.TreeView transactionList;
    protected Gtk.TreeView resourceList;
    protected Gtk.TreeView processList;

    public MainWindow()
        : base("")
    {
        Stetic.Gui.Build(this, typeof(MainWindow));

		Gtk.TreeStore processListStore = new Gtk.TreeStore(typeof(string));
		processList.Model = processListStore;
		processList.AppendColumn("Process", new Gtk.CellRendererText(), "text", 0);
		
		Gtk.TreeStore resourceListStore = new Gtk.TreeStore(typeof(string));
		resourceList.Model = resourceListStore;
		resourceList.AppendColumn("Resource", new Gtk.CellRendererText(), "text", 0);

        // index, type, time, sender, description
        Gtk.TreeStore eventListStore = new Gtk.TreeStore(typeof(int),
                                                         typeof(Gdk.Pixbuf),
                                                         typeof(string),
                                                         typeof(string),
                                                         typeof(string));
        transactionList.Model = eventListStore;

        transactionList.AppendColumn("Index", new Gtk.CellRendererText(), "text", 0);
        transactionList.AppendColumn("Type", new Gtk.CellRendererPixbuf(), "pixbuf", 1);
        transactionList.AppendColumn("Time", new Gtk.CellRendererText(), "text", 2);
        transactionList.AppendColumn("Sender", new Gtk.CellRendererText(), "text", 3);
        transactionList.AppendColumn("Description", new Gtk.CellRendererText(), "text", 4);
    }
    
    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }

    protected virtual void OnQuit(object sender, System.EventArgs e)
    {
        Application.Quit();
    }
    
    protected virtual void OnOpen(object sender, System.EventArgs e)
    {
    	Gtk.FileChooserDialog fc=
        new Gtk.FileChooserDialog("Choose the file to open",
        	                      this,
                                  FileChooserAction.Open,
                                  "Cancel", ResponseType.Cancel,
                                  "Open", ResponseType.Accept);
		Gtk.FileFilter ff = new Gtk.FileFilter();
		ff.AddPattern("*.osd");
		ff.Name = "oSpy dump files (.osd)";
		fc.AddFilter(ff);

        if (fc.Run() == (int)ResponseType.Accept) 
        {
        	System.IO.FileStream file = System.IO.File.OpenRead(fc.Filename);
 			//TODO: decode the input file and fill the viewer
 			file.Close();
        }
        fc.Destroy();
    }

    protected virtual void OnAbout(object sender, System.EventArgs e)
    {
    	Gtk.AboutDialog ad = new Gtk.AboutDialog();
    	ad.Name = "oSpy Studio";
    	ad.Website = "http://code.google.com/p/ospy/";
    	string[] Authors = new string[3];
    	Authors[0] = "Ole André Vadla Ravnås";
    	Authors[1] = "Ali Sabil";
    	Authors[2] = "Johann Prieur";
    	ad.Authors = Authors;
    	ad.Run();
    	ad.Destroy();
    }
}

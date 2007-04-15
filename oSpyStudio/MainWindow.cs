using System;
using System.IO;
using Gtk;
using oSpyStudio.Widgets;

public class MainWindow : Gtk.Window
{
    protected TreeView transactionList;
    protected TreeView resourceList;
    protected TreeView processList;
    protected DataView dataView;

    public MainWindow()
        : base("")
    {
        Stetic.Gui.Build(this, typeof(MainWindow));

		Gtk.TreeStore processListStore = new TreeStore(typeof(string));
		processList.Model = processListStore;
		processList.AppendColumn("Process", new CellRendererText(), "text", 0);
		
		Gtk.TreeStore resourceListStore = new TreeStore(typeof(string));
		resourceList.Model = resourceListStore;
		resourceList.AppendColumn("Resource", new CellRendererText(), "text", 0);

        // index, type, time, sender, description
        Gtk.TreeStore eventListStore = new Gtk.TreeStore(typeof(int),
                                                         typeof(Gdk.Pixbuf),
                                                         typeof(string),
                                                         typeof(string),
                                                         typeof(string));
        transactionList.Model = eventListStore;

        transactionList.AppendColumn("Index", new CellRendererText(), "text", 0);
        transactionList.AppendColumn("Type", new CellRendererPixbuf(), "pixbuf", 1);
        transactionList.AppendColumn("Time", new CellRendererText(), "text", 2);
        transactionList.AppendColumn("Sender", new CellRendererText(), "text", 3);
        transactionList.AppendColumn("Description", new CellRendererText(), "text", 4);
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
#if false
        // Use this code for testing the DataView widget
        byte[] bytes = File.ReadAllBytes("/home/oleavr/Desktop/base64.bin");
        ListStore store = new ListStore(typeof(object));
        store.AppendValues(new object[] { bytes });
        dataView.Model = store;
		return;
#endif

    	FileChooserDialog fc =
            new FileChooserDialog("Choose the file to open",
        	                      this,
                                  FileChooserAction.Open,
                                  "Cancel", ResponseType.Cancel,
                                  "Open", ResponseType.Accept);
		FileFilter ff = new FileFilter();
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
    	AboutDialog ad = new AboutDialog();
    	ad.Name = "oSpy Studio";
    	ad.Website = "http://code.google.com/p/ospy/";
    	string[] authors = new string[3];
    	authors[0] = "Ole André Vadla Ravnås";
    	authors[1] = "Ali Sabil";
    	authors[2] = "Johann Prieur";
    	ad.Authors = authors;
    	ad.Run();
    	ad.Destroy();
    }
}

using System;
using System.IO;
using System.ComponentModel;
using Gtk;
using oSpyStudio.Widgets;
using oSpy.SharpDumpLib;
using ICSharpCode.SharpZipLib.BZip2;

public class MainWindow : Gtk.Window
{
    protected TreeView transactionList;
    protected TreeView resourceList;
    protected TreeView processList;
    protected DataView dataView;
    
    protected Dump curDump = null;

    protected ProgressDialog progressDlg = null;
    protected string curOperation = null;

    protected DumpLoader dumpLoader;

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
        Gtk.TreeStore eventListStore = new Gtk.TreeStore(typeof(string));
        transactionList.Model = eventListStore;

        transactionList.AppendColumn("Transaction", new CellRendererText(), "text", 0);
        
        dumpLoader = new DumpLoader();
        dumpLoader.LoadProgressChanged += new ProgressChangedEventHandler(curOperation_ProgressChanged);
        dumpLoader.LoadCompleted += new LoadCompletedEventHandler(dumpLoader_LoadDumpCompleted);
    }
    
    private void CloseCurrentDump()
    {
        if (curDump != null)
        {
            curDump.Close();
            curDump = null;
        }
    }

    private void curOperation_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        if (progressDlg != null)
            progressDlg.UpdateProgress(e.ProgressPercentage);
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        CloseCurrentDump();
        Application.Quit();
        a.RetVal = true;
    }

    protected virtual void OnQuit(object sender, System.EventArgs e)
    {
        CloseCurrentDump();
        Application.Quit();
    }
    
    protected virtual void OnOpen(object sender, System.EventArgs e)
    {

        // Use this code for testing the DataView widget
        byte[] bytes = File.ReadAllBytes("/home/asabil/Desktop/bling.tar.bz2");
        ListStore store = new ListStore(typeof(object));
        store.AppendValues(new object[] { bytes });
        dataView.Model = store;
		return;


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

        ResponseType response = (ResponseType) fc.Run();
        string filename = fc.Filename;
        fc.Destroy();
        
        if (response == ResponseType.Accept)
        {
        	Stream file = new BZip2InputStream(File.OpenRead(filename));
        	
        	curOperation = "Loading";
        	progressDlg = new ProgressDialog(curOperation);
        	dumpLoader.LoadAsync(file, curOperation);
        	
        	int result = progressDlg.Run();
        	if (result != (int) ResponseType.Accept)
        	{
        	    Console.Out.WriteLine("cancelling");
        	    dumpLoader.LoadAsyncCancel(curOperation);
        	}
        	
        	progressDlg.Destroy();
        	progressDlg = null;
        	curOperation = null;
        }
    }

    private void dumpLoader_LoadDumpCompleted(object sender, LoadCompletedEventArgs e)
    {
        if (e.Cancelled)
        {
        	Console.Out.WriteLine("cancelled");
            return;
        }

        progressDlg.Respond(ResponseType.Accept);

        Dump dump;

        try
        {
            dump = e.Dump;
        	Console.Out.WriteLine("opened successfully");
        }
        catch (Exception ex)
        {
            ShowErrorMessage(String.Format("Failed to load dump: {0}", ex.Message));
            return;
        }
        
        CloseCurrentDump();
        curDump = dump;    }

    private void ShowErrorMessage(string message)
    {
        MessageDialog md = new MessageDialog(this,
            DialogFlags.DestroyWithParent,
            MessageType.Error,
            ButtonsType.Close,
            message);
        md.Run();
        md.Destroy();
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

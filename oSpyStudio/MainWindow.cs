using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using oSpyStudio.Widgets;
using oSpy.SharpDumpLib;
using ICSharpCode.SharpZipLib.BZip2;

public partial class MainWindow : Gtk.Window
{
    protected Dump curDump = null;

    protected string curOperation = null;
    protected DumpLoader dumpLoader;
    protected DumpParser dumpParser;

    public MainWindow()
        : base("")
    {
        this.Build();

		Gtk.TreeStore processListStore = new Gtk.TreeStore(typeof(string));
		processList.Model = processListStore;
		processList.AppendColumn("Process", new Gtk.CellRendererText(), "text", 0);
		
		Gtk.TreeStore resourceListStore = new Gtk.TreeStore(typeof(string));
		resourceList.Model = resourceListStore;
		resourceList.AppendColumn("Resource", new Gtk.CellRendererText(), "text", 0);

        // index, type, time, sender, description
        Gtk.TreeStore eventListStore = new Gtk.TreeStore(typeof(string));
        transactionList.Model = eventListStore;

        transactionList.AppendColumn("Transaction", new Gtk.CellRendererText(), "text", 0);
        
        dumpLoader = new DumpLoader();
        dumpLoader.LoadProgressChanged += new ProgressChangedEventHandler(curOperation_ProgressChanged);
        dumpLoader.LoadCompleted += new LoadCompletedEventHandler(dumpLoader_LoadCompleted);
        
        dumpParser = new DumpParser();
        dumpParser.ParseProgressChanged += new ParseProgressChangedEventHandler(dumpParser_ParseProgressChanged);
        dumpParser.ParseCompleted += new ParseCompletedEventHandler(dumpParser_ParseCompleted);
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
        if (loadProgress != null)
            loadProgress.Fraction = (float) e.ProgressPercentage / 100.0f;
    }

    protected void OnDeleteEvent(object sender, Gtk.DeleteEventArgs a)
    {
        CloseCurrentDump();
        Gtk.Application.Quit();
        a.RetVal = true;
    }

    protected virtual void OnQuit(object sender, System.EventArgs e)
    {
        CloseCurrentDump();
        Gtk.Application.Quit();
    }
    
    protected virtual void OnOpen(object sender, System.EventArgs e)
    {
/*
        // Use this code for testing the DataView widget
        byte[] bytes = File.ReadAllBytes("/home/asabil/Desktop/bling.tar.bz2");
        ListStore store = new ListStore(typeof(object));
        store.AppendValues(new object[] { bytes });
        dataView.Model = store;
		return;
*/
    	Gtk.FileChooserDialog fc =
            new Gtk.FileChooserDialog("Choose the file to open",
        	                      this,
                                  Gtk.FileChooserAction.Open,
                                  "Cancel", Gtk.ResponseType.Cancel,
                                  "Open", Gtk.ResponseType.Accept);
		Gtk.FileFilter ff = new Gtk.FileFilter();
		ff.AddPattern("*.osd");
		ff.Name = "oSpy dump files (.osd)";
		fc.AddFilter(ff);

        Gtk.ResponseType response = (Gtk.ResponseType) fc.Run();
        string filename = fc.Filename;
        fc.Destroy();
        
        if (response == Gtk.ResponseType.Accept)
        {
        	Stream file = new BZip2InputStream(File.OpenRead(filename));
        	
        	curOperation = "Loading";
            statusBar.Push(1, curOperation);
            cancelLoadButton.Sensitive = true;
        	dumpLoader.LoadAsync(file, curOperation);
        }
    }

    private void dumpLoader_LoadCompleted(object sender, LoadCompletedEventArgs e)
    {
        loadProgress.Fraction = 0;
        cancelLoadButton.Sensitive = false;
        statusBar.Pop(1);
        curOperation = null;
        if (e.Cancelled)
        {
        	Console.Out.WriteLine("load cancelled");
            return;
        }

        Dump dump;

        try
        {
            dump = e.Dump;

        	Console.Out.WriteLine("load cancelled");
        }
        catch (Exception ex)
        {
            ShowErrorMessage(String.Format("Failed to load dump: {0}", ex.Message));
            return;
        }
        
        CloseCurrentDump();
        curDump = dump;
        curOperation = "Parsing";
        statusBar.Push(1, curOperation);
        cancelLoadButton.Sensitive = true;
        dumpParser.ParseAsync(curDump, curOperation);
    }
    
    private Resource latestResource = null;
    
    private void dumpParser_ParseProgressChanged(object sender, ParseProgressChangedEventArgs e)
    {
        curOperation_ProgressChanged(sender, e);

        if (e.LatestResource != null)
            latestResource = e.LatestResource;

        if (latestResource != null)
        {
            loadProgress.Text = String.Format("Found handle 0x{0:x}", latestResource.Handle);
        }
    }
    
    private void dumpParser_ParseCompleted(object sender, ParseCompletedEventArgs e)
    {
        loadProgress.Fraction = 0;
        loadProgress.Text = "";
        cancelLoadButton.Sensitive = false;
        statusBar.Pop(1);
        curOperation = null;
        if (e.Cancelled)
        {
        	Console.Out.WriteLine("parse cancelled");
            return;
        }

        List<Resource> resources;

        try
        {
            resources = e.Resources;
            
            // Just clean up for now
            foreach (Resource res in resources)
            {
                res.Close();
            }
        }
        catch (Exception ex)
        {
            ShowErrorMessage(String.Format("Failed to parse dump: {0}", ex.Message));
            return;
        }
    }

    private void ShowErrorMessage(string message)
    {
        Gtk.MessageDialog md = new Gtk.MessageDialog(this,
            Gtk.DialogFlags.DestroyWithParent,
            Gtk.MessageType.Error,
            Gtk.ButtonsType.Close,
            message);
        md.Run();
        md.Destroy();
    }

    protected virtual void OnAbout(object sender, System.EventArgs e)
    {
    	Gtk.AboutDialog ad = new Gtk.AboutDialog();
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

    protected virtual void cancelLoadButton_clicked(object sender, System.EventArgs e)
    {
        // FIXME: this is ugly
        if (curOperation == "Loading")
        {
            Console.Out.WriteLine("cancelling load");
            dumpLoader.LoadAsyncCancel(curOperation);
        }
        else        
        {
            Console.Out.WriteLine("cancelling parse");
            dumpParser.ParseAsyncCancel(curOperation);
        }
    }
}

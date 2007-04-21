using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using oSpyStudio.Widgets;
using oSpy.SharpDumpLib;
using ICSharpCode.SharpZipLib.BZip2;

public partial class MainWindow : Gtk.Window
{
	protected Gtk.TreeStore processListStore;
	protected Gtk.TreeStore resourceListStore;
    protected Gtk.TreeStore dataExchangeListStore;
    
    protected Gtk.ListStore dataChunkStore;

    private List<Process> selectedProcesses = new List<Process>();

    protected Dump curDump = null;
    protected List<Process> curProcesses = null;

    protected string curOperation = null;
    protected DumpLoader dumpLoader;
    protected DumpParser dumpParser;

    public MainWindow()
        : base("")
    {
        this.Build();

		processListStore = new Gtk.TreeStore(typeof(Process));
		processList.Model = processListStore;
		processList.AppendColumn("Process", new Gtk.CellRendererText(), new Gtk.TreeCellDataFunc(processList_CellDataFunc));

		resourceListStore = new Gtk.TreeStore(typeof(Resource));
		resourceList.Model = resourceListStore;
		resourceList.AppendColumn("Resource", new Gtk.CellRendererText(), new Gtk.TreeCellDataFunc(resourceList_CellDataFunc));

        dataExchangeListStore = new Gtk.TreeStore(typeof(DataExchange));
        dataExchangeList.Model = dataExchangeListStore;
        dataExchangeList.AppendColumn("DataExchange", new Gtk.CellRendererText(), new Gtk.TreeCellDataFunc(dataExchangeList_CellDataFunc));

        dataChunkStore = new Gtk.ListStore(typeof(object));

        dumpLoader = new DumpLoader();
        dumpLoader.LoadProgressChanged += new ProgressChangedEventHandler(curOperation_ProgressChanged);
        dumpLoader.LoadCompleted += new LoadCompletedEventHandler(dumpLoader_LoadCompleted);

        dumpParser = new DumpParser();
        dumpParser.ParseProgressChanged += new ParseProgressChangedEventHandler(dumpParser_ParseProgressChanged);
        dumpParser.ParseCompleted += new ParseCompletedEventHandler(dumpParser_ParseCompleted);
    }

    protected virtual void processList_CursorChanged(object sender, System.EventArgs e)
    {
        resourceListStore.Clear();
        selectedProcesses.Clear();

        Gtk.TreePath[] selected = processList.Selection.GetSelectedRows();
        if (selected.Length < 1)
            return;

        Gtk.TreeIter iter;
        if (!processListStore.GetIter(out iter, selected[0]))
            return;

        Process selectedProc = processListStore.GetValue(iter, 0) as Process;
        if (selectedProc != null)
            selectedProcesses.Add(selectedProc);
        else
            selectedProcesses.AddRange(curProcesses);

        resourceListStore.AppendValues(new object[] { null, });
        foreach (Process proc in selectedProcesses)
        {
            foreach (Resource res in proc.Resources)
            {
                resourceListStore.AppendValues(new object[] { res, });
            }
        }
    }

    private void processList_CellDataFunc(Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
    {
    	Gtk.CellRendererText textCell = cell as Gtk.CellRendererText;

    	Process proc = model.GetValue(iter, 0) as Process;
    	if (proc != null)
    	    textCell.Text = proc.ToString();
    	else
    	    textCell.Text = "(all)";
    }

    protected virtual void resourceList_CursorChanged(object sender, System.EventArgs e)
    {
        dataExchangeListStore.Clear();

        Gtk.TreePath[] selected = resourceList.Selection.GetSelectedRows();
        if (selected.Length < 1)
            return;

        Gtk.TreeIter iter;
        if (!resourceListStore.GetIter(out iter, selected[0]))
            return;

        List<Resource> resources = null;
        Resource selectedResource = resourceListStore.GetValue(iter, 0) as Resource;
        if (selectedResource != null)
        {
            resources = new List<Resource>(1);
            resources.Add(selectedResource);
        }
        else
        {
            resources = new List<Resource>();

            foreach (Process proc in selectedProcesses)
            {
                resources.AddRange(proc.Resources);
            }
        }

        foreach (Resource res in resources)
        {
            foreach (DataExchange exchange in res.DataExchanges)
            {
                dataExchangeListStore.AppendValues(new object[] { exchange, });
            }
        }
    }

    private void resourceList_CellDataFunc(Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
    {
    	Gtk.CellRendererText textCell = cell as Gtk.CellRendererText;

    	Resource res = model.GetValue(iter, 0) as Resource;
    	if (res != null)
    	    textCell.Text = res.ToString();
    	else
    	    textCell.Text = "(all)";
    }

    protected virtual void dataExchangeList_CursorChanged(object sender, System.EventArgs e)
    {
        dataChunkStore.Clear();
        
        Gtk.TreePath[] selected = dataExchangeList.Selection.GetSelectedRows();
        if (selected.Length < 1)
            return;

        Gtk.TreeIter iter;
        if (!dataExchangeListStore.GetIter(out iter, selected[0]))
            return;

        DataExchange selectedExchange = dataExchangeListStore.GetValue(iter, 0) as DataExchange;
        for (int i = 0; i < selectedExchange.Count; i++)
        {
            dataChunkStore.AppendValues(new object[] { selectedExchange.GetData(i) });
        }
        dataView.Model = dataChunkStore;
    }

    private void dataExchangeList_CellDataFunc(Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
    {
    	Gtk.CellRendererText textCell = cell as Gtk.CellRendererText;

    	DataExchange exchange = model.GetValue(iter, 0) as DataExchange;
	    textCell.Text = exchange.ToString();
    }

    private void CloseCurrentDump()
    {
        selectedProcesses.Clear();

        processListStore.Clear();
        resourceListStore.Clear();
        dataExchangeListStore.Clear();
        
        dataChunkStore.Clear();

        // TODO: might not be necessary
        processList.Selection.UnselectAll();
        resourceList.Selection.UnselectAll();
        dataExchangeList.Selection.UnselectAll();

    	if (curProcesses != null)
    	{
    		foreach (Process proc in curProcesses)
    		{
    			proc.Close();
    		}
    		curProcesses = null;
    	}
    	
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

        try
        {
        	curProcesses = e.Processes;
        }
        catch (Exception ex)
        {
            ShowErrorMessage(String.Format("Failed to parse dump: {0}", ex.Message));
            return;
        }

        processListStore.AppendValues(new object[] { null, });
        foreach (Process proc in curProcesses)
        {
        	processListStore.AppendValues(new object[] { proc, });
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

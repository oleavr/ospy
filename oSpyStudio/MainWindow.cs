using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using oSpyStudio.Widgets;
using oSpy.SharpDumpLib;
using ICSharpCode.SharpZipLib.BZip2;
using Gdl;

internal enum DataTransferColumn
{
    From,
    Size,
    Description,
}

public partial class MainWindow : Gtk.Window
{
    protected Gdk.Pixbuf incomingPixbuf = null; 
    protected Gdk.Pixbuf outgoingPixbuf = null; 
    protected Gdk.Pixbuf socketPixbuf = null; 
    protected Gdk.Pixbuf securePixbuf = null; 

	protected Gtk.TreeView processList;
	protected Gtk.TreeView resourceList;
	protected Gtk.TreeView dataTransferList;
	protected Gtk.TreeStore processListStore;
	protected Gtk.TreeStore resourceListStore;
    protected Gtk.TreeStore dataTransferListStore;
    
    protected DataView dataView;
    protected Gtk.ListStore dataChunkStore;
    
    protected Gdl.Dock dock;

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

		// Main widgets configuration        
        System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
        incomingPixbuf = new Gdk.Pixbuf(asm, "incoming.png");
        outgoingPixbuf = new Gdk.Pixbuf(asm, "outgoing.png");
        socketPixbuf = new Gdk.Pixbuf(asm, "socket.png");
        securePixbuf = new Gdk.Pixbuf(asm, "secure.png");

        dataChunkStore = new Gtk.ListStore(typeof(object), typeof(string), typeof(string));

		processListStore = new Gtk.TreeStore(typeof(Process));
		processList = new Gtk.TreeView(processListStore);
		processList.HeadersVisible = false;
		processList.AppendColumn("Process", new Gtk.CellRendererText(), new Gtk.TreeCellDataFunc(processList_CellDataFunc));
		processList.CursorChanged += new EventHandler(processList_CursorChanged);

		resourceListStore = new Gtk.TreeStore(typeof(Resource));
		resourceList = new Gtk.TreeView(resourceListStore);
		resourceList.HeadersVisible = false;
		Gtk.TreeViewColumn col = new Gtk.TreeViewColumn();
		col.Title = "Resource";
		Gtk.CellRendererPixbuf pbRenderer = new Gtk.CellRendererPixbuf();
		Gtk.CellRendererText txtRenderer = new Gtk.CellRendererText();
		col.PackStart(pbRenderer, false);
		col.PackStart(txtRenderer, true);
		col.SetCellDataFunc(pbRenderer, new Gtk.TreeCellDataFunc(resourceList_PixbufCellDataFunc));
		col.SetCellDataFunc(txtRenderer, new Gtk.TreeCellDataFunc(resourceList_TextCellDataFunc));
		resourceList.AppendColumn(col);
		resourceList.CursorChanged += new EventHandler(resourceList_CursorChanged);

        dataTransferListStore = new Gtk.TreeStore(typeof(DataTransfer));
        dataTransferList = new Gtk.TreeView(dataTransferListStore);
        dataTransferList.Selection.Mode = Gtk.SelectionMode.Multiple;
        dataTransferList.Selection.Changed += new EventHandler(dataTransferList_SelectionChanged);
        
        dataTransferList.AppendColumn("", new Gtk.CellRendererPixbuf(), new Gtk.TreeCellDataFunc(dataTransferList_DirectionCellDataFunc));
        col = dataTransferList.AppendColumn("From", new Gtk.CellRendererText(), new Gtk.TreeCellDataFunc(dataTransferList_TextCellDataFunc));
        col.UserData = (IntPtr) DataTransferColumn.From;
        col = dataTransferList.AppendColumn("Size", new Gtk.CellRendererText(), new Gtk.TreeCellDataFunc(dataTransferList_TextCellDataFunc));
        col.UserData = (IntPtr) DataTransferColumn.Size;
        col = dataTransferList.AppendColumn("Description", new Gtk.CellRendererText(), new Gtk.TreeCellDataFunc(dataTransferList_TextCellDataFunc));
        col.UserData = (IntPtr) DataTransferColumn.Description;
		dataView = new DataView();
        dataView.Model = dataChunkStore;
		
		// Docking
		dock = new Gdl.Dock();
		Gdl.DockBar dockBar = new Gdl.DockBar(dock);
		Gtk.Box workspace = new Gtk.HBox(false, 5);
		workspace.PackStart(dockBar, false, false, 0);
		workspace.PackEnd(dock, true, true, 0);
		MainVBox.PackStart(workspace, true, true, 0);
		
		Gdl.DockItem processDock = getDockItem(processList, "process", "Process", Gtk.Stock.Execute);
		dock.AddItem(processDock, DockPlacement.Left);
		Gdl.DockItem resourceDock = getDockItem(resourceList, "resource", "Resource", Gtk.Stock.File);
		dock.AddItem(resourceDock, DockPlacement.Center);
		Gdl.DockItem dataTransferDock = getDockItem(dataTransferList, "datatransfer", "Data transfer", Gtk.Stock.File);
		dock.AddItem(dataTransferDock, DockPlacement.Right);
		Gdl.DockItem dataViewDock= getDockItem(dataView, "dataview", "Data view", Gtk.Stock.File);
		dock.AddItem(dataViewDock, DockPlacement.Bottom);
		
		resourceDock.DockTo(dataTransferDock, DockPlacement.Left);
		processDock.DockTo(resourceDock, DockPlacement.Left);

		MainVBox.ShowAll();
		
		// Dump related objects
        dumpLoader = new DumpLoader();
        dumpLoader.LoadProgressChanged += new ProgressChangedEventHandler(curOperation_ProgressChanged);
        dumpLoader.LoadCompleted += new LoadCompletedEventHandler(dumpLoader_LoadCompleted);

        dumpParser = new DumpParser();
        dumpParser.ParseProgressChanged += new ParseProgressChangedEventHandler(dumpParser_ParseProgressChanged);
        dumpParser.ParseCompleted += new ParseCompletedEventHandler(dumpParser_ParseCompleted);
        
    }

	private Gdl.DockItem getDockItem(Gtk.Widget widget, string id, string label, string icon)
	{
		Gdl.DockItem dockItem = new Gdl.DockItem(id, label, icon, Gdl.DockItemBehavior.Normal);
		Gtk.ScrolledWindow scroll = new Gtk.ScrolledWindow();
		scroll.Add(widget);
		dockItem.Add(scroll);
		return dockItem;	
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
        dataChunkStore.Clear();
        dataTransferListStore.Clear();

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
            foreach (DataTransfer transfer in res.DataTransfers)
            {                
                dataTransferListStore.AppendValues(new object[] { transfer, });
            }
        }
    }

    private void resourceList_PixbufCellDataFunc(Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
    {
        Gtk.CellRendererPixbuf pbCell = cell as Gtk.CellRendererPixbuf;
        pbCell.Pixbuf = null;

    	Resource res = model.GetValue(iter, 0) as Resource;
    	if (res == null)
    	    return;

        if (res is SocketResource)
            pbCell.Pixbuf = socketPixbuf;
        else if (res is CryptoResource)
            pbCell.Pixbuf = securePixbuf;
    }

    private void resourceList_TextCellDataFunc(Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
    {
    	Gtk.CellRendererText textCell = cell as Gtk.CellRendererText;

    	Resource res = model.GetValue(iter, 0) as Resource;
    	if (res != null)
    	{
    	    bool handled = false;

    	    if (res is SocketResource)
    	    {
    	        SocketResource sockRes = res as SocketResource;

                if (sockRes.AddressFamily != AddressFamily.Unknown)
                {
        	        textCell.Text = String.Format("0x{0:x8} - {1}, {2}", sockRes.Handle, sockRes.AddressFamily, sockRes.SocketType);
        	        handled = true;
        	    }
    	    }
    	    
    	    if (!handled)
        	    textCell.Text = String.Format("0x{0:x8}", res.Handle);
    	}
    	else
    	{
    	    textCell.Text = "(all)";
    	}
    }

    private void dataTransferList_SelectionChanged(object sender, EventArgs e)
    {
        dataChunkStore.Clear();

        Gtk.TreePath[] selected = dataTransferList.Selection.GetSelectedRows();
        if (selected.Length < 1)
            return;

        foreach (Gtk.TreePath path in selected)
        {
            Gtk.TreeIter iter;
            if (!dataTransferListStore.GetIter(out iter, path))
                return;

            DataTransfer transfer = dataTransferListStore.GetValue(iter, 0) as DataTransfer;
            
            string linePrefixStr = null;
            string linePrefixColor = null;
            if (transfer.Direction == DataDirection.Incoming)
            {
                linePrefixStr = "<<";
                linePrefixColor = "#8ae899";
            }
            else
            {
                linePrefixStr = ">>";
                linePrefixColor = "#9cb7d1";
            }

            dataChunkStore.AppendValues(new object[] { transfer.Data, linePrefixStr, linePrefixColor });
        }
    }

    private void dataTransferList_DirectionCellDataFunc(Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
    {
    	Gtk.CellRendererPixbuf pbCell = cell as Gtk.CellRendererPixbuf;
    	DataTransfer transfer = model.GetValue(iter, 0) as DataTransfer;
    	
    	if (transfer.Direction == DataDirection.Incoming)
    	    pbCell.Pixbuf = incomingPixbuf;
    	else
    	    pbCell.Pixbuf = outgoingPixbuf;
    }
    
    private void dataTransferList_TextCellDataFunc(Gtk.TreeViewColumn treeCol, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
    {
    	Gtk.CellRendererText textCell = cell as Gtk.CellRendererText;
    	DataTransfer transfer = model.GetValue(iter, 0) as DataTransfer;
    	DataTransferColumn col = (DataTransferColumn) treeCol.UserData;

    	switch (col)
    	{
    	    case DataTransferColumn.From:
        	    textCell.Text = transfer.FunctionName;
        	    break;
        	case DataTransferColumn.Size:
        	    textCell.Text = Convert.ToString(transfer.Size);
        	    break;
        	case DataTransferColumn.Description:
        	    if (transfer.HasMetaKey("net.ipv4.remoteEndpoint"))
        	        textCell.Text = String.Format("remote: {0}", transfer.GetMetaValue("net.ipv4.remoteEndpoint")); 
        	    else
        	        textCell.Text = "";
        	    break;
        }
    }

    private void CloseCurrentDump()
    {
        selectedProcesses.Clear();

        processListStore.Clear();
        resourceListStore.Clear();
        dataTransferListStore.Clear();
        
        dataChunkStore.Clear();

        // TODO: might not be necessary
        processList.Selection.UnselectAll();
        resourceList.Selection.UnselectAll();
        dataTransferList.Selection.UnselectAll();

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
            loadProgress.Text = String.Format("Last resource: 0x{0:x8}", latestResource.Handle);
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

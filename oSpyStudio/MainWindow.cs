using System;
using Gtk;

public class MainWindow : Gtk.Window
{
    protected Gtk.TreeView eventList;

    public MainWindow()
        : base("")
    {
        Stetic.Gui.Build(this, typeof(MainWindow));

        // index, type, time, sender, description
        Gtk.TreeStore eventListStore = new Gtk.TreeStore(typeof(int),
                                                         typeof(Gdk.Pixbuf),
                                                         typeof(string),
                                                         typeof(string),
                                                         typeof(string));
        eventList.Model = eventListStore;

        eventList.AppendColumn("Index", new Gtk.CellRendererText(), "text", 0);
        eventList.AppendColumn("Type", new Gtk.CellRendererPixbuf(), "pixbuf", 1);
        eventList.AppendColumn("Time", new Gtk.CellRendererText(), "text", 2);
        eventList.AppendColumn("Sender", new Gtk.CellRendererText(), "text", 3);
        eventList.AppendColumn("Description", new Gtk.CellRendererText(), "text", 4);
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
}

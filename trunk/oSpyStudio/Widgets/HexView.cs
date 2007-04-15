using System;
using Gtk;

namespace oSpyStudio
{
	public class HexView : Gtk.Bin
	{
	    protected Notebook notebook;
	    protected TextView offsetView;
	    protected TextView mainView;
	    protected TextView asciiView;
	    
	    protected TreeModel model;
	    public TreeModel Model
	    {
	        get { return model; }
	        set { model = value; Update(); }
	    }
	    
	    protected int modelColIndex = -1;
	    public int ModelColIndex
	    {
	        get { return modelColIndex; }
	        set { modelColIndex = value; Update(); }
	    }

		public HexView()
		{
			Stetic.Gui.Build(this, typeof(oSpyStudio.HexView));
			
			ApplyHaxorStyle(notebook);
			ApplyHaxorStyle(offsetView);
			ApplyHaxorStyle(mainView);
			ApplyHaxorStyle(asciiView);
			
			offsetView.Buffer.InsertAtCursor("0000");
			mainView.Buffer.InsertAtCursor("CA FE BA BE CA FE BA BE CA FE BA BE CA FE BA BE");
			asciiView.Buffer.InsertAtCursor("................");
		}
		
		private void ApplyHaxorStyle(Widget w)
		{
			w.ModifyBase(StateType.Normal, new Gdk.Color(0, 0, 0));
			w.ModifyBg(StateType.Normal, new Gdk.Color(0, 0, 0));
		    w.ModifyText(StateType.Normal, new Gdk.Color(192, 192, 192));
		    w.ModifyFont(Pango.FontDescription.FromString("ASCII 10"));
		}
		
		private void Update()
		{
		    if (model == null || modelColIndex < 0)
		        return;


		}
	}
}

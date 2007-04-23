using System;
using System.Text;

namespace oSpyStudio.Widgets
{
	public partial class DataView : Gtk.Bin
	{
	    private class HexChunk : Gtk.HBox
	    {
	        protected Gtk.TextView prefixView;
    	    protected Gtk.TextView offsetView;
    	    protected Gtk.TextView mainView;
    	    protected Gtk.TextView asciiView;
    	    
    	    private bool frozen = false;
    	    
    	    protected int colsPerRow = 16;
    	    public int ColsPerRow
    	    {
    	        get { return colsPerRow; }
    	    }
    	    
    	    protected byte[] bytes = null;
    	    public byte[] Bytes
    	    {
    	        get { return bytes; }
    	        set { bytes = value; Update(); }
    	    }
	        
	        protected string linePrefix = null;
	        public string LinePrefix
	        {
	            get { return linePrefix; }
	            set { linePrefix = value; Update(); }
	        }

            protected Gtk.TextTag[] linePrefixTags = null;
	        public string LinePrefixColor
	        {
	            set
	            {
	                if (value == null || value == String.Empty)
	                    throw new ArgumentException("LinePrefixColor can't be null or blank");
	                linePrefixTags[0].Foreground = value;
	                Update();
	            }
	        }

	        public HexChunk()
	        {
	            prefixView = new Gtk.TextView();
	            linePrefixTags = new Gtk.TextTag[1];
	            Gtk.TextTag tag = new Gtk.TextTag("default");
	            linePrefixTags[0] = tag;
	            prefixView.Buffer.TagTable.Add(linePrefixTags[0]);
	            
	            offsetView = new Gtk.TextView();
	            mainView = new Gtk.TextView();
	            asciiView = new Gtk.TextView();

                prefixView.Editable = false;
	            offsetView.Editable = false;
	            mainView.Editable = false;
	            asciiView.Editable = false;
	            
	            PackStart(prefixView, false, true, 5);
	            PackStart(offsetView, false, true, 5);
	            PackStart(mainView, false, true, 10);
	            PackStart(asciiView, false, true, 5);

                prefixView.Show();
	            offsetView.Show();
	            mainView.Show();
	            asciiView.Show();
	        }
	        
	        public void Freeze()
	        {
	            frozen = true;
	        }
	        
	        public void UnFreeze()
	        {
	            if (!frozen)
	                return;
	            frozen = false;
	            Update();
	        }
	        
    		public void ApplyCustomStyle(ApplyStyleFunction f)
    		{
    		    f(prefixView);
    		    f(offsetView);
    		    f(mainView);
    		    f(asciiView);
    		}
    		
    		private void Update()
    		{
    		    if (frozen || bytes == null)
    		        return;

    		    int rowCount = (bytes.Length / colsPerRow) + 1;
    		    
    		    Gtk.TextBuffer pfxBuf = prefixView.Buffer;
    		    Gtk.TextBuffer offBuf = offsetView.Buffer;
    		    Gtk.TextBuffer mainBuf = mainView.Buffer;
    		    Gtk.TextBuffer asciiBuf = asciiView.Buffer;

                Gtk.TextIter pfxIter = pfxBuf.StartIter;
    		    Gtk.TextIter offIter = offBuf.StartIter;
    		    Gtk.TextIter mainIter = mainBuf.StartIter;
    		    Gtk.TextIter asciiIter = asciiBuf.StartIter;

                int offset = 0, remaining = bytes.Length;
    		    for (int i = 0; i < rowCount; i++)
    		    {
    		        int len = Math.Min(remaining, colsPerRow);
    		        if (len == 0)
    		            break;

    		        if (i > 0)
    		        {
    		            pfxBuf.Insert(ref pfxIter, "\n");
            		    offBuf.Insert(ref offIter, "\n");
            		    mainBuf.Insert(ref mainIter, "\n");
            		    asciiBuf.Insert(ref asciiIter, "\n");
    		        }

    		        if (linePrefix != null)
    		            pfxBuf.InsertWithTags(ref pfxIter, linePrefix, linePrefixTags);

           		    offBuf.Insert(ref offIter, String.Format("{0:x4}", offset));

        		    StringBuilder builder = new StringBuilder(colsPerRow * 3);
        		    for (int j = 0; j < len; j++)
        		    {
        		        if (j > 0)
                   		    builder.Append(" ");

        		        builder.AppendFormat("{0:x2}", bytes[offset + j]);
        		    }
        		    for (int j = len; j < colsPerRow; j++)
        		    {
        		        if (j > 0)
        		            builder.Append(" ");

        		        builder.Append("  ");
        		    }
        		    mainBuf.Insert(ref mainIter, builder.ToString());

        		    builder = new StringBuilder(16);
    		        ToNormalizedAscii(bytes, offset, len, builder);
    		        asciiBuf.Insert(ref asciiIter, builder.ToString());
    		        
    		        offset += colsPerRow;
    		        remaining -= colsPerRow;
    		    }
    		}
    		
            private void ToNormalizedAscii(byte[] bytes, int offset, int len, StringBuilder str)
            {
                for (int i = 0; i < len; i++)
                {
                    byte b = bytes[offset + i];
                    char c;

                    if (b >= 33 && b <= 126)
                    {
                        c = (char) b;
                    }
                    else
                    {
                        c = '.';
                    }

                    str.Append(c);
                }
            }
	    }

	    private delegate void ApplyStyleFunction(Gtk.Widget w);

        private bool frozen = false;

	    protected Gtk.TreeModel model = null;
	    public Gtk.TreeModel Model
	    {
	        get { return model; }
	        set
	        {
    		    if (model != null)
    		    {
    		        model.RowChanged -= rowChangedHandler;
    		        model.RowDeleted -= rowDeletedHandler;
    		        model.RowInserted -= rowInsertedHandler;
    		        model.RowsReordered -= rowsReorderedHandler;
    		    }

	            model = value;
	            model.RowChanged += rowChangedHandler;
	            model.RowDeleted += rowDeletedHandler;
	            model.RowInserted += rowInsertedHandler;
	            model.RowsReordered += rowsReorderedHandler;

	            DoFullUpdate();
	        }
	    }
	    
	    private Gtk.RowChangedHandler rowChangedHandler = null;
	    private Gtk.RowDeletedHandler rowDeletedHandler = null;
	    private Gtk.RowInsertedHandler rowInsertedHandler = null;
	    private Gtk.RowsReorderedHandler rowsReorderedHandler = null;

	    protected int dataColIndex = -1;
	    public int DataColIndex
	    {
	        get { return dataColIndex; }
	        set { dataColIndex = value; DoFullUpdate(); }
	    }
	    
	    protected int linePrefixTextColIndex = -1;
	    public int LinePrefixTextColIndex
	    {
	        get { return linePrefixTextColIndex; }
	        set { linePrefixTextColIndex = value; DoFullUpdate(); }
	    }

	    protected int linePrefixColorColIndex = -1;
	    public int LinePrefixColorColIndex
	    {
	        get { return linePrefixColorColIndex; }
	        set { linePrefixColorColIndex = value; DoFullUpdate(); }
	    }

		public DataView()
		{
		    this.Build();
		    
		    rowChangedHandler = new Gtk.RowChangedHandler(model_RowChanged);
		    rowDeletedHandler = new Gtk.RowDeletedHandler(model_RowDeleted);
		    rowInsertedHandler = new Gtk.RowInsertedHandler(model_RowInserted);
		    rowsReorderedHandler = new Gtk.RowsReorderedHandler(model_RowsReordered);

            ApplyHaxorStyle(scrollWin.Child);
		}
		
		public void Freeze()
		{
		    frozen = true;
		}
		
		public void UnFreeze()
		{
		    if (!frozen)
		        return;
		        
		    frozen = false;
		    DoFullUpdate();
		}

		private void DoFullUpdate()
		{
		    if (frozen)
		        return;

		    foreach (Gtk.Widget w in hexVBox.Children)
		    {
		        hexVBox.Remove(w);
		    }

		    if (model == null || dataColIndex < 0)
		        return;

		    Gtk.TreeIter iter;
		    if (!model.GetIterFirst(out iter))
		        return;

            do
		    {
		        HexChunk chunk = new HexChunk();
		        chunk.ApplyCustomStyle(ApplyHaxorStyle);
		        chunk.Show();

		        chunk.Freeze();
		        chunk.Bytes = model.GetValue(iter, dataColIndex) as byte[];
		        if (linePrefixTextColIndex >= 0)
		        {
		            string s = model.GetValue(iter, linePrefixTextColIndex) as string;
		            if (s != null && s != String.Empty)
		                chunk.LinePrefix = s;
		        }
		        if (linePrefixColorColIndex >= 0)
		        {
		            string s = model.GetValue(iter, linePrefixColorColIndex) as string;
		            if (s != null && s != String.Empty)
    		            chunk.LinePrefixColor = s;
		        }
		        chunk.UnFreeze();

		        hexVBox.PackStart(chunk, false, true, 0);
		    }
            while (model.IterNext(ref iter));
		}

        #region Model change handlers
        // TODO: these should be optimized in the future to just
        //       do incremental updates
        
		private void model_RowChanged(object o, Gtk.RowChangedArgs e)
		{
		    DoFullUpdate();
		}
		
		private void model_RowDeleted(object o, Gtk.RowDeletedArgs e)
		{
		    DoFullUpdate();
		}
		
		private void model_RowInserted(object o, Gtk.RowInsertedArgs e)
		{
		    DoFullUpdate();
		}
		
		private void model_RowsReordered(object o, Gtk.RowsReorderedArgs e)
		{
		    DoFullUpdate();
		}
		
        #endregion // Model change handlers

		private void ApplyHaxorStyle(Gtk.Widget w)
		{
		    //Gdk.Color bg = new Gdk.Color(0, 0, 0);
		    //Gdk.Color fg = new Gdk.Color(192, 192, 192);
		    Gdk.Color bg = new Gdk.Color(128, 128, 128);
		    Gdk.Color fg = new Gdk.Color(0, 0, 0);
		    
			w.ModifyBase(Gtk.StateType.Normal, bg);
			w.ModifyBase(Gtk.StateType.Prelight, bg);
			w.ModifyBase(Gtk.StateType.Active, bg);
			w.ModifyBase(Gtk.StateType.Insensitive, bg);
			
			w.ModifyBg(Gtk.StateType.Normal, bg);
			w.ModifyBg(Gtk.StateType.Prelight, bg);
			w.ModifyBg(Gtk.StateType.Active, bg);
			w.ModifyBg(Gtk.StateType.Insensitive, bg);

		    w.ModifyText(Gtk.StateType.Normal, fg);
		    
		    w.ModifyFont(Pango.FontDescription.FromString("Monospace 8"));
		}
	}
}

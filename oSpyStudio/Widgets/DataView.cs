using System;
using System.Text;
using Gtk;

namespace oSpyStudio.Widgets
{
	public partial class DataView : Gtk.Bin
	{
	    private class HexChunk : Gtk.HBox
	    {
    	    protected TextView offsetView;
    	    protected TextView mainView;
    	    protected TextView asciiView;
    	    
    	    protected int colsPerRow = 16;
    	    public int ColsPerRow
    	    {
    	        get { return colsPerRow; }
    	    }
    	    
    	    protected byte[] bytes;
    	    public byte[] Bytes
    	    {
    	        get { return bytes; }
    	        set { bytes = value; Update(); }
    	    }
	        
	        public HexChunk()
	        {	            
	            offsetView = new TextView();
	            mainView = new TextView();
	            asciiView = new TextView();

	            offsetView.Editable = false;
	            mainView.Editable = false;
	            asciiView.Editable = false;
	            
	            PackStart(offsetView, false, true, 0);
	            PackStart(mainView, false, true, 15);
	            PackStart(asciiView, false, true, 0);

	            offsetView.Show();
	            mainView.Show();
	            asciiView.Show();
	        }
	        
    		public void ApplyCustomStyle(ApplyStyleFunction f)
    		{
    		    f(offsetView);
    		    f(mainView);
    		    f(asciiView);
    		}
    		
    		private void Update()
    		{
    		    int rowCount = (bytes.Length / colsPerRow) + 1;

    		    StringBuilder offStr = new StringBuilder(5 * rowCount);
    		    StringBuilder mainStr = new StringBuilder(colsPerRow * 3 * rowCount);
    		    StringBuilder asciiStr = new StringBuilder((colsPerRow + 1) * rowCount);

                int offset = 0, remaining = bytes.Length;
    		    for (int i = 0; i < rowCount; i++)
    		    {
    		        if (i > 0)
    		        {
    		            offStr.Append("\n");
    		            mainStr.Append("\n");
    		            asciiStr.Append("\n");
    		        }

    		        offStr.AppendFormat("{0:x4}", offset);
    		        
    		        int len = Math.Min(remaining, colsPerRow);
        		    for (int j = 0; j < len; j++)
        		    {
        		        if (j > 0)
        		            mainStr.Append(" ");

        		        mainStr.AppendFormat("{0:x2}", bytes[offset + j]);
        		    }

    		        ToNormalizedAscii(bytes, offset, len, asciiStr);
    		        
    		        offset += colsPerRow;
    		        remaining -= colsPerRow;
    		    }

    		    TextIter iter = offsetView.Buffer.StartIter;
    		    offsetView.Buffer.Insert(ref iter, offStr.ToString());

    		    iter = mainView.Buffer.StartIter;
    		    mainView.Buffer.Insert(ref iter, mainStr.ToString());

    		    iter = asciiView.Buffer.StartIter;
    		    asciiView.Buffer.Insert(ref iter, asciiStr.ToString());
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

	    private delegate void ApplyStyleFunction(Widget w);

	    protected TreeModel model;
	    public TreeModel Model
	    {
	        get { return model; }
	        set { model = value; Update(); }
	    }
	    
	    protected int dataColIndex = -1;
	    public int DataColIndex
	    {
	        get { return dataColIndex; }
	        set { dataColIndex = value; Update(); }
	    }

		public DataView()
		{
		    this.Build();

            ApplyHaxorStyle(scrollWin.Child);
		}

		private void Update()
		{
		    if (model == null || dataColIndex < 0)
		        return;

		    TreeIter iter;
		    if (!model.GetIterFirst(out iter))
		        return;

            do
		    {
		        HexChunk chunk = new HexChunk();
		        chunk.ApplyCustomStyle(ApplyHaxorStyle);
		        chunk.Show();
		        chunk.Bytes = model.GetValue(iter, dataColIndex) as byte[];
		        hexVBox.PackStart(chunk);
		    }
            while (model.IterNext(ref iter));
		}
		
		private void ApplyHaxorStyle(Widget w)
		{
		    Gdk.Color bg = new Gdk.Color(0, 0, 0);
		    Gdk.Color fg = new Gdk.Color(192, 192, 192);
		    
			w.ModifyBase(StateType.Normal, bg);
			w.ModifyBase(StateType.Prelight, bg);
			w.ModifyBase(StateType.Active, bg);
			w.ModifyBase(StateType.Insensitive, bg);
			
			w.ModifyBg(StateType.Normal, bg);
			w.ModifyBg(StateType.Prelight, bg);
			w.ModifyBg(StateType.Active, bg);
			w.ModifyBg(StateType.Insensitive, bg);

		    w.ModifyText(StateType.Normal, fg);
		    
		    w.ModifyFont(Pango.FontDescription.FromString("Monospace 10"));
		}
	}
}

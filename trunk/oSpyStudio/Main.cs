using System;
using Gtk;

namespace oSpyStudio
{
    class MainClass
    {
        public static void Main (string[] args)
        {
            Application.Init ();
            System.ComponentModel.AsyncOperationManager.SynchronizationContext = new GLibSynchronizationContext ();
            
            MainWindow win = new MainWindow ();
            Application.Run ();
        }
    }
}

using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace RJLG.IntelliSEM.UI.Controls.PythonDataScience
{
    internal class ToolDockContent : DockContent
    {
        public ToolDockContent()
        {
            DockAreas = DockAreas.DockLeft | DockAreas.DockRight |
                        DockAreas.DockTop | DockAreas.DockBottom |
                        DockAreas.Float;
            HideOnClose = true;
        }

        protected override string GetPersistString()
        {
            return Text;
        }
    }

    internal class DocumentDockContent : DockContent
    {
        public DocumentDockContent()
        {
            DockAreas = DockAreas.Document | DockAreas.Float;
            CloseButtonVisible = false;
        }

        protected override string GetPersistString()
        {
            return "Document";
        }
    }
}

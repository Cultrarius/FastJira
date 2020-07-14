using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fast_Jira.ui
{
    public class MarkdownContainer : FlowDocumentScrollViewer
    {
        public MarkdownContainer() : base()
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            IsTabStop = false;
            Focusable = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            e.Handled = false;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            e.Handled = false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastJira.ui
{
    public class DelegatingScrollViewer : ScrollViewer
    {
        public DelegatingScrollViewer() : base()
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
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

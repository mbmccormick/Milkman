using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using IronCow;

namespace Milkman.Controls
{
    public partial class TaskItemControl : UserControl
    {
        public TaskItemControl()
        {
            InitializeComponent();

            this.Loaded += TaskItemControl_Loaded;
        }

        private void TaskItemControl_Loaded(object sender, RoutedEventArgs e)
        {
            Task target = (Task)this.DataContext;

            if (target.Notes.Count > 0)
                this.vbxNotes.Visibility = System.Windows.Visibility.Visible;
            else
                this.vbxNotes.Visibility = System.Windows.Visibility.Collapsed;

            if (target.HasRecurrence == true)
                this.vbxRecurrence.Visibility = System.Windows.Visibility.Visible;
            else
                this.vbxRecurrence.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}

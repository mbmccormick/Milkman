using IronCow;
using System.Windows;
using System.Windows.Controls;

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
        }
    }
}

using IronCow;
using System.Windows;
using System.Windows.Controls;

namespace Milkman.Controls
{
    public partial class TaskItemInfoControl : UserControl
    {
        public TaskItemInfoControl()
        {
            InitializeComponent();

            this.Loaded += TaskItemInfoControl_Loaded;
        }

        private bool DarkThemeUsed()
        {
            return Visibility.Visible == (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
        }

        private void TaskItemInfoControl_Loaded(object sender, RoutedEventArgs e)
        {
            Task target = (Task)this.DataContext;

            if (this.DarkThemeUsed() == true)
            {
                this.stkDark.Visibility = System.Windows.Visibility.Visible;
                this.stkLight.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.stkLight.Visibility = System.Windows.Visibility.Visible;
                this.stkDark.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }
}

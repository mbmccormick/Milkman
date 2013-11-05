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

        private bool DarkThemeUsed()
        {
            return Visibility.Visible == (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
        }

        private void TaskItemControl_Loaded(object sender, RoutedEventArgs e)
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

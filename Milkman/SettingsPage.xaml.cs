using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Milkman.Common;
using System.Windows.Data;
using Microsoft.Phone.Shell;

namespace Milkman
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public static bool IsLoading = true;

        public SettingsPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(SettingsPage_Loaded);

            AppSettings settings = new AppSettings();

            Binding binding;

            binding = new Binding("AutomaticSyncEnabled");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = settings;
            this.togAutomaticSync.SetBinding(ToggleSwitch.IsCheckedProperty, binding);

            binding = new Binding("LocationServiceEnabled");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = settings;
            this.togLocationService.SetBinding(ToggleSwitch.IsCheckedProperty, binding);

            binding = new Binding("TaskRemindersEnabled");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = settings;
            this.togTaskReminders.SetBinding(ToggleSwitch.IsCheckedProperty, binding);

            binding = new Binding("LightThemeEnabled");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = settings;
            this.togLightTheme.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            IsLoading = false;
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = "On";

            if (target == this.togLightTheme &&
                IsLoading == false)
            {
                MessageBox.Show("Your changes to the theme will take effect the next time you launch Milkman.", "Settings", MessageBoxButton.OK);
            }
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = "Off";

            if (target == this.togLightTheme)
            {
                MessageBox.Show("Your changes to the theme will take effect the next time you launch Milkman.", "Settings", MessageBoxButton.OK);
            }
        }
    }
}
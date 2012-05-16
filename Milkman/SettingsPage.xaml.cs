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
        public SettingsPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(SettingsPage_Loaded);

            AppSettings settings = new AppSettings();

            Binding binding;

            try
            {
                binding = new Binding("AutomaticSyncEnabled");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.togAutomaticSync.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while loading your settings. You may want to reload this page to ensure that your settings are loaded correctly.", "Error", MessageBoxButton.OK);
            }

            try
            {
                binding = new Binding("LightThemeEnabled");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.togLightTheme.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while loading your settings. You may want to reload this page to ensure that your settings are loaded correctly.", "Error", MessageBoxButton.OK);
            }

            try
            {
                binding = new Binding("TaskRemindersEnabled");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.lstTaskReminders.SetBinding(ListPicker.SelectedIndexProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while loading your settings. You may want to reload this page to ensure that your settings are loaded correctly.", "Error", MessageBoxButton.OK);
            }

            try
            {
                binding = new Binding("LocationServiceEnabled");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.lstLocationService.SetBinding(ListPicker.SelectedIndexProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong while loading your settings. You may want to reload this page to ensure that your settings are loaded correctly.", "Error", MessageBoxButton.OK);
            }
        }

        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            GlobalLoading.Instance.IsLoading = false;
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = "On";

            if (target == this.togLightTheme &&
                GlobalLoading.Instance.IsLoading == false)
            {
                MessageBox.Show("Your changes to the theme will take effect the next time you launch Milkman.", "Settings", MessageBoxButton.OK);
            }
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = "Off";

            if (target == this.togLightTheme &&
                GlobalLoading.Instance.IsLoading == false)
            {
                MessageBox.Show("Your changes to the theme will take effect the next time you launch Milkman.", "Settings", MessageBoxButton.OK);
            }
        }
    }
}
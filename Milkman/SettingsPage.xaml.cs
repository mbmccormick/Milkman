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
        }

        private void SettingsPage_Loaded(object sender, EventArgs e)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.Loading);

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
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);
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
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);
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
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);

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
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);

            }

            this.togAutomaticSync.Checked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Checked);
            this.togAutomaticSync.Unchecked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Unchecked);

            this.togLightTheme.Checked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Checked);
            this.togLightTheme.Unchecked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Unchecked);

            GlobalLoading.Instance.IsLoading = false;
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = Strings.On;

            if (target == this.togLightTheme &&
                GlobalLoading.Instance.IsLoading == false)
            {
                MessageBox.Show(Strings.SettingsThemeDialog, Strings.SettingsThemeDialogTitle, MessageBoxButton.OK);
            }
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = Strings.Off;

            if (target == this.togLightTheme &&
                GlobalLoading.Instance.IsLoading == false)
            {
                MessageBox.Show(Strings.SettingsThemeDialog, Strings.SettingsThemeDialogTitle, MessageBoxButton.OK);
            }
        }
    }
}
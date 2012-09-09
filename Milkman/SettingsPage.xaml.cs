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
using IronCow.Resources;

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
                binding = new Binding("LocationRemindersEnabled");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.togLocationReminders.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);
            }

            try
            {
                binding = new Binding("NearbyRadius");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.lstNearbyRadius.SetBinding(ListPicker.SelectedIndexProperty, binding);
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

            this.togLocationReminders.Checked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Checked);
            this.togLocationReminders.Unchecked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Unchecked);

            GlobalLoading.Instance.IsLoading = false;
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = Strings.On;
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = Strings.Off;
        }
    }
}
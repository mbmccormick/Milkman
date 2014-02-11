using IronCow.Resources;
using Microsoft.Phone.Controls;
using Milkman.Common;
using System;
using System.Windows;
using System.Windows.Data;

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
                binding = new Binding("AddTaskDialogEnabled");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.togAddTaskDialog.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);
            }

            try
            {
                binding = new Binding("IgnorePriorityEnabled");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.togIgnorePriority.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);
            }

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

            try
            {
                binding = new Binding("LiveTileCounter");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = settings;
                this.lstLiveTileCounter.SetBinding(ListPicker.SelectedIndexProperty, binding);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Strings.SettingsErrorDialog, Strings.SettingsErrorDialogTitle, MessageBoxButton.OK);
            }

            this.togAddTaskDialog.Checked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Checked);
            this.togAddTaskDialog.Unchecked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Unchecked);
            
            this.togIgnorePriority.Checked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Checked);
            this.togIgnorePriority.Unchecked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Unchecked);

            this.togLocationReminders.Checked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Checked);
            this.togLocationReminders.Unchecked += new EventHandler<RoutedEventArgs>(ToggleSwitch_Unchecked);

            this.lstNearbyRadius.IsEnabled = this.togLocationReminders.IsChecked.Value;

            GlobalLoading.Instance.IsLoading = false;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = Strings.On;

            this.lstNearbyRadius.IsEnabled = this.togLocationReminders.IsChecked.Value;
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleSwitch target = (ToggleSwitch)sender;
            target.Content = Strings.Off;

            this.lstNearbyRadius.IsEnabled = this.togLocationReminders.IsChecked.Value;
        }

        private void lstTaskReminders_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                this.scvLayout.UpdateLayout();
                this.scvLayout.ScrollToVerticalOffset(this.scvLayout.ScrollableHeight);
            }
            catch (Exception ex)
            {
                // do nothing
            }
        }

        private void lstLiveTileCounter_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                this.scvLayout.UpdateLayout();
                this.scvLayout.ScrollToVerticalOffset(this.scvLayout.ScrollableHeight);
            }
            catch (Exception ex)
            {
                // do nothing
            }
        }
    }
}

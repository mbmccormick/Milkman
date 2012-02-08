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

namespace Milkman
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        public SettingsPage()
        {
            InitializeComponent();

            AppSettings settings = new AppSettings();

            Binding binding;

            binding = new Binding("BackgroundWorkerEnabled");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = settings;
            this.togBackgroundWorker.SetBinding(ToggleSwitch.IsCheckedProperty, binding);

            binding = new Binding("LocationNotificationsEnabled");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = settings;
            this.togLocationService.SetBinding(ToggleSwitch.IsCheckedProperty, binding);

            binding = new Binding("TaskRemindersEnabled");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = settings;
            this.togTaskReminders.SetBinding(ToggleSwitch.IsCheckedProperty, binding);
        }
    }
}
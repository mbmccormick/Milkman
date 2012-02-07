﻿using System;
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
using System.Collections.ObjectModel;
using Milkman.Common;
using IronCow;
using System.ComponentModel;
using Microsoft.Phone.Shell;

namespace Milkman
{
    public partial class TaskDetailsPage : PhoneApplicationPage
    {
        #region IsLoading Property

        public static bool sReload = true;

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(MainPage),
                new PropertyMetadata((bool)false));

        public bool IsLoading
        {
            get
            {
                return (bool)GetValue(IsLoadingProperty);
            }

            set
            {
                try
                {
                    SetValue(IsLoadingProperty, value);
                    if (progressIndicator != null)
                        progressIndicator.IsIndeterminate = value;
                }
                catch (Exception ex)
                {
                }
            }
        }

        #endregion

        #region Task Property

        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register("Task", typeof(Task), typeof(TaskDetailsPage), new PropertyMetadata(new Task()));

        private Task CurrentTask
        {
            get { return (Task)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        #endregion

        #region Construction and Navigation

        ProgressIndicator progressIndicator;

        public TaskDetailsPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(TaskDetailsPage_Loaded);
        }

        private void TaskDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            progressIndicator = new ProgressIndicator();
            progressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator = progressIndicator;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            ReloadTask();
        }

        private void ReloadTask()
        {
            string id;

            if (NavigationContext.QueryString.TryGetValue("id", out id))
            {
                CurrentTask = App.RtmClient.GetTask(id);
            }
        }

        #endregion

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to mark this task as complete?", "Complete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !IsLoading)
                {
                    IsLoading = true;

                    CurrentTask.Complete(() =>
                    {
                        App.RtmClient.CacheTasks(() =>
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                IsLoading = false;

                                if (this.NavigationService.CanGoBack)
                                    this.NavigationService.GoBack();
                                else
                                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                            });
                        });
                    });
                }
            }
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to postpone this task?", "Postpone", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !IsLoading)
                {
                    IsLoading = true;

                    CurrentTask.Postpone(() =>
                    {
                        App.RtmClient.CacheTasks(() =>
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                ReloadTask();
                                IsLoading = false;                                
                            });
                        });
                    });
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this task?", "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !IsLoading)
                {
                    IsLoading = true;

                    CurrentTask.Delete(() =>
                    {
                        App.RtmClient.CacheTasks(() =>
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                IsLoading = false;

                                if (this.NavigationService.CanGoBack)
                                    this.NavigationService.GoBack();
                                else
                                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                            });
                        });
                    });
                }
            }
        }
    }
}
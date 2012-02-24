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
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(TaskDetailsPage),
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

        ApplicationBarIconButton complete;
        ApplicationBarIconButton postpone;
        ApplicationBarIconButton edit;
        ApplicationBarIconButton delete;
        ApplicationBarIconButton add;

        public TaskDetailsPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(TaskDetailsPage_Loaded);

            complete = new ApplicationBarIconButton();
            complete.IconUri = new Uri("/Resources/complete.png", UriKind.RelativeOrAbsolute);
            complete.Text = "complete";
            complete.Click += btnComplete_Click;

            postpone = new ApplicationBarIconButton();
            postpone.IconUri = new Uri("/Resources/postpone.png", UriKind.RelativeOrAbsolute);
            postpone.Text = "postpone";
            postpone.Click += btnPostpone_Click;

            edit = new ApplicationBarIconButton();
            edit.IconUri = new Uri("/Resources/edit.png", UriKind.RelativeOrAbsolute);
            edit.Text = "edit";
            edit.Click += btnEdit_Click;

            delete = new ApplicationBarIconButton();
            delete.IconUri = new Uri("/Resources/delete.png", UriKind.RelativeOrAbsolute);
            delete.Text = "delete";
            delete.Click += btnDelete_Click;

            add = new ApplicationBarIconButton();
            add.IconUri = new Uri("/Resources/add.png", UriKind.RelativeOrAbsolute);
            add.Text = "add";
            add.Click += btnAdd_Click;
        }

        private void TaskDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentTask != null)
            {
                if (CurrentTask.Priority == TaskPriority.One)
                    this.txtName.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 82, 0));
                else if (CurrentTask.Priority == TaskPriority.Two)
                    this.txtName.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 96, 191));
                else if (CurrentTask.Priority == TaskPriority.Three)
                    this.txtName.Foreground = new SolidColorBrush(Color.FromArgb(255, 53, 154, 255));
                else
                    this.txtName.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            progressIndicator = new ProgressIndicator();
            progressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator = progressIndicator;

            IsLoading = true;
            
            if (e.IsNavigationInitiator)
            {
                ReloadTask();
            }
            else
            {
                App.RtmClient.SyncEverything(() =>
                {
                    ReloadTask();
                });
            }

            base.OnNavigatedTo(e);
        }

        #endregion

        #region Loading Data

        private void ReloadTask()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    CurrentTask = App.RtmClient.GetTask(id);
                    if (CurrentTask.Notes.Count == 0)
                        this.txtEmpty.Visibility = System.Windows.Visibility.Visible;
                    else
                        this.txtEmpty.Visibility = System.Windows.Visibility.Collapsed;
                }

                IsLoading = false;
            });
        }

        #endregion

        #region Event Handlers

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to mark this task as complete?", "Complete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !IsLoading)
                {
                    CompleteTask(CurrentTask);
                }
            }
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to postpone this task?", "Postpone", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !IsLoading)
                {
                    PostponeTask(CurrentTask);
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.NavigationService.Navigate(new Uri("/EditTaskPage.xaml?id=" + CurrentTask.Id, UriKind.Relative));
            });
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this task?", "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !IsLoading)
                {
                    DeleteTask(CurrentTask);
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.NavigationService.Navigate(new Uri("/AddNotePage.xaml?task=" + CurrentTask.Id, UriKind.Relative));
            });
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (IsLoading) return;

            TaskNote item = ((FrameworkElement)sender).DataContext as TaskNote;

            if (item != null)
                this.NavigationService.Navigate(new Uri("/EditNotePage.xaml?task=" + CurrentTask.Id + "&id=" + item.Id, UriKind.Relative));
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            while (ApplicationBar.Buttons.Count > 0)
            {
                ApplicationBar.Buttons.RemoveAt(0);
            }

            if (this.pivLayout.SelectedIndex == 0)
            {
                ApplicationBar.Buttons.Add(complete);
                ApplicationBar.Buttons.Add(postpone);
                ApplicationBar.Buttons.Add(edit);
                ApplicationBar.Buttons.Add(delete);
            }
            else if (this.pivLayout.SelectedIndex == 1)
            {
                ApplicationBar.Buttons.Add(add);
            }
        }

        #endregion

        #region Task Methods

        private void CompleteTask(Task data)
        {
            IsLoading = true;
            data.Complete(() =>
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

        private void PostponeTask(Task data)
        {
            IsLoading = true;
            data.Postpone(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        IsLoading = false;
                        ReloadTask();
                    });
                });
            });
        }

        private void DeleteTask(Task data)
        {
            IsLoading = true;
            data.Delete(() =>
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

        private void DeleteNote(TaskNote data)
        {
            IsLoading = true;
            data.Delete(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        IsLoading = false;
                        ReloadTask();
                    });
                });
            });
        }

        #endregion
    }
}

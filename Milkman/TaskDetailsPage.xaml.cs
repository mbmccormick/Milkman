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
using IronCow.Resources;
using System.ComponentModel;
using Microsoft.Phone.Shell;

namespace Milkman
{
    public partial class TaskDetailsPage : PhoneApplicationPage
    {
        public static bool sReload = false;

        public System.Windows.Navigation.NavigationEventArgs navigationArgs = null;

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

        ApplicationBarIconButton complete;
        ApplicationBarIconButton postpone;
        ApplicationBarIconButton edit;
        ApplicationBarIconButton delete;
        ApplicationBarIconButton add;

        public TaskDetailsPage()
        {
            InitializeComponent();

            this.Loaded += TaskDetailsPage_Loaded;

            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            this.BuildApplicationBar();
        }

        private void TaskDetailsPage_Loaded(object sender, EventArgs e)
        {
            if (navigationArgs.IsNavigationInitiator == false)
            {
                LittleWatson.CheckForPreviousException(true);

                App.RtmClient.SyncEverything(() =>
                {
                    LoadData();
                });
            }
            else
            {
                LoadData();
            }
        }

        private void BuildApplicationBar()
        {
            complete = new ApplicationBarIconButton();
            complete.IconUri = new Uri("/Resources/complete.png", UriKind.RelativeOrAbsolute);
            complete.Text = Strings.CompleteMenuLower;
            complete.Click += btnComplete_Click;

            postpone = new ApplicationBarIconButton();
            postpone.IconUri = new Uri("/Resources/postpone.png", UriKind.RelativeOrAbsolute);
            postpone.Text = Strings.PostponeMenuLower;
            postpone.Click += btnPostpone_Click;

            edit = new ApplicationBarIconButton();
            edit.IconUri = new Uri("/Resources/edit.png", UriKind.RelativeOrAbsolute);
            edit.Text = Strings.EditMenuLower;
            edit.Click += btnEdit_Click;

            delete = new ApplicationBarIconButton();
            delete.IconUri = new Uri("/Resources/delete.png", UriKind.RelativeOrAbsolute);
            delete.Text = Strings.DeleteMenuLower;
            delete.Click += btnDelete_Click;

            add = new ApplicationBarIconButton();
            add.IconUri = new Uri("/Resources/add.png", UriKind.RelativeOrAbsolute);
            add.Text = Strings.AddMenuLower;
            add.Click += btnAdd_Click;

            // build application bar
            ApplicationBar.Buttons.Add(complete);
            ApplicationBar.Buttons.Add(postpone);
            ApplicationBar.Buttons.Add(edit);
            ApplicationBar.Buttons.Add(delete);

            // disable buttons when working offline
            if (App.RtmClient.Syncing == false)
            {
                complete.IsEnabled = false;
                postpone.IsEnabled = false;
                edit.IsEnabled = false;
                delete.IsEnabled = false;
                add.IsEnabled = false;
            }
        }

        private void App_UnhandledExceptionHandled(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                GlobalLoading.Instance.IsLoading = false;
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.Loading);

            navigationArgs = e;

            base.OnNavigatedTo(e);
        }

        #endregion

        #region Loading Data

        private void LoadData()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    CurrentTask = App.RtmClient.GetTask(id);

                    ToggleLoadingText();
                    ToggleEmptyText();
                }

                GlobalLoading.Instance.IsLoading = false;
            });
        }

        private void ToggleLoadingText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.txtLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.grdTaskDetails.Visibility = System.Windows.Visibility.Visible;
            });
        }

        private void ToggleEmptyText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (CurrentTask != null &&
                    CurrentTask.Notes.Count == 0)
                    this.txtEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtEmpty.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        #endregion

        #region Event Handlers

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Strings.CompleteDialog, Strings.CompleteDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !GlobalLoading.Instance.IsLoading)
                {
                    CompleteTask(CurrentTask);
                }
            }
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Strings.PostponeDialog, Strings.PostponeDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !GlobalLoading.Instance.IsLoading)
                {
                    PostponeTask(CurrentTask);
                }
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/EditTaskPage.xaml?id=" + CurrentTask.Id, UriKind.Relative));
            });
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Strings.DeleteTaskDialog, Strings.DeleteTaskDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                if (CurrentTask != null && !GlobalLoading.Instance.IsLoading)
                {
                    DeleteTask(CurrentTask);
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/AddNotePage.xaml?task=" + CurrentTask.Id, UriKind.Relative));
            });
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (App.RtmClient.Syncing == false) return;

            if (GlobalLoading.Instance.IsLoading) return;

            TaskNote item = ((FrameworkElement)sender).DataContext as TaskNote;

            if (item != null)
                NavigationService.Navigate(new Uri("/EditNotePage.xaml?task=" + CurrentTask.Id + "&id=" + item.Id, UriKind.Relative));
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
            GlobalLoading.Instance.IsLoadingText(Strings.CompletingTask);
            data.Complete(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;

                        if (NavigationService.CanGoBack)
                            NavigationService.GoBack();
                        else
                            NavigationService.Navigate(new Uri("/MainPage.xaml?IsFirstRun=true", UriKind.Relative));
                    });
                });
            });
        }

        private void PostponeTask(Task data)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.PostponingTask);
            data.Postpone(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;
                        LoadData();
                    });
                });
            });
        }

        private void DeleteTask(Task data)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.DeletingTask);
            data.Delete(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;

                        if (NavigationService.CanGoBack)
                            NavigationService.GoBack();
                        else
                            NavigationService.Navigate(new Uri("/MainPage.xaml?IsFirstRun=true", UriKind.Relative));
                    });
                });
            });
        }

        private void DeleteNote(TaskNote data)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.DeletingNote);
            data.Delete(() =>
            {
                App.RtmClient.CacheTasks(() =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;
                        LoadData();
                    });
                });
            });
        }

        #endregion
    }
}

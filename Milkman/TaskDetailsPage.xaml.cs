using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Milkman.Common;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Milkman
{
    public partial class TaskDetailsPage : PhoneApplicationPage
    {
        public static bool sReload = false;
        public static bool sFirstLaunch = false;

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

            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            this.BuildApplicationBar();
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

            if (e.IsNavigationInitiator == false)
            {
                LittleWatson.CheckForPreviousException(true);

                sFirstLaunch = true;
            }

            LoadData();

            if (e.IsNavigationInitiator == false)
            {
                App.CheckTimezone();
            }

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

                ShowLastUpdatedStatus();
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

        private void ShowLastUpdatedStatus()
        {
            if (sFirstLaunch == true)
            {
                int minutes = Convert.ToInt32((DateTime.Now - App.LastUpdated).TotalMinutes);

                if (minutes < 2)
                    GlobalLoading.Instance.StatusText(Strings.UpToDate);
                else if (minutes > 60)
                    GlobalLoading.Instance.StatusText(Strings.LastUpdated + " " + Strings.OverAnHourAgo);
                else
                    GlobalLoading.Instance.StatusText(Strings.LastUpdated + " " + minutes + " " + Strings.MinutesAgo);

                System.ComponentModel.BackgroundWorker b = new System.ComponentModel.BackgroundWorker();
                b.DoWork += (s, e) =>
                {
                    System.Threading.Thread.Sleep(4000);

                    SmartDispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.ClearStatusText();
                    });
                };

                sFirstLaunch = false;

                b.RunWorkerAsync();
            }
        }

        #endregion

        #region Event Handlers

        private void btnComplete_Click(object sender, EventArgs e)
        {
            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.CompleteDialogTitle,
                Message = Strings.CompleteDialog,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        if (CurrentTask != null && !GlobalLoading.Instance.IsLoading)
                        {
                            CompleteTask(CurrentTask, false);
                        }

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.PostponeDialogTitle,
                Message = Strings.PostponeDialog,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        if (CurrentTask != null && !GlobalLoading.Instance.IsLoading)
                        {
                            PostponeTask(CurrentTask, false);
                        }

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
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
            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.DeleteTaskDialogTitle,
                Message = Strings.DeleteTaskDialog,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        if (CurrentTask != null && !GlobalLoading.Instance.IsLoading)
                        {
                            DeleteTask(CurrentTask, false);
                        }

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
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

        private TaskNote MostRecentTaskNoteClick
        {
            get;
            set;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement frameworkElement = (FrameworkElement)e.OriginalSource;
                if (frameworkElement.DataContext is TaskNote)
                {
                    MostRecentTaskNoteClick = (TaskNote)frameworkElement.DataContext;
                }
            }

            base.OnMouseLeftButtonDown(e);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem target = (MenuItem)sender;
            ContextMenu parent = (ContextMenu)target.Parent;

            if (target.Header.ToString() == Strings.DeleteMenuLower)
            {
                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Caption = Strings.DeleteNoteDialogTitle,
                    Message = Strings.DeleteNoteDialog,
                    LeftButtonContent = Strings.YesLower,
                    RightButtonContent = Strings.NoLower,
                    IsFullScreen = false
                };

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            DeleteNote(MostRecentTaskNoteClick);

                            break;
                        default:
                            break;
                    }
                };

                messageBox.Show();
            }
        }

        #endregion

        #region Task Methods

        private void CompleteTask(Task data, bool isMultiple)
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

        private void PostponeTask(Task data, bool isMultiple)
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

        private void DeleteTask(Task data, bool isMultiple)
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

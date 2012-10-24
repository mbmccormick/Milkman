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
using Microsoft.Phone.Tasks;
using System.Device.Location;

namespace Milkman
{
    public partial class TaskListByLocationPage : PhoneApplicationPage
    {
        public static bool sReload = true;

        #region Task List Property

        public static readonly DependencyProperty AllTasksProperty =
               DependencyProperty.Register("AllTasks", typeof(ObservableCollection<Task>), typeof(TaskListByLocationPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> AllTasks
        {
            get { return (ObservableCollection<Task>)GetValue(AllTasksProperty); }
            set { SetValue(AllTasksProperty, value); }
        }

        #endregion

        #region Construction and Navigation

        ApplicationBarIconButton dashboard;
        ApplicationBarIconButton add;
        ApplicationBarIconButton select;
        ApplicationBarIconButton sync;
        ApplicationBarIconButton complete;
        ApplicationBarIconButton postpone;
        ApplicationBarIconButton delete;

        ApplicationBarMenuItem settings;
        ApplicationBarMenuItem about;
        ApplicationBarMenuItem feedback;
        ApplicationBarMenuItem donate;
        ApplicationBarMenuItem signOut;

        GeoCoordinateWatcher _watcher;

        public TaskListByLocationPage()
        {
            InitializeComponent();

            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);
            App.SyncingDisabled += new EventHandler<EventArgs>(App_SyncingDisabled);

            TiltEffect.TiltableItems.Add(typeof(MultiselectItem));

            this.BuildApplicationBar();

            _watcher = new GeoCoordinateWatcher();
            _watcher.Start();
        }

        private void BuildApplicationBar()
        {
            dashboard = new ApplicationBarIconButton();
            dashboard.IconUri = new Uri("/Resources/dashboard.png", UriKind.RelativeOrAbsolute);
            dashboard.Text = Strings.DashboardMenuLower;
            dashboard.Click += btnDashboard_Click;

            add = new ApplicationBarIconButton();
            add.IconUri = new Uri("/Resources/add.png", UriKind.RelativeOrAbsolute);
            add.Text = Strings.AddMenuLower;
            add.Click += btnAdd_Click;

            select = new ApplicationBarIconButton();
            select.IconUri = new Uri("/Resources/select.png", UriKind.RelativeOrAbsolute);
            select.Text = Strings.SelectMenuLower;
            select.Click += btnSelect_Click;

            sync = new ApplicationBarIconButton();
            sync.IconUri = new Uri("/Resources/retry.png", UriKind.RelativeOrAbsolute);
            sync.Text = Strings.SyncMenuLower;
            sync.Click += btnSync_Click;

            complete = new ApplicationBarIconButton();
            complete.IconUri = new Uri("/Resources/complete.png", UriKind.RelativeOrAbsolute);
            complete.Text = Strings.CompleteMenuLower;
            complete.Click += btnComplete_Click;

            postpone = new ApplicationBarIconButton();
            postpone.IconUri = new Uri("/Resources/postpone.png", UriKind.RelativeOrAbsolute);
            postpone.Text = Strings.PostponeMenuLower;
            postpone.Click += btnPostpone_Click;

            delete = new ApplicationBarIconButton();
            delete.IconUri = new Uri("/Resources/delete.png", UriKind.RelativeOrAbsolute);
            delete.Text = Strings.DeleteMenuLower;
            delete.Click += btnDelete_Click;

            settings = new ApplicationBarMenuItem();
            settings.Text = Strings.SettingsMenuLower;
            settings.Click += mnuSettings_Click;

            about = new ApplicationBarMenuItem();
            about.Text = Strings.AboutMenuLower;
            about.Click += mnuAbout_Click;

            feedback = new ApplicationBarMenuItem();
            feedback.Text = Strings.FeedbackMenuLower;
            feedback.Click += mnuFeedback_Click;

            donate = new ApplicationBarMenuItem();
            donate.Text = Strings.DonateMenuLower;
            donate.Click += mnuDonate_Click;

            signOut = new ApplicationBarMenuItem();
            signOut.Text = Strings.SignOutMenuLower;
            signOut.Click += mnuSignOut_Click;

            // build application bar
            ApplicationBar.Buttons.Add(dashboard);
            ApplicationBar.Buttons.Add(add);
            ApplicationBar.Buttons.Add(select);
            ApplicationBar.Buttons.Add(sync);

            ApplicationBar.MenuItems.Add(settings);
            ApplicationBar.MenuItems.Add(about);
            ApplicationBar.MenuItems.Add(feedback);
            ApplicationBar.MenuItems.Add(donate);
            ApplicationBar.MenuItems.Add(signOut);

            // disable buttons when working offline
            if (App.RtmClient.Syncing == false)
            {
                add.IsEnabled = false;
                sync.IsEnabled = false;
                complete.IsEnabled = false;
                postpone.IsEnabled = false;
                delete.IsEnabled = false;
            }
        }

        private void App_UnhandledExceptionHandled(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                GlobalLoading.Instance.IsLoading = false;
            });
        }

        private void App_SyncingDisabled(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                // disable buttons when working offline
                if (App.RtmClient.Syncing == false)
                {
                    add.IsEnabled = false;
                    sync.IsEnabled = false;
                    complete.IsEnabled = false;
                    postpone.IsEnabled = false;
                    delete.IsEnabled = false;
                }
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.Loading);

            if (e.IsNavigationInitiator == false)
            {
                LittleWatson.CheckForPreviousException(true);

                SyncData();
            }
            else
            {
                LoadData();
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (this.dlgAddTask.IsOpen)
            {
                this.dlgAddTask.Close();
                e.Cancel = true;

                this.ApplicationBar.IsVisible = true;
            }

            if (this.lstAll.IsSelectionEnabled)
            {
                this.lstAll.IsSelectionEnabled = false;
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        #endregion

        #region Loading Data

        private void SyncData()
        {
            System.ComponentModel.BackgroundWorker b = new System.ComponentModel.BackgroundWorker();
            b.DoWork += (s, e) =>
            {
                if (sReload)
                {
                    if (!string.IsNullOrEmpty(App.RtmClient.AuthToken))
                    {
                        SmartDispatcher.BeginInvoke(() =>
                        {
                            GlobalLoading.Instance.IsLoadingText(Strings.SyncingTasks);
                        });

                        App.RtmClient.SyncEverything(() =>
                        {
                            LoadData();
                        });
                    }
                    else
                    {
                        Login();
                    }
                }
                else
                {
                    SmartDispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;
                    });
                }

                sReload = false;
            };

            b.RunWorkerAsync();
        }

        private void LoadData()
        {
            System.ComponentModel.BackgroundWorker b = new System.ComponentModel.BackgroundWorker();
            b.DoWork += (s, e) =>
            {
                LoadDataInBackground();
                NotificationsManager.SetupNotifications(_watcher.Position.Location);
            };
            b.RunWorkerAsync();
        }

        private void LoadDataInBackground()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                GlobalLoading.Instance.IsLoadingText(Strings.SyncingTasks);

                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    var tempAllTasks = new SortableObservableCollection<Task>();

                    AppSettings settings = new AppSettings();

                    double radius;
                    if (settings.NearbyRadius == 0)
                        radius = 1.0;
                    else if (settings.NearbyRadius == 1)
                        radius = 2.0;
                    else if (settings.NearbyRadius == 2)
                        radius = 5.0;
                    else if (settings.NearbyRadius == 3)
                        radius = 10.0;
                    else if (settings.NearbyRadius == 3)
                        radius = 20.0;
                    else
                        radius = 0.0;

                    if (settings.IgnorePriorityEnabled == true)
                    {
                        foreach (Task t in App.RtmClient.GetNearbyTasks(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, radius).OrderBy(z => z.DueDateTime))
                        {
                            tempAllTasks.Add(t);
                        }
                    }
                    else
                    {
                        foreach (Task t in App.RtmClient.GetNearbyTasks(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, radius))
                        {
                            tempAllTasks.Add(t);
                        }
                    }

                    AllTasks = tempAllTasks;

                    ToggleLoadingText();
                    ToggleEmptyText();

                    GlobalLoading.Instance.IsLoading = false;
                }
            });
        }

        private void ToggleLoadingText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.txtAllLoading.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        private void ToggleEmptyText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (AllTasks.Count == 0)
                    this.txtAllEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtAllEmpty.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        public void Login()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/WelcomePage.xaml", UriKind.Relative));
            });
        }

        #endregion

        #region Event Handlers

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
                else
                    NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            });
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AppSettings settings = new AppSettings();

            if (settings.AddTaskDialogEnabled == true)
            {
                this.dlgAddTask.Open();

                this.ApplicationBar.IsVisible = false;
            }
            else
            {
                NavigationService.Navigate(new Uri("/AddTaskPage.xaml"));
            }
        }

        private void dlgAddTask_Submit(object sender, SubmitEventArgs e)
        {
            AddTask(e.Text);

            this.ApplicationBar.IsVisible = true;
        }

        private void dlgAddTask_Cancel(object sender, EventArgs e)
        {
            this.ApplicationBar.IsVisible = true;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            this.lstAll.IsSelectionEnabled = true;
        }

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            string messageBoxText;
            if (this.lstAll.SelectedItems.Count == 1)
                messageBoxText = Strings.CompleteTaskSingleDialog;
            else
                messageBoxText = Strings.CompleteTaskPluralDialog;

            if (MessageBox.Show(messageBoxText, Strings.CompleteDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (this.lstAll.SelectedItems.Count > 0)
                {
                    CompleteTask((Task)this.lstAll.SelectedItems[0]);
                    this.lstAll.SelectedItems.RemoveAt(0);
                }

                this.lstAll.IsSelectionEnabled = false;
            }
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            string messageBoxText;
            if (this.lstAll.SelectedItems.Count == 1)
                messageBoxText = Strings.PostponeTaskSingleDialog;
            else
                messageBoxText = Strings.PostponeTaskPluralDialog;

            if (MessageBox.Show(messageBoxText, Strings.PostponeDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (this.lstAll.SelectedItems.Count > 0)
                {
                    PostponeTask((Task)this.lstAll.SelectedItems[0]);
                    this.lstAll.SelectedItems.RemoveAt(0);
                }

                this.lstAll.IsSelectionEnabled = false;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            string messageBoxText;
            if (this.lstAll.SelectedItems.Count == 1)
                messageBoxText = Strings.DeleteTaskSingleDialog;
            else
                messageBoxText = Strings.DeleteTaskPluralDialog;

            if (MessageBox.Show(messageBoxText, Strings.DeleteTaskDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (this.lstAll.SelectedItems.Count > 0)
                {
                    DeleteTask((Task)this.lstAll.SelectedItems[0]);
                    this.lstAll.SelectedItems.RemoveAt(0);
                }

                this.lstAll.IsSelectionEnabled = false;
            }
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            sReload = true;
            SyncData();
        }

        private void MultiselectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MultiselectList target = (MultiselectList)sender;
            ApplicationBarIconButton i = (ApplicationBarIconButton)ApplicationBar.Buttons[0]; // complete
            ApplicationBarIconButton j = (ApplicationBarIconButton)ApplicationBar.Buttons[1]; // postpone
            ApplicationBarIconButton k = (ApplicationBarIconButton)ApplicationBar.Buttons[2]; // delete

            if (target.IsSelectionEnabled)
            {
                if (target.SelectedItems.Count > 0)
                {
                    i.IsEnabled = true;
                    j.IsEnabled = true;
                    k.IsEnabled = true;
                }
                else
                {
                    i.IsEnabled = false;
                    j.IsEnabled = false;
                    k.IsEnabled = false;
                }
            }
            else
            {
                i.IsEnabled = true;
                j.IsEnabled = true;
                k.IsEnabled = true;
            }

            // disable buttons when working offline
            if (App.RtmClient.Syncing == false)
            {
                add.IsEnabled = false;
                sync.IsEnabled = false;
                complete.IsEnabled = false;
                postpone.IsEnabled = false;
                delete.IsEnabled = false;
            }
        }

        private void MultiselectList_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            MultiselectList target = (MultiselectList)sender;

            while (ApplicationBar.Buttons.Count > 0)
            {
                ApplicationBar.Buttons.RemoveAt(0);
            }

            while (ApplicationBar.MenuItems.Count > 0)
            {
                ApplicationBar.MenuItems.RemoveAt(0);
            }

            if ((bool)e.NewValue)
            {
                ApplicationBar.Buttons.Add(complete);
                ApplicationBarIconButton i = (ApplicationBarIconButton)ApplicationBar.Buttons[0];
                i.IsEnabled = false;

                ApplicationBar.Buttons.Add(postpone);
                ApplicationBarIconButton j = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                j.IsEnabled = false;

                ApplicationBar.Buttons.Add(delete);
                ApplicationBarIconButton k = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
                k.IsEnabled = false;
            }
            else
            {
                ApplicationBar.Buttons.Add(dashboard);
                ApplicationBar.Buttons.Add(add);
                ApplicationBar.Buttons.Add(select);
                ApplicationBar.Buttons.Add(sync);

                ApplicationBar.MenuItems.Add(settings);
                ApplicationBar.MenuItems.Add(about);
                ApplicationBar.MenuItems.Add(feedback);
                ApplicationBar.MenuItems.Add(donate);
                ApplicationBar.MenuItems.Add(signOut);
            }

            Thickness margin = target.Margin;

            if (target.IsSelectionEnabled)
                margin.Left = margin.Left - 12;
            else
                margin.Left = margin.Left + 12;

            target.Margin = margin;

            // disable buttons when working offline
            if (App.RtmClient.Syncing == false)
            {
                add.IsEnabled = false;
                sync.IsEnabled = false;
                complete.IsEnabled = false;
                postpone.IsEnabled = false;
                delete.IsEnabled = false;
            }
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            Task item = ((FrameworkElement)sender).DataContext as Task;

            if (item != null)
                NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative));
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
            });
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
            });
        }

        private void mnuFeedback_Click(object sender, EventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.To = "milkman@mbmccormick.com";
            emailComposeTask.Subject = "Milkman Feedback";
            emailComposeTask.Body = "Version " + App.ExtendedVersionNumber + " (" + App.PlatformVersionNumber + ")\n\n";
            emailComposeTask.Show();
        }

        private void mnuDonate_Click(object sender, EventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();

            webBrowserTask.Uri = new Uri("http://mbmccormick.com/donate/", UriKind.Absolute);
            webBrowserTask.Show();
        }

        private void mnuSignOut_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(Strings.SignOutDialog, Strings.SignOutDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                App.DeleteData();
                Login();
            }
        }

        private Task MostRecentTaskClick
        {
            get;
            set;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement frameworkElement = (FrameworkElement)e.OriginalSource;
                if (frameworkElement.DataContext is Task)
                {
                    MostRecentTaskClick = (Task)frameworkElement.DataContext;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem target = (MenuItem)sender;
            ContextMenu parent = (ContextMenu)target.Parent;

            if (target.Header.ToString() == Strings.CompleteMenuLower)
            {
                if (MessageBox.Show(Strings.CompleteDialog, Strings.CompleteDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    CompleteTask(MostRecentTaskClick);
            }
            else if (target.Header.ToString() == Strings.PostponeMenuLower)
            {
                if (MessageBox.Show(Strings.PostponeDialog, Strings.PostponeDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    PostponeTask(MostRecentTaskClick);
            }
            else if (target.Header.ToString() == Strings.DeleteMenuLower)
            {
                if (MessageBox.Show(Strings.DeleteTaskDialog, Strings.DeleteTaskDialogTitle, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    DeleteTask(MostRecentTaskClick);
            }
        }

        #endregion

        #region Task Methods

        private void AddTask(string smartAddText)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.AddingTask);

            string input = smartAddText;
            if (input.Contains('#') == false)
            {
                TaskList defaultList = App.RtmClient.GetDefaultTaskList();
                if (defaultList.IsSmart == false)
                    input = input + " #" + defaultList.Name;
            }

            App.RtmClient.AddTask(input, true, null, () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    GlobalLoading.Instance.IsLoading = false;
                });

                sReload = true;
                LoadData();
            });
        }

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
                    });

                    sReload = true;
                    LoadData();
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
                    });

                    sReload = true;
                    LoadData();
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
                    });

                    sReload = true;
                    LoadData();
                });
            });
        }

        #endregion
    }
}
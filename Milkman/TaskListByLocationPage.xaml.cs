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
using Microsoft.Phone.Tasks;
using System.Device.Location;

namespace Milkman
{
    public partial class TaskListByLocationPage : PhoneApplicationPage
    {
        public static bool sReload = false;

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

        GeoCoordinateWatcher watcher;
        GeoCoordinate currentLocation;

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

        public TaskListByLocationPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(TaskDetailsPage_Loaded);
            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            TiltEffect.TiltableItems.Add(typeof(MultiselectItem));

            dashboard = new ApplicationBarIconButton();
            dashboard.IconUri = new Uri("/Resources/dashboard.png", UriKind.RelativeOrAbsolute);
            dashboard.Text = "dashboard";
            dashboard.Click += btnDashboard_Click;

            add = new ApplicationBarIconButton();
            add.IconUri = new Uri("/Resources/add.png", UriKind.RelativeOrAbsolute);
            add.Text = "add";
            add.Click += btnAdd_Click;

            select = new ApplicationBarIconButton();
            select.IconUri = new Uri("/Resources/select.png", UriKind.RelativeOrAbsolute);
            select.Text = "select";
            select.Click += btnSelect_Click;

            sync = new ApplicationBarIconButton();
            sync.IconUri = new Uri("/Resources/retry.png", UriKind.RelativeOrAbsolute);
            sync.Text = "sync";
            sync.Click += btnSync_Click;

            complete = new ApplicationBarIconButton();
            complete.IconUri = new Uri("/Resources/complete.png", UriKind.RelativeOrAbsolute);
            complete.Text = "complete";
            complete.Click += btnComplete_Click;

            postpone = new ApplicationBarIconButton();
            postpone.IconUri = new Uri("/Resources/postpone.png", UriKind.RelativeOrAbsolute);
            postpone.Text = "postpone";
            postpone.Click += btnPostpone_Click;

            delete = new ApplicationBarIconButton();
            delete.IconUri = new Uri("/Resources/delete.png", UriKind.RelativeOrAbsolute);
            delete.Text = "delete";
            delete.Click += btnDelete_Click;

            settings = new ApplicationBarMenuItem();
            settings.Text = "settings";
            settings.Click += mnuSettings_Click;

            about = new ApplicationBarMenuItem();
            about.Text = "about milkman";
            about.Click += mnuAbout_Click;

            feedback = new ApplicationBarMenuItem();
            feedback.Text = "feedback";
            feedback.Click += mnuFeedback_Click;

            donate = new ApplicationBarMenuItem();
            donate.Text = "donate";
            donate.Click += mnuDonate_Click;

            signOut = new ApplicationBarMenuItem();
            signOut.Text = "sign out";
            signOut.Click += mnuSignOut_Click;

            watcher = new GeoCoordinateWatcher();
            watcher.Start();
        }

        private void TaskDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
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
            GlobalLoading.Instance.IsLoadingText("Loading...");

            AppSettings settings = new AppSettings();

            if (e.IsNavigationInitiator &&
                sReload == false)
            {
                LoadData();
            }
            else
            {
                LittleWatson.CheckForPreviousException(true);

                if (settings.AutomaticSyncEnabled == true)
                    SyncData();

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
            }

            if (this.lstTasks.IsSelectionEnabled)
            {
                this.lstTasks.IsSelectionEnabled = false;
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
                            GlobalLoading.Instance.IsLoadingText("Syncing tasks...");
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
                NotificationsManager.SetupNotifications();
            };
            b.RunWorkerAsync();
        }

        private void LoadDataInBackground()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                GlobalLoading.Instance.IsLoadingText("Syncing tasks...");

                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    var tempAllTasks = new SortableObservableCollection<Task>();

                    if (App.RtmClient.TaskLists != null)
                    {
                        foreach (TaskList l in App.RtmClient.TaskLists)
                        {
                            if (l.Tasks != null)
                            {
                                foreach (Task t in l.Tasks)
                                {
                                    if (t.IsCompleted == true ||
                                        t.IsDeleted == true) continue;

                                    if (t.Location != null)
                                    {
                                        if (tempAllTasks.Contains(t) == false)
                                            tempAllTasks.Add(t);
                                    }
                                }
                            }
                        }
                    }

                    AllTasks = tempAllTasks;

                    if (watcher.Position != null)
                    {
                        AllTasks.OrderByDescending(t => LocationHelper.Distance(watcher.Position.Location.Latitude,
                                                                      watcher.Position.Location.Longitude,
                                                                      t.Location.Latitude,
                                                                      t.Location.Longitude));
                    }

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
                this.txtLoading.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        private void ToggleEmptyText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (AllTasks.Count == 0)
                    this.txtEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtEmpty.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        public void Login()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.NavigationService.Navigate(new Uri("/WelcomePage.xaml", UriKind.Relative));
            });
        }

        #endregion

        #region Event Handlers

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (this.NavigationService.CanGoBack)
                    this.NavigationService.GoBack();
                else
                    this.NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            });
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            this.dlgAddTask.Open();
        }

        private void dlgAddTask_Submit(object sender, SubmitEventArgs e)
        {
            AddTask(e.Text);
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            this.lstTasks.IsSelectionEnabled = true;
        }

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            string messageBoxText = null;
            if (this.lstTasks.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to mark the selected task as complete?";
            else
                messageBoxText = "Are you sure you want to mark the selected tasks as complete?";

            if (MessageBox.Show(messageBoxText, "Complete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (this.lstTasks.SelectedItems.Count > 0)
                {
                    CompleteTask((Task)this.lstTasks.SelectedItems[0]);
                    this.lstTasks.SelectedItems.RemoveAt(0);
                }

                this.lstTasks.IsSelectionEnabled = false;
            }
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            string messageBoxText = null;
            if (this.lstTasks.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to postpone the selected task?";
            else
                messageBoxText = "Are you sure you want to postpone the selected tasks?";

            if (MessageBox.Show(messageBoxText, "Postpone", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (this.lstTasks.SelectedItems.Count > 0)
                {
                    PostponeTask((Task)this.lstTasks.SelectedItems[0]);
                    this.lstTasks.SelectedItems.RemoveAt(0);
                }

                this.lstTasks.IsSelectionEnabled = false;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            string messageBoxText = null;
            if (this.lstTasks.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to delete the selected task?";
            else
                messageBoxText = "Are you sure you want to delete the selected tasks?";

            if (MessageBox.Show(messageBoxText, "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (this.lstTasks.SelectedItems.Count > 0)
                {
                    DeleteTask((Task)this.lstTasks.SelectedItems[0]);
                    this.lstTasks.SelectedItems.RemoveAt(0);
                }

                this.lstTasks.IsSelectionEnabled = false;
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
        }

        private void MultiselectList_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
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
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            Task item = ((FrameworkElement)sender).DataContext as Task;

            if (item != null)
                this.NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative));
        }

        private void TaskName_Loaded(object sender, EventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            Task task = (Task)target.DataContext;

            // set priority
            if (task.Priority == TaskPriority.One)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 82, 0));
            else if (task.Priority == TaskPriority.Two)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 96, 191));
            else if (task.Priority == TaskPriority.Three)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 53, 154, 255));
            else
                target.Foreground = (SolidColorBrush)Resources["PhoneForegroundBrush"];
        }

        private void TaskLocationName_Loaded(object sender, EventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            Task task = (Task)target.DataContext;

            double distance = LocationHelper.Distance(watcher.Position.Location.Latitude, 
                                                      watcher.Position.Location.Longitude, 
                                                      task.Location.Latitude, 
                                                      task.Location.Longitude);
            double radius;

            AppSettings settings = new AppSettings();
            if (settings.LocationServiceEnabled == 1)
                radius = 1.0;
            else if (settings.LocationServiceEnabled == 2)
                radius = 2.0;
            else if (settings.LocationServiceEnabled == 3)
                radius = 5.0;
            else
                radius = 1.0;

            // set distance
            if (distance <= radius)
                target.Foreground = (SolidColorBrush)Resources["PhoneAccentBrush"];
            else
                target.Foreground = (SolidColorBrush)Resources["PhoneSubtleBrush"];
        }

        private void TaskDistance_Loaded(object sender, EventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            Task task = (Task)target.DataContext;

            double distance = LocationHelper.Distance(watcher.Position.Location.Latitude,
                                                      watcher.Position.Location.Longitude, 
                                                      task.Location.Latitude, 
                                                      task.Location.Longitude);

            // set distance
            target.Text = String.Format("{0:0.00} miles", distance);
        }

        private void mnuSettings_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
            });
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
            });
        }

        private void mnuFeedback_Click(object sender, EventArgs e)
        {
            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.To = "milkmanwp@gmail.com";
            emailComposeTask.Subject = "Milkman Feedback";
            emailComposeTask.Body = "Version " + App.VersionNumber + "\n\n";
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
            if (MessageBox.Show("Are you sure you want to sign out of Milkman and remove all of your data?", "Sign Out", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
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

            if (target.Header.ToString() == "complete")
            {
                if (MessageBox.Show("Are you sure you want to mark this task as complete?", "Complete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    CompleteTask(MostRecentTaskClick);
            }
            else if (target.Header.ToString() == "postpone")
            {
                if (MessageBox.Show("Are you sure you want to postpone this task?", "Postpone", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    PostponeTask(MostRecentTaskClick);
            }
            else if (target.Header.ToString() == "delete")
            {
                if (MessageBox.Show("Are you sure you want to delete this task?", "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    DeleteTask(MostRecentTaskClick);
            }
        }

        #endregion

        #region Task Methods

        private void AddTask(string smartAddText)
        {
            GlobalLoading.Instance.IsLoadingText("Adding task...");

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
            GlobalLoading.Instance.IsLoadingText("Completing task...");
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
            GlobalLoading.Instance.IsLoadingText("Postponing task...");
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
            GlobalLoading.Instance.IsLoadingText("Deleting task...");
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

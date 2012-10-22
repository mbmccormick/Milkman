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
    public partial class MainPage : PhoneApplicationPage
    {
        public static bool sReload = true;

        #region Task Lists Property

        public static readonly DependencyProperty TaskListsProperty =
               DependencyProperty.Register("TaskLists", typeof(ObservableCollection<TaskList>), typeof(MainPage), new PropertyMetadata(new ObservableCollection<TaskList>()));

        public ObservableCollection<TaskList> TaskLists
        {
            get { return (ObservableCollection<TaskList>)GetValue(TaskListsProperty); }
            set { SetValue(TaskListsProperty, value); }
        }

        #endregion

        #region Construction and Navigation

        ApplicationBarIconButton add;
        ApplicationBarIconButton sync;

        ApplicationBarMenuItem settings;
        ApplicationBarMenuItem about;
        ApplicationBarMenuItem feedback;
        ApplicationBarMenuItem donate;
        ApplicationBarMenuItem signOut;

        GeoCoordinateWatcher _watcher;

        public MainPage()
        {
            InitializeComponent();

            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);
            App.SyncingDisabled += new EventHandler<EventArgs>(App_SyncingDisabled);

            this.BuildApplicationBar();

            _watcher = new GeoCoordinateWatcher();
            _watcher.Start();
        }

        private void BuildApplicationBar()
        {
            add = new ApplicationBarIconButton();
            add.IconUri = new Uri("/Resources/add.png", UriKind.RelativeOrAbsolute);
            add.Text = Strings.AddMenuLower;
            add.Click += btnAdd_Click;

            sync = new ApplicationBarIconButton();
            sync.IconUri = new Uri("/Resources/retry.png", UriKind.RelativeOrAbsolute);
            sync.Text = Strings.SyncMenuLower;
            sync.Click += btnSync_Click;

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
            ApplicationBar.Buttons.Add(add);
            ApplicationBar.Buttons.Add(sync);

            ApplicationBar.MenuItems.Add(settings);
            ApplicationBar.MenuItems.Add(about);
            ApplicationBar.MenuItems.Add(feedback);
            ApplicationBar.MenuItems.Add(donate);
            ApplicationBar.MenuItems.Add(signOut);
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
                }
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            GlobalLoading.Instance.IsLoadingText(Strings.Loading);

            if (NavigationContext.QueryString.ContainsKey("IsFirstRun") == true)
            {
                NavigationService.RemoveBackEntry();

                Dispatcher.BeginInvoke(() =>
                {
                    SyncData();
                });
            }

            if (e.IsNavigationInitiator == false)
            {
                LittleWatson.CheckForPreviousException(true);

                Dispatcher.BeginInvoke(() =>
                {
                    SyncData();
                });
            }

            Dispatcher.BeginInvoke(() =>
            {
                LoadData();
            });

            base.OnNavigatedTo(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (this.dlgAddTask.IsOpen)
            {
                this.dlgAddTask.Close();
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

                if (App.RtmClient.TaskLists != null)
                {
                    var tempTaskLists = new SortableObservableCollection<TaskList>();

                    foreach (TaskList l in App.RtmClient.TaskLists)
                    {
                        if (l.Name.ToLower() == Strings.AllTasksLower)
                            tempTaskLists.Insert(0, l);
                        else
                            tempTaskLists.Add(l);
                    }

                    // insert the nearby list placeholder
                    TaskList nearby = new TaskList(Strings.Nearby);
                    tempTaskLists.Insert(1, nearby);

                    TaskLists = tempTaskLists;

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
                if (TaskLists.Count == 0)
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

        private void btnAdd_Click(object sender, EventArgs e)
        {
            this.dlgAddTask.Open();
        }

        private void dlgAddTask_Submit(object sender, SubmitEventArgs e)
        {
            AddTask(e.Text);
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            sReload = true;
            SyncData();
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            TaskList item = ((FrameworkElement)sender).DataContext as TaskList;

            if (item != null)
            {
                if (item.Name.ToLower() == Strings.AllTasksLower)
                    this.NavigationService.Navigate(new Uri("/TaskListByDatePage.xaml?id=" + item.Id, UriKind.Relative));
                else if (item.Name.ToLower() == Strings.NearbyLower)
                    this.NavigationService.Navigate(new Uri("/TaskListByLocationPage.xaml?id=" + item.Id, UriKind.Relative));
                else
                    this.NavigationService.Navigate(new Uri("/TaskListPage.xaml?id=" + item.Id, UriKind.Relative));
            }
        }

        private void TaskListCount_Loaded(object sender, EventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            TaskList taskList = (TaskList)target.DataContext;

            if (App.RtmClient.Locations == null) return;

            // set count
            if (taskList.Name.ToLower() == Strings.NearbyLower)
            {
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

                int count = App.RtmClient.GetNearbyTasks(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, radius).Count;

                if (count == 0)
                    target.Text = Strings.HubTileEmpty;
                else if (count == 1)
                    target.Text = 1 + " " + Strings.HubTileSingle;
                else
                    target.Text = count + " " + Strings.HubTilePlural;
            }
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

        private TaskList MostRecentTaskListClick
        {
            get;
            set;
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement)
            {
                FrameworkElement frameworkElement = (FrameworkElement)e.OriginalSource;
                if (frameworkElement.DataContext is TaskList)
                {
                    MostRecentTaskListClick = (TaskList)frameworkElement.DataContext;
                }
            }
            base.OnMouseLeftButtonDown(e);
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem target = (MenuItem)sender;
            ContextMenu parent = (ContextMenu)target.Parent;

            if (target.Header.ToString() == Strings.PinToStartLower)
            {
                ShellTile secondaryTile = ShellTile.ActiveTiles.FirstOrDefault(t => t.NavigationUri.ToString().Contains("id=" + MostRecentTaskListClick.Id));

                if (secondaryTile == null)
                {
                    StandardTileData data = new StandardTileData();

                    int tasksDueToday = 0;
                    int tasksOverdue = 0;
                    int tasksNearby = 0;

                    data.BackgroundImage = new Uri("BackgroundPinned.png", UriKind.Relative);
                    data.Title = "Milkman";
                    data.BackTitle = MostRecentTaskListClick.Name;

                    if (MostRecentTaskListClick.Name.ToLower() == Strings.NearbyLower)
                    {
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

                        tasksNearby = App.RtmClient.GetNearbyTasks(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, radius).Count;

                        if (tasksNearby == 0)
                            data.BackContent = Strings.LiveTileNearbyEmpty;
                        else if (tasksNearby == 1)
                            data.BackContent = tasksNearby + " " + Strings.LiveTileNearbySingle;
                        else
                            data.BackContent = tasksNearby + " " + Strings.LiveTileNearbyPlural;
                    }
                    else
                    {
                        if (MostRecentTaskListClick.Tasks != null)
                        {
                            tasksDueToday = MostRecentTaskListClick.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                                                     z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                            tasksOverdue = MostRecentTaskListClick.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                                                    z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                        }

                        if (tasksDueToday == 0)
                            data.BackContent = Strings.LiveTileEmpty;
                        else if (tasksDueToday == 1)
                            data.BackContent = tasksDueToday + " " + Strings.LiveTileSingle;
                        else
                            data.BackContent = tasksDueToday + " " + Strings.LiveTilePlural;

                        if (tasksOverdue > 0)
                            data.BackContent += ", " + tasksOverdue + " " + Strings.LiveTileOverdue;
                    }

                    if (MostRecentTaskListClick.Name.ToLower() == Strings.AllTasksLower)
                        ShellTile.Create(new Uri("/TaskListByDatePage.xaml?id=" + MostRecentTaskListClick.Id, UriKind.Relative), data);
                    else if (MostRecentTaskListClick.Name.ToLower() == Strings.NearbyLower)
                        ShellTile.Create(new Uri("/TaskListByLocationPage.xaml?id=" + MostRecentTaskListClick.Id, UriKind.Relative), data);
                    else
                        ShellTile.Create(new Uri("/TaskListPage.xaml?id=" + MostRecentTaskListClick.Id, UriKind.Relative), data);
                }
                else
                {
                    MessageBox.Show(Strings.PinToStartDialog, Strings.PinToStartDialogTitle, MessageBoxButton.OK);
                }
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

                NotificationsManager.SetupNotifications(_watcher.Position.Location);
            });
        }

        #endregion
    }
}

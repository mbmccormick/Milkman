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

        #region Dashboard Properties

        public static readonly DependencyProperty DashboardTasksProperty =
               DependencyProperty.Register("DashboardTasks", typeof(ObservableCollection<Task>), typeof(MainPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> DashboardTasks
        {
            get { return (ObservableCollection<Task>)GetValue(DashboardTasksProperty); }
            set { SetValue(DashboardTasksProperty, value); }
        }

        public static readonly DependencyProperty TaskListsProperty =
               DependencyProperty.Register("TaskLists", typeof(ObservableCollection<TaskList>), typeof(MainPage), new PropertyMetadata(new ObservableCollection<TaskList>()));

        public ObservableCollection<TaskList> TaskLists
        {
            get { return (ObservableCollection<TaskList>)GetValue(TaskListsProperty); }
            set { SetValue(TaskListsProperty, value); }
        }

        public static readonly DependencyProperty TaskTagsProperty =
               DependencyProperty.Register("TaskTags", typeof(ObservableCollection<TaskTag>), typeof(MainPage), new PropertyMetadata(new ObservableCollection<TaskTag>()));

        public ObservableCollection<TaskTag> TaskTags
        {
            get { return (ObservableCollection<TaskTag>)GetValue(TaskTagsProperty); }
            set { SetValue(TaskTagsProperty, value); }
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
                SyncData();
            }

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

                if (App.RtmClient.TaskLists != null)
                {
                    var tempDashboardTasks = new SortableObservableCollection<Task>();
                    var tempTaskLists = new SortableObservableCollection<TaskList>();
                    var tempTaskTags = new SortableObservableCollection<TaskTag>();

                    AppSettings settings = new AppSettings();

                    foreach (TaskList l in App.RtmClient.TaskLists)
                    {
                        if (l.Tasks != null &&
                            l.IsNormal == true)
                        {
                            foreach (Task t in l.Tasks)
                            {
                                if (t.IsCompleted == true ||
                                    t.IsDeleted == true) continue;

                                if (t.DueDateTime.HasValue &&
                                    t.DueDateTime.Value.Date <= DateTime.Now.AddDays(1).Date)
                                {
                                    tempDashboardTasks.Add(t);
                                }
                            }
                        }

                        if (l.Name.ToLower() == Strings.AllTasksLower)
                            tempTaskLists.Insert(0, l);
                        else
                            tempTaskLists.Add(l);
                    }

                    tempDashboardTasks.Sort();

                    // insert the nearby list placeholder
                    TaskList nearby = new TaskList(Strings.Nearby);
                    tempTaskLists.Insert(1, nearby);

                    foreach (var tag in App.RtmClient.GetTasksByTag().OrderBy(z => z.Key))
                    {
                        TaskTag data = new TaskTag();
                        data.Name = tag.Key;
                        data.Count = tag.Value.Count;

                        tempTaskTags.Add(data);
                    }

                    DashboardTasks = tempDashboardTasks;
                    TaskLists = tempTaskLists;
                    TaskTags = tempTaskTags;

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
                this.txtDashboardLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtListsLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtTagsLoading.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        private void ToggleEmptyText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if (DashboardTasks.Count == 0)
                    this.txtDashboardEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtDashboardEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if (TaskLists.Count == 0)
                    this.txtListsEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtListsEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if (TaskTags.Count == 0)
                    this.txtTagsEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtTagsEmpty.Visibility = System.Windows.Visibility.Collapsed;
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

        private void btnAdd_Click(object sender, EventArgs e)
        {
            AppSettings settings = new AppSettings();

            if (settings.AddTaskDialogEnabled == true)
            {
                AddTaskDialog dialog = new AddTaskDialog();

                dialog.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            

                            break;
                        default:
                            break;
                    }
                };

                dialog.Show();
            }
            else
            {
                NavigationService.Navigate(new Uri("/AddTaskPage.xaml", UriKind.Relative));
            }
        }

        private void dlgAddTask_Dismissed(object sender, EventArgs e)
        {
            // AddTask(e.Text);

            this.ApplicationBar.IsVisible = true;
        }

        private void dlgAddTask_Cancel(object sender, EventArgs e)
        {
            this.ApplicationBar.IsVisible = true;
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            sReload = true;
            SyncData();
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading) return;

            if (this.pivLayout.SelectedIndex == 0)
            {
                Task item = ((FrameworkElement)sender).DataContext as Task;

                if (item != null)
                {
                    NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative));
                }
            }
            else if (this.pivLayout.SelectedIndex == 1)
            {
                TaskList item = ((FrameworkElement)sender).DataContext as TaskList;

                if (item != null)
                {
                    if (item.Name.ToLower() == Strings.AllTasksLower)
                        NavigationService.Navigate(new Uri("/TaskListByDatePage.xaml?id=" + item.Id, UriKind.Relative));
                    else if (item.Name.ToLower() == Strings.NearbyLower)
                        NavigationService.Navigate(new Uri("/TaskListByLocationPage.xaml?id=" + item.Id, UriKind.Relative));
                    else
                        NavigationService.Navigate(new Uri("/TaskListPage.xaml?id=" + item.Id, UriKind.Relative));
                }
            }
            else if (this.pivLayout.SelectedIndex == 2)
            {
                TaskTag item = ((FrameworkElement)sender).DataContext as TaskTag;

                if (item != null)
                {
                    NavigationService.Navigate(new Uri("/TaskListByTagPage.xaml?id=" + item.Name, UriKind.Relative));
                }
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
            CustomMessageBox messageBox = new CustomMessageBox()
            {
                Caption = Strings.SignOutDialogTitle,
                Message = Strings.SignOutDialog,
                LeftButtonContent = Strings.YesLower,
                RightButtonContent = Strings.NoLower,
                IsFullScreen = false
            };

            messageBox.Dismissed += (s1, e1) =>
            {
                switch (e1.Result)
                {
                    case CustomMessageBoxResult.LeftButton:
                        App.DeleteData();
                        Login();

                        break;
                    default:
                        break;
                }
            };

            messageBox.Show();
        }

        private TaskList MostRecentTaskListClick
        {
            get;
            set;
        }

        private TaskTag MostRecentTaskTagClick
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
                else if (frameworkElement.DataContext is TaskTag)
                {
                    MostRecentTaskTagClick = (TaskTag)frameworkElement.DataContext;
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
                ShellTile secondaryTile = null;

                if (this.pivLayout.SelectedIndex == 1)
                    secondaryTile = ShellTile.ActiveTiles.FirstOrDefault(t => t.NavigationUri.ToString().Contains("id=" + MostRecentTaskListClick.Id));
                else if (this.pivLayout.SelectedIndex == 2)
                    secondaryTile = ShellTile.ActiveTiles.FirstOrDefault(t => t.NavigationUri.ToString().Contains("id=" + MostRecentTaskTagClick.Name));
                else
                    return;

                if (secondaryTile == null)
                {
                    StandardTileData data = new StandardTileData();

                    int tasksDueToday = 0;
                    int tasksOverdue = 0;
                    int tasksNearby = 0;

                    data.BackgroundImage = new Uri("BackgroundPinned.png", UriKind.Relative);
                    data.Title = "Milkman";

                    if (this.pivLayout.SelectedIndex == 1)
                        data.BackTitle = MostRecentTaskListClick.Name;
                    else if (this.pivLayout.SelectedIndex == 2)
                        data.BackTitle = MostRecentTaskTagClick.Name;
                    else
                        return;

                    if (this.pivLayout.SelectedIndex == 1 &&
                        MostRecentTaskListClick.Name.ToLower() == Strings.NearbyLower)
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
                        if (this.pivLayout.SelectedIndex == 1)
                        {
                            if (MostRecentTaskListClick.Tasks != null)
                            {
                                tasksDueToday = MostRecentTaskListClick.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                                                         z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                                tasksOverdue = MostRecentTaskListClick.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                                                        z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                            }
                        }
                        else if (this.pivLayout.SelectedIndex == 2)
                        {
                            var tasks = App.RtmClient.GetTasksByTag()[MostRecentTaskTagClick.Name];

                            tasksDueToday = tasks.Where(z => z.DueDateTime.HasValue &&
                                                             z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                            tasksOverdue = tasks.Where(z => z.DueDateTime.HasValue &&
                                                            z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                        }
                        else
                        {
                            return;
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

                    if (this.pivLayout.SelectedIndex == 1)
                    {
                        if (MostRecentTaskListClick.Name.ToLower() == Strings.AllTasksLower)
                            ShellTile.Create(new Uri("/TaskListByDatePage.xaml?id=" + MostRecentTaskListClick.Id, UriKind.Relative), data);
                        else if (MostRecentTaskListClick.Name.ToLower() == Strings.NearbyLower)
                            ShellTile.Create(new Uri("/TaskListByLocationPage.xaml?id=" + MostRecentTaskListClick.Id, UriKind.Relative), data);
                        else
                            ShellTile.Create(new Uri("/TaskListPage.xaml?id=" + MostRecentTaskListClick.Id, UriKind.Relative), data);
                    }
                    else if (this.pivLayout.SelectedIndex == 2)
                    {
                        ShellTile.Create(new Uri("/TaskListByTagPage.xaml?id=" + MostRecentTaskTagClick.Name, UriKind.Relative), data);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    MessageBox.Show(Strings.PinToStartDialog, Strings.PinToStartDialogTitle, MessageBoxButton.OK);
                }
            }
        }

        private void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            if (e.IsMenuVisible == true)
                ApplicationBar.Opacity = 1.0;
            else
                ApplicationBar.Opacity = 0.7;
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

    public class TaskTag
    {
        private string _name;
        private int _count;

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public string CountString
        {
            get
            {
                if (Count == 0)
                    return Strings.NoTasks;
                else if (Count == 1)
                    return Count + " " + Strings.TaskSingle;
                else
                    return Count + " " + Strings.TaskPlural;
            }
        }
    }
}

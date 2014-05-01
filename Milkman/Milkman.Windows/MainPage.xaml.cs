using System;
using System.Collections.ObjectModel;
using System.Linq;
using IronCow;
using Milkman.Common;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Milkman
{
    public partial class MainPage : Page
    {
        public static bool sReload = true;
        public static bool sFirstLaunch = false;

        #region Dashboard Properties

        public static readonly DependencyProperty AllTasksProperty =
               DependencyProperty.Register("AllTasks", typeof(ObservableCollection<Task>), typeof(MainPage), new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> AllTasks
        {
            get { return (ObservableCollection<Task>)GetValue(AllTasksProperty); }
            set { SetValue(AllTasksProperty, value); }
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

        //AddTaskDialog dlgAddTask;

        //GeoCoordinateWatcher _watcher;

        public MainPage()
        {
            this.InitializeComponent();

            //App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            //_watcher = new GeoCoordinateWatcher();
            //_watcher.Start();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //GlobalLoading.Instance.IsLoadingText(Strings.Loading);

            if (Frame.CanGoBack == true)
                Frame.BackStack.RemoveAt(0);

            //if (e.Parameter.ToString().Contains("IsFirstRun") == true)
            //{
            SyncData();
            //}

            if (e.NavigationMode != NavigationMode.New)
            {
                //LittleWatson.CheckForPreviousException(true);

                //App.PromptForMarketplaceReview();

                sFirstLaunch = true;
            }

            LoadData();
        }

        //private void App_UnhandledExceptionHandled(object sender, ApplicationUnhandledExceptionEventArgs e)
        //{
        //    Dispatcher.BeginInvoke(() =>
        //    {
        //        GlobalLoading.Instance.IsLoading = false;
        //    });
        //}

        #endregion

        #region Loading Data

        private void SyncData()
        {
            if (sReload)
            {
                if (!string.IsNullOrEmpty(App.RtmClient.AuthToken))
                {
                    SmartDispatcher.BeginInvoke(() =>
                    {
                        //GlobalLoading.Instance.IsLoadingText(Strings.SyncingTasks);
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
                    //GlobalLoading.Instance.IsLoading = false;
                });
            }

            sReload = false;
        }

        private void LoadData()
        {
            LoadDataInBackground();

            //NotificationsManager.SetupNotifications(_watcher.Position.Location);
        }

        private void LoadDataInBackground()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                //GlobalLoading.Instance.IsLoadingText(Strings.SyncingTasks);

                if (App.RtmClient.TaskLists != null)
                {
                    var tempAllTasks = new SortableObservableCollection<Task>();
                    var tempTaskLists = new SortableObservableCollection<TaskList>();
                    var tempTaskTags = new SortableObservableCollection<TaskTag>();

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
                                    tempAllTasks.Add(t);
                                }
                            }
                        }

                        if (l.Name.ToLower() == "all tasks")
                            tempTaskLists.Insert(0, l);
                        else
                            tempTaskLists.Add(l);
                    }

                    //tempAllTasks.Sort(Task.CompareByDate);

                    // insert the nearby list placeholder
                    TaskList nearby = new TaskList("Nearby");
                    tempTaskLists.Insert(1, nearby);

                    foreach (var tag in App.RtmClient.GetTasksByTag().OrderBy(z => z.Key))
                    {
                        TaskTag data = new TaskTag();
                        data.Name = tag.Key;
                        data.Count = tag.Value.Count;

                        tempTaskTags.Add(data);
                    }

                    AllTasks = tempAllTasks;
                    TaskLists = tempTaskLists;
                    TaskTags = tempTaskTags;

                    ToggleLoadingText();
                    ToggleEmptyText();

                    //GlobalLoading.Instance.IsLoading = false;

                    ShowLastUpdatedStatus();
                }
            });
        }

        private void ToggleLoadingText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.txtDashboardLoading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            });
        }

        private void ToggleEmptyText()
        {
            //SmartDispatcher.BeginInvoke(() =>
            //{
            //    if (DashboardTasks.Count == 0)
            //        this.txtDashboardEmpty.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //    else
            //        this.txtDashboardEmpty.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //    if (TaskLists.Count == 0)
            //        this.txtListsEmpty.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //    else
            //        this.txtListsEmpty.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            //    if (TaskTags.Count == 0)
            //        this.txtTagsEmpty.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //    else
            //        this.txtTagsEmpty.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //});
        }

        private void ShowLastUpdatedStatus()
        {
            //if (sFirstLaunch == true)
            //{
            //    int minutes = Convert.ToInt32((DateTime.Now - App.LastUpdated).TotalMinutes);

            //    if (minutes < 2)
            //        GlobalLoading.Instance.StatusText(Strings.UpToDate);
            //    else if (minutes > 60)
            //        GlobalLoading.Instance.StatusText(Strings.LastUpdated + " " + Strings.OverAnHourAgo);
            //    else
            //        GlobalLoading.Instance.StatusText(Strings.LastUpdated + " " + minutes + " " + Strings.MinutesAgo);

            //    System.ComponentModel.BackgroundWorker b = new System.ComponentModel.BackgroundWorker();
            //    b.DoWork += (s, e) =>
            //    {
            //        System.Threading.Thread.Sleep(4000);

            //        SmartDispatcher.BeginInvoke(() =>
            //        {
            //            GlobalLoading.Instance.ClearStatusText();
            //        });
            //    };

            //    sFirstLaunch = false;

            //    b.RunWorkerAsync();
            //}
        }

        public void Login()
        {
            Frame.Navigate(typeof(WelcomePage));
        }

        #endregion

        #region Event Handlers

        //private void btnAdd_Click(object sender, EventArgs e)
        //{
        //    AppSettings settings = new AppSettings();

        //    if (settings.AddTaskDialogEnabled == true)
        //    {
        //        this.dlgAddTask = new AddTaskDialog();

        //        CustomMessageBox messageBox = this.dlgAddTask.CreateDialog("");

        //        messageBox.Dismissed += (s1, e1) =>
        //        {
        //            switch (e1.Result)
        //            {
        //                case CustomMessageBoxResult.LeftButton:
        //                    AddTask(this.dlgAddTask.txtDetails.Text);

        //                    break;
        //                default:
        //                    break;
        //            }
        //        };

        //        messageBox.Show();
        //    }
        //    else
        //    {
        //        NavigationService.Navigate(new Uri("/AddTaskPage.xaml", UriKind.Relative));
        //    }
        //}

        //private void btnSelect_Click(object sender, EventArgs e)
        //{
        //    LongListMultiSelector target = this.lstTasks;

        //    target.IsSelectionEnabled = true;
        //}

        //private void btnComplete_Click(object sender, EventArgs e)
        //{
        //    if (GlobalLoading.Instance.IsLoading) return;

        //    LongListMultiSelector target = this.lstTasks;

        //    string messageBoxText;
        //    if (target.SelectedItems.Count == 1)
        //        messageBoxText = Strings.CompleteTaskSingleDialog;
        //    else
        //        messageBoxText = Strings.CompleteTaskPluralDialog;

        //    CustomMessageBox messageBox = new CustomMessageBox()
        //    {
        //        Caption = Strings.CompleteDialogTitle,
        //        Message = messageBoxText,
        //        LeftButtonContent = Strings.YesLower,
        //        RightButtonContent = Strings.NoLower,
        //        IsFullScreen = false
        //    };

        //    messageBox.Dismissed += (s1, e1) =>
        //    {
        //        switch (e1.Result)
        //        {
        //            case CustomMessageBoxResult.LeftButton:
        //                bool isMultiple = target.SelectedItems.Count > 1;

        //                while (target.SelectedItems.Count > 0)
        //                {
        //                    CompleteTask((Task)target.SelectedItems[0], isMultiple);
        //                    target.SelectedItems.RemoveAt(0);
        //                }

        //                target.IsSelectionEnabled = false;

        //                break;
        //            default:
        //                break;
        //        }
        //    };

        //    messageBox.Show();
        //}

        //private void btnPostpone_Click(object sender, EventArgs e)
        //{
        //    if (GlobalLoading.Instance.IsLoading) return;

        //    LongListMultiSelector target = this.lstTasks;

        //    string messageBoxText;
        //    if (target.SelectedItems.Count == 1)
        //        messageBoxText = Strings.PostponeTaskSingleDialog;
        //    else
        //        messageBoxText = Strings.PostponeTaskPluralDialog;

        //    CustomMessageBox messageBox = new CustomMessageBox()
        //    {
        //        Caption = Strings.PostponeDialogTitle,
        //        Message = messageBoxText,
        //        LeftButtonContent = Strings.YesLower,
        //        RightButtonContent = Strings.NoLower,
        //        IsFullScreen = false
        //    };

        //    messageBox.Dismissed += (s1, e1) =>
        //    {
        //        switch (e1.Result)
        //        {
        //            case CustomMessageBoxResult.LeftButton:
        //                bool isMultiple = target.SelectedItems.Count > 1;

        //                while (target.SelectedItems.Count > 0)
        //                {
        //                    PostponeTask((Task)target.SelectedItems[0], isMultiple);
        //                    target.SelectedItems.RemoveAt(0);
        //                }

        //                target.IsSelectionEnabled = false;

        //                break;
        //            default:
        //                break;
        //        }
        //    };

        //    messageBox.Show();
        //}

        //private void btnDelete_Click(object sender, EventArgs e)
        //{
        //    if (GlobalLoading.Instance.IsLoading) return;

        //    LongListMultiSelector target = this.lstTasks;

        //    string messageBoxText;
        //    if (target.SelectedItems.Count == 1)
        //        messageBoxText = Strings.DeleteTaskSingleDialog;
        //    else
        //        messageBoxText = Strings.DeleteTaskPluralDialog;

        //    CustomMessageBox messageBox = new CustomMessageBox()
        //    {
        //        Caption = Strings.DeleteTaskDialogTitle,
        //        Message = messageBoxText,
        //        LeftButtonContent = Strings.YesLower,
        //        RightButtonContent = Strings.NoLower,
        //        IsFullScreen = false
        //    };

        //    messageBox.Dismissed += (s1, e1) =>
        //    {
        //        switch (e1.Result)
        //        {
        //            case CustomMessageBoxResult.LeftButton:
        //                bool isMultiple = target.SelectedItems.Count > 1;

        //                while (target.SelectedItems.Count > 0)
        //                {
        //                    DeleteTask((Task)target.SelectedItems[0], isMultiple);
        //                    target.SelectedItems.RemoveAt(0);
        //                }

        //                target.IsSelectionEnabled = false;

        //                break;
        //            default:
        //                break;
        //        }
        //    };

        //    messageBox.Show();
        //}

        private void btnSync_Click(object sender, EventArgs e)
        {
            sReload = true;
            sFirstLaunch = true;

            SyncData();
        }

        private void ItemContent_Tapped(object sender, RoutedEventArgs e)
        {
            //if (GlobalLoading.Instance.IsLoading) return;

            //if (this.pivLayout.SelectedIndex == 0)
            //{
            //    Task item = ((FrameworkElement)sender).DataContext as Task;

            //    if (item != null)
            //    {
            //        NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative));
            //    }
            //}
            //else if (this.pivLayout.SelectedIndex == 1)
            //{
            //    TaskList item = ((FrameworkElement)sender).DataContext as TaskList;

            //    if (item != null)
            //    {
            //        if (item.Name.ToLower() == Strings.NearbyLower)
            //            NavigationService.Navigate(new Uri("/TaskListByLocationPage.xaml?id=" + item.Id, UriKind.Relative));
            //        else
            //            NavigationService.Navigate(new Uri("/TaskListPage.xaml?id=" + item.Id, UriKind.Relative));
            //    }
            //}
            //else if (this.pivLayout.SelectedIndex == 2)
            //{
            //    TaskTag item = ((FrameworkElement)sender).DataContext as TaskTag;

            //    if (item != null)
            //    {
            //        NavigationService.Navigate(new Uri("/TaskListByTagPage.xaml?id=" + item.Name, UriKind.Relative));
            //    }
            //}
        }

        private void TaskListCount_Loaded(object sender, EventArgs e)
        {
            //TextBlock target = (TextBlock)sender;

            //TaskList taskList = (TaskList)target.DataContext;

            //if (App.RtmClient.Locations == null) return;

            //// set count
            //if (taskList.Name.ToLower() == Strings.NearbyLower)
            //{
            //    AppSettings settings = new AppSettings();

            //    double radius;
            //    if (settings.NearbyRadius == 0)
            //        radius = 1.0;
            //    else if (settings.NearbyRadius == 1)
            //        radius = 2.0;
            //    else if (settings.NearbyRadius == 2)
            //        radius = 5.0;
            //    else if (settings.NearbyRadius == 3)
            //        radius = 10.0;
            //    else if (settings.NearbyRadius == 3)
            //        radius = 20.0;
            //    else
            //        radius = 0.0;

            //    int count = App.RtmClient.GetNearbyTasks(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, radius).Count;

            //    if (count == 0)
            //        target.Text = Strings.HubTileEmpty;
            //    else if (count == 1)
            //        target.Text = 1 + " " + Strings.HubTileSingle;
            //    else
            //        target.Text = count + " " + Strings.HubTilePlural;
            //}
        }

        //private void mnuSettings_Click(object sender, EventArgs e)
        //{
        //    SmartDispatcher.BeginInvoke(() =>
        //    {
        //        NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        //    });
        //}

        //private void mnuAbout_Click(object sender, EventArgs e)
        //{
        //    SmartDispatcher.BeginInvoke(() =>
        //    {
        //        NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        //    });
        //}

        //private void mnuFeedback_Click(object sender, EventArgs e)
        //{
        //    EmailComposeTask emailComposeTask = new EmailComposeTask();

        //    emailComposeTask.To = "feedback@mbmccormick.com";
        //    emailComposeTask.Subject = "Milkman Feedback";
        //    emailComposeTask.Body = "Version " + App.ExtendedVersionNumber + " (" + App.PlatformVersionNumber + ")\n\n";
        //    emailComposeTask.Show();
        //}

        //private async void mnuDonate_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        var productList = await CurrentApp.LoadListingInformationAsync();
        //        var product = productList.ProductListings.FirstOrDefault(p => p.Value.ProductType == ProductType.Consumable);
        //        var receipt = await CurrentApp.RequestProductPurchaseAsync(product.Value.ProductId, true);

        //        if (CurrentApp.LicenseInformation.ProductLicenses[product.Value.ProductId].IsActive)
        //        {
        //            CurrentApp.ReportProductFulfillment(product.Value.ProductId);

        //            MessageBox.Show(Strings.DonateDialog, Strings.DonateDialogTitle, MessageBoxButton.OK);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // do nothing
        //    }
        //}

        //private void mnuSignOut_Click(object sender, EventArgs e)
        //{
        //    CustomMessageBox messageBox = new CustomMessageBox()
        //    {
        //        Caption = Strings.SignOutDialogTitle,
        //        Message = Strings.SignOutDialog,
        //        LeftButtonContent = Strings.YesLower,
        //        RightButtonContent = Strings.NoLower,
        //        IsFullScreen = false
        //    };

        //    messageBox.Dismissed += (s1, e1) =>
        //    {
        //        switch (e1.Result)
        //        {
        //            case CustomMessageBoxResult.LeftButton:
        //                App.DeleteData();
        //                Login();

        //                break;
        //            default:
        //                break;
        //        }
        //    };

        //    messageBox.Show();
        //}

        private Task MostRecentTaskClick
        {
            get;
            set;
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

        //protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    if (e.OriginalSource is FrameworkElement)
        //    {
        //        FrameworkElement frameworkElement = (FrameworkElement)e.OriginalSource;
        //        if (frameworkElement.DataContext is Task)
        //        {
        //            MostRecentTaskClick = (Task)frameworkElement.DataContext;
        //        }
        //        else if (frameworkElement.DataContext is TaskList)
        //        {
        //            MostRecentTaskListClick = (TaskList)frameworkElement.DataContext;
        //        }
        //        else if (frameworkElement.DataContext is TaskTag)
        //        {
        //            MostRecentTaskTagClick = (TaskTag)frameworkElement.DataContext;
        //        }
        //    }

        //    base.OnMouseLeftButtonDown(e);
        //}

        #endregion

        #region Task Methods

        private void AddTask(string smartAddText)
        {
            //GlobalLoading.Instance.IsLoadingText(Strings.AddingTask);

            //string input = smartAddText;
            //if (input.Contains('#') == false)
            //{
            //    TaskList defaultList = App.RtmClient.GetDefaultTaskList();
            //    if (defaultList.IsSmart == false)
            //        input = input + " #" + defaultList.Name;
            //}

            //App.RtmClient.AddTask(input, true, null, () =>
            //{
            //    Dispatcher.BeginInvoke(() =>
            //    {
            //        GlobalLoading.Instance.IsLoading = false;
            //    });

            //    sReload = true;
            //    LoadData();

            //    NotificationsManager.SetupNotifications(_watcher.Position.Location);
            //});
        }

        private void CompleteTask(Task data, bool isMultiple)
        {
            //if (isMultiple == true)
            //    GlobalLoading.Instance.IsLoadingText(Strings.CompletingTasks);
            //else
            //    GlobalLoading.Instance.IsLoadingText(Strings.CompletingTask);

            //data.Complete(() =>
            //{
            //    App.RtmClient.CacheTasks(() =>
            //    {
            //        Dispatcher.BeginInvoke(() =>
            //        {
            //            GlobalLoading.Instance.IsLoading = false;
            //        });

            //        sReload = true;
            //        LoadData();
            //    });
            //});
        }

        private void PostponeTask(Task data, bool isMultiple)
        {
            //if (isMultiple == true)
            //    GlobalLoading.Instance.IsLoadingText(Strings.PostponingTasks);
            //else
            //    GlobalLoading.Instance.IsLoadingText(Strings.PostponingTask);

            //data.Postpone(() =>
            //{
            //    App.RtmClient.CacheTasks(() =>
            //    {
            //        Dispatcher.BeginInvoke(() =>
            //        {
            //            GlobalLoading.Instance.IsLoading = false;
            //        });

            //        sReload = true;
            //        LoadData();
            //    });
            //});
        }

        private void DeleteTask(Task data, bool isMultiple)
        {
            //if (isMultiple == true)
            //    GlobalLoading.Instance.IsLoadingText(Strings.DeletingTasks);
            //else
            //    GlobalLoading.Instance.IsLoadingText(Strings.DeletingTask);

            //data.Delete(() =>
            //{
            //    App.RtmClient.CacheTasks(() =>
            //    {
            //        Dispatcher.BeginInvoke(() =>
            //        {
            //            GlobalLoading.Instance.IsLoading = false;
            //        });

            //        sReload = true;
            //        LoadData();
            //    });
            //});
        }

        #endregion
    }

    #region TaskTag Class Declaration

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
                    return "No tasks";
                else if (Count == 1)
                    return Count + " " + "task";
                else
                    return Count + " " + "tasks";
            }
        }
    }

    #endregion
}
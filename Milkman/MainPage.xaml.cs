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
using Microsoft.Phone.Scheduler;

namespace Milkman
{
    public partial class MainPage : PhoneApplicationPage
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

        #region Task Lists Properties

        public ObservableCollection<Task> TodayTasks
        {
            get { return (ObservableCollection<Task>)GetValue(TodayTasksProperty); }
            set { SetValue(TodayTasksProperty, value); }
        }

        public static readonly DependencyProperty TodayTasksProperty =
               DependencyProperty.Register("TodayTasks", typeof(ObservableCollection<Task>), typeof(MainPage),
                   new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> TomorrowTasks
        {
            get { return (ObservableCollection<Task>)GetValue(TomorrowTasksProperty); }
            set { SetValue(TomorrowTasksProperty, value); }
        }

        public static readonly DependencyProperty TomorrowTasksProperty =
               DependencyProperty.Register("TomorrowTasks", typeof(ObservableCollection<Task>), typeof(MainPage),
                   new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> OverdueTasks
        {
            get { return (ObservableCollection<Task>)GetValue(OverdueTasksProperty); }
            set { SetValue(OverdueTasksProperty, value); }
        }

        public static readonly DependencyProperty OverdueTasksProperty =
               DependencyProperty.Register("OverdueTasks", typeof(ObservableCollection<Task>), typeof(MainPage),
                   new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> NoDueTasks
        {
            get { return (ObservableCollection<Task>)GetValue(NoDueTasksProperty); }
            set { SetValue(NoDueTasksProperty, value); }
        }

        public static readonly DependencyProperty NoDueTasksProperty =
               DependencyProperty.Register("NoDueTasks", typeof(ObservableCollection<Task>), typeof(MainPage),
                   new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<Task> WeekTasks
        {
            get { return (ObservableCollection<Task>)GetValue(WeekTasksProperty); }
            set { SetValue(WeekTasksProperty, value); }
        }

        public static readonly DependencyProperty WeekTasksProperty =
               DependencyProperty.Register("WeekTasks", typeof(ObservableCollection<Task>), typeof(MainPage),
                   new PropertyMetadata(new ObservableCollection<Task>()));

        public ObservableCollection<TaskList> TaskLists
        {
            get { return (ObservableCollection<TaskList>)GetValue(TaskListsProperty); }
            set { SetValue(TaskListsProperty, value); }
        }

        public static readonly DependencyProperty TaskListsProperty =
               DependencyProperty.Register("TaskLists", typeof(ObservableCollection<TaskList>), typeof(MainPage),
                   new PropertyMetadata(new ObservableCollection<TaskList>()));

        public readonly static DependencyProperty TagsProperty =
            DependencyProperty.Register("Tags", typeof(SortableObservableCollection<string>), typeof(MainPage),
                new PropertyMetadata((SortableObservableCollection<string>)null));

        public SortableObservableCollection<string> Tags
        {
            get { return (SortableObservableCollection<string>)GetValue(TagsProperty); }
            set { SetValue(TagsProperty, value); }
        }

        #endregion

        #region Construction and Navigation

        ProgressIndicator progressIndicator;

        ApplicationBarIconButton add;
        ApplicationBarIconButton select;
        ApplicationBarIconButton sync;
        ApplicationBarIconButton complete;
        ApplicationBarIconButton postpone;
        ApplicationBarIconButton delete;

        ApplicationBarMenuItem about;
        ApplicationBarMenuItem help;
        ApplicationBarMenuItem logout;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

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

            about = new ApplicationBarMenuItem();
            about.Text = "about milkman";
            about.Click += mnuAbout_Click;

            help = new ApplicationBarMenuItem();
            help.Text = "shortcuts help";
            help.Click += mnuHelp_Click;

            logout = new ApplicationBarMenuItem();
            logout.Text = "logout";
            logout.Click += mnuLogout_Click;

            IsLoading = false;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            progressIndicator = new ProgressIndicator();
            progressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator = progressIndicator;

            LoadData();
            SyncData();

            // stop and restart background worker
            if (ScheduledActionService.Find("BackgroundWorker") != null)
                ScheduledActionService.Remove("BackgroundWorker");

            PeriodicTask task = new PeriodicTask("BackgroundWorker");
            task.Description = "Manages background syncing, task reminders, and live tile updates.";

            ScheduledActionService.Add(task);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstWeek;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstNoDue;
            
            if (this.dlgAddTask.IsOpen)
            {
                this.dlgAddTask.Close();
                e.Cancel = true;
            }

            if (target.IsSelectionEnabled)
            {
                target.IsSelectionEnabled = false;
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
                            this.IsLoading = true;
                        });

                        App.RtmClient.SyncEverything(() =>
                        {
                            LoadData();

                            SmartDispatcher.BeginInvoke(() =>
                            {
                                IsLoading = false;
                            });
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
            };
            b.RunWorkerAsync();
        }

        private void LoadDataInBackground()
        {
            if (App.RtmClient.TaskLists != null)
            {
                var tempTaskLists = new SortableObservableCollection<TaskList>();

                var tempOverdueTasks = new SortableObservableCollection<Task>();
                var tempTodayTasks = new SortableObservableCollection<Task>();
                var tempTomorrowTasks = new SortableObservableCollection<Task>();
                var tempWeekTasks = new SortableObservableCollection<Task>();
                var tempNoDueTasks = new SortableObservableCollection<Task>();

                var tempTags = new SortableObservableCollection<string>();

                foreach (TaskList l in App.RtmClient.TaskLists)
                {
                    tempTaskLists.Add(l);

                    if (l.IsNormal && l.Tasks != null)
                    {
                        foreach (Task task in l.Tasks)
                        {
                            // add tags
                            foreach (string tag in task.Tags)
                            {
                                if (!tempTags.Contains(tag)) tempTags.Add(tag);
                            }

                            if (task.IsIncomplete)
                            {
                                if (task.DueDateTime.HasValue)
                                {
                                    // overdue
                                    if (task.DueDateTime.Value < DateTime.Today || (task.HasDueTime && task.DueDateTime.Value < DateTime.Now))
                                    {
                                        tempOverdueTasks.Add(task);
                                    }
                                    // today
                                    else if (task.DueDateTime.Value.Date == DateTime.Today)
                                    {
                                        tempTodayTasks.Add(task);
                                    }
                                    // tomorrow
                                    else if (task.DueDateTime.Value.Date == DateTime.Today.AddDays(1))
                                    {
                                        tempTomorrowTasks.Add(task);
                                    }
                                    // this week
                                    else if (task.DueDateTime.Value.Date > DateTime.Today.AddDays(1) && task.DueDateTime.Value.Date <= DateTime.Today.AddDays(6))
                                    {
                                        tempWeekTasks.Add(task);
                                    }
                                }
                                else
                                {
                                    // no due
                                    tempNoDueTasks.Add(task);
                                }
                            }
                        }
                    }
                }

                tempOverdueTasks.Sort();
                tempTodayTasks.Sort();
                tempTomorrowTasks.Sort();
                tempWeekTasks.Sort();
                tempNoDueTasks.Sort();

                tempTags.Sort();

                SmartDispatcher.BeginInvoke(() =>
                {
                    TaskLists = tempTaskLists;

                    TodayTasks = tempTodayTasks;
                    TomorrowTasks = tempTomorrowTasks;
                    OverdueTasks = tempOverdueTasks;
                    WeekTasks = tempWeekTasks;
                    NoDueTasks = tempNoDueTasks;

                    Tags = tempTags;
                });

                // delete all existing reminders
                foreach (var item in ScheduledActionService.GetActions<Reminder>())
                {
                    ScheduledActionService.Remove(item.Name);
                }

                // add new reminders
                foreach (var item in tempTodayTasks.Concat(tempTomorrowTasks).Concat(tempWeekTasks))
                {
                    if (item.HasDueTime)
                    {
                        Reminder r = new Reminder(item.Id);
                        r.Title = item.Name;
                        r.Content = "This task is due " + item.FriendlyDueDate.Replace("Due ", "") + ".";
                        r.NavigationUri = new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative);
                        r.BeginTime = item.DueDateTime.Value.AddHours(-1);
                        r.ExpirationTime = item.DueDateTime.Value;

                        ScheduledActionService.Add(r);
                    }
                }

                // update live tile data
                ShellTile primaryTile = ShellTile.ActiveTiles.First();
                if (primaryTile != null)
                {
                    StandardTileData data = new StandardTileData();

                    data.BackTitle = "Milkman";
                    if (tempTodayTasks.Count == 0)
                        data.BackContent = "No tasks due today";
                    else if (tempTodayTasks.Count == 1)
                        data.BackContent = tempTodayTasks.Count + " task due today";
                    else
                        data.BackContent = tempTodayTasks.Count + " tasks due today";

                    primaryTile.Update(data);
                }
            }
        }

        public void Login()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.NavigationService.Navigate(new Uri("/AuthorizationPage.xaml", UriKind.Relative));
            });
        }

        #endregion

        private void btnAdd_Click(object sender, EventArgs e)
        {
            this.dlgAddTask.Open();
        }

        private void dlgAddTask_Submit(object sender, SubmitEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;
            });

            App.RtmClient.AddTask(e.Text, true, null, () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    IsLoading = false;
                });

                sReload = true;
                LoadData();
            });
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstWeek;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstNoDue;

            target.IsSelectionEnabled = true;
        }

        private void btnComplete_Click(object sender, EventArgs e)
        {
            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstWeek;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstNoDue;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to mark the selected task as complete?";
            else
                messageBoxText = "Are you sure you want to mark the selected tasks as complete?";

            if (MessageBox.Show(messageBoxText, "Complete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                List<Task> toComplete = new List<Task>();
                while (target.SelectedItems.Count > 0)
                {
                    toComplete.Add((Task)target.SelectedItems[0]);
                    target.SelectedItems.RemoveAt(0);
                }

                foreach (var item in toComplete)
                {
                    IsLoading = true;
                    ((Task)item).Complete(() =>
                    {
                        App.RtmClient.CacheTasks(() =>
                        {
                            IsLoading = false;
                        });
                    });
                }

                sReload = true;
                SyncData();

                target.IsSelectionEnabled = false;
            }
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstWeek;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstNoDue;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to postpone the selected task?";
            else
                messageBoxText = "Are you sure you want to postpone the selected tasks?";

            if (MessageBox.Show(messageBoxText, "Postpone", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                List<Task> toPostpone = new List<Task>();
                while (target.SelectedItems.Count > 0)
                {
                    toPostpone.Add((Task)target.SelectedItems[0]);
                    target.SelectedItems.RemoveAt(0);
                }

                foreach (var item in toPostpone)
                {
                    IsLoading = true;
                    ((Task)item).Postpone(() =>
                    {
                        App.RtmClient.CacheTasks(() =>
                        {
                            IsLoading = false;
                        });
                    });
                }

                sReload = true;
                SyncData();

                target.IsSelectionEnabled = false;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstWeek;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstNoDue;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to delete the selected task?";
            else
                messageBoxText = "Are you sure you want to delete the selected tasks?";

            if (MessageBox.Show(messageBoxText, "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                List<Task> toDelete = new List<Task>();
                while (target.SelectedItems.Count > 0)
                {
                    toDelete.Add((Task)target.SelectedItems[0]);
                    target.SelectedItems.RemoveAt(0);
                }

                foreach (var item in toDelete)
                {
                    IsLoading = true;
                    ((Task)item).Delete(() =>
                    {
                        App.RtmClient.CacheTasks(() =>
                        {
                            IsLoading = false;
                        });
                    });
                }

                sReload = true;
                SyncData();

                target.IsSelectionEnabled = false;
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
                i.IsEnabled = false;

                ApplicationBar.Buttons.Add(delete);
                ApplicationBarIconButton k = (ApplicationBarIconButton)ApplicationBar.Buttons[2];
                j.IsEnabled = false;
            }
            else
            {
                ApplicationBar.Buttons.Add(add);
                ApplicationBar.Buttons.Add(select);
                ApplicationBar.Buttons.Add(sync);

                ApplicationBar.MenuItems.Add(about);
                ApplicationBar.MenuItems.Add(help);
                ApplicationBar.MenuItems.Add(logout);
            }
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Task item = ((FrameworkElement)sender).DataContext as Task;

            if (item != null)
                this.NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative));
        }

        private void ItemContent_Loaded(object sender, EventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            Task task = (Task)target.DataContext;
            if (task.Priority == TaskPriority.One)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 234, 82, 0));
            else if (task.Priority == TaskPriority.Two)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 96, 191));
            else if (task.Priority == TaskPriority.Three)
                target.Foreground = new SolidColorBrush(Color.FromArgb(255, 53, 154, 255));
        }

        private void mnuAbout_Click(object sender, EventArgs e)
        {
            this.NavigationService.Navigate(new Uri("/YourLastAboutDialog;component/AboutPage.xaml", UriKind.Relative));
        }

        private void mnuHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Milkman uses the Smart Add shortcuts for creating tasks: ^ for due date, ! for priority, # for lists and tags, @ for location, * for repeat, and = for time estimate.\n\nFor example, \"Pick up milk ^today at 2pm !1 #Personal @Grocery Store *weekly =15 minutes\" would create a task to pick up the milk that is due today at 2:00 PM with high priority on the Personal list at the Grocery Store that occurs every week for 15 minutes.", "Smart Add Shortcuts", MessageBoxButton.OK);
        }

        private void mnuLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout of Milkman and remove all of your data?", "Logout", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                App.DeleteData();
                Login();
            }
        }
    }
}
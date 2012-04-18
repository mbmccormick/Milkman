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
using Microsoft.Phone.Tasks;

namespace Milkman
{
    public partial class TaskListByDatePage : PhoneApplicationPage
    {
        #region IsLoading Property

        public static bool sReload = false;

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(TaskListByDatePage),
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

        #region Task List Property

        public static readonly DependencyProperty TaskListProperty =
            DependencyProperty.Register("CurrentList", typeof(TaskList), typeof(TaskListByDatePage), new PropertyMetadata(new TaskList()));

        private TaskList CurrentList
        {
            get { return (TaskList)GetValue(TaskListProperty); }
            set { SetValue(TaskListProperty, value); }
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

        ApplicationBarMenuItem settings;
        ApplicationBarMenuItem about;
        ApplicationBarMenuItem feedback;
        ApplicationBarMenuItem signOut;

        // Constructor
        public TaskListByDatePage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(TaskListByDatePage_Loaded);
            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            TiltEffect.TiltableItems.Add(typeof(MultiselectItem));

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

            signOut = new ApplicationBarMenuItem();
            signOut.Text = "sign out";
            signOut.Click += mnuSignOut_Click;
        }

        private void TaskListByDatePage_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void App_UnhandledExceptionHandled(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = false;
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            progressIndicator = new ProgressIndicator();
            progressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator = progressIndicator;

            IsLoading = true;

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
            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

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
            SmartDispatcher.BeginInvoke(() =>
            {
                IsLoading = true;

                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    CurrentList = App.RtmClient.TaskLists.SingleOrDefault<TaskList>(l => l.Id == id);

                    this.lstAll.ItemsSource = CurrentList.Tasks.ToList<Task>();
                    this.lstToday.ItemsSource = CurrentList.Tasks.Where(t => t.DueDateTime.HasValue &&
                                                                             t.DueDateTime.Value.Date == DateTime.Now.Date).ToList<Task>();
                    this.lstTomorrow.ItemsSource = CurrentList.Tasks.Where(t => t.DueDateTime.HasValue &&
                                                                                t.DueDateTime.Value.Date == DateTime.Now.Date.AddDays(1)).ToList<Task>();
                    this.lstOverdue.ItemsSource = CurrentList.Tasks.Where(t => t.IsLate == true).ToList<Task>();
                    this.lstWeek.ItemsSource = CurrentList.Tasks.Where(t => t.DueDateTime.HasValue &&
                                                                            t.DueDateTime.Value.Date <= DateTime.Now.Date.AddDays(7)).ToList<Task>();

                    ToggleLoadingText();
                    ToggleEmptyText();
                }

                IsLoading = false;
            });
        }

        private void ToggleLoadingText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                this.txtAllLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtTodayLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtTomorrowLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtOverdueLoading.Visibility = System.Windows.Visibility.Collapsed;
                this.txtWeekLoading.Visibility = System.Windows.Visibility.Collapsed;
            });
        }

        private void ToggleEmptyText()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                if ((this.lstAll.ItemsSource as List<Task>).Count() == 0)
                    this.txtAllEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtAllEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if ((this.lstToday.ItemsSource as List<Task>).Count() == 0)
                    this.txtTodayEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtTodayEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if ((this.lstTomorrow.ItemsSource as List<Task>).Count() == 0)
                    this.txtTomorrowEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtTomorrowEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if ((this.lstOverdue.ItemsSource as List<Task>).Count() == 0)
                    this.txtOverdueEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtOverdueEmpty.Visibility = System.Windows.Visibility.Collapsed;

                if ((this.lstWeek.ItemsSource as List<Task>).Count() == 0)
                    this.txtWeekEmpty.Visibility = System.Windows.Visibility.Visible;
                else
                    this.txtWeekEmpty.Visibility = System.Windows.Visibility.Collapsed;
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

        private void btnSelect_Click(object sender, EventArgs e)
        {
            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            target.IsSelectionEnabled = true;
        }

        private void btnComplete_Click(object sender, EventArgs e)
        {
            if (IsLoading) return;

            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to mark the selected task as complete?";
            else
                messageBoxText = "Are you sure you want to mark the selected tasks as complete?";

            if (MessageBox.Show(messageBoxText, "Complete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (target.SelectedItems.Count > 0)
                {
                    CompleteTask((Task)target.SelectedItems[0]);
                    target.SelectedItems.RemoveAt(0);
                }

                target.IsSelectionEnabled = false;
            }
        }

        private void btnPostpone_Click(object sender, EventArgs e)
        {
            if (IsLoading) return;

            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to postpone the selected task?";
            else
                messageBoxText = "Are you sure you want to postpone the selected tasks?";

            if (MessageBox.Show(messageBoxText, "Postpone", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (target.SelectedItems.Count > 0)
                {
                    PostponeTask((Task)target.SelectedItems[0]);
                    target.SelectedItems.RemoveAt(0);
                }

                target.IsSelectionEnabled = false;
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (IsLoading) return;

            MultiselectList target = null;
            if (this.pivLayout.SelectedIndex == 0)
                target = this.lstAll;
            else if (this.pivLayout.SelectedIndex == 1)
                target = this.lstToday;
            else if (this.pivLayout.SelectedIndex == 2)
                target = this.lstTomorrow;
            else if (this.pivLayout.SelectedIndex == 3)
                target = this.lstOverdue;
            else if (this.pivLayout.SelectedIndex == 4)
                target = this.lstWeek;

            string messageBoxText;
            if (target.SelectedItems.Count == 1)
                messageBoxText = "Are you sure you want to delete the selected task?";
            else
                messageBoxText = "Are you sure you want to delete the selected tasks?";

            if (MessageBox.Show(messageBoxText, "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                while (target.SelectedItems.Count > 0)
                {
                    DeleteTask((Task)target.SelectedItems[0]);
                    target.SelectedItems.RemoveAt(0);
                }

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
                this.pivLayout.IsLocked = true;

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
                this.pivLayout.IsLocked = false;

                ApplicationBar.Buttons.Add(add);
                ApplicationBar.Buttons.Add(select);
                ApplicationBar.Buttons.Add(sync);

                ApplicationBar.MenuItems.Add(settings);
                ApplicationBar.MenuItems.Add(about);
                ApplicationBar.MenuItems.Add(feedback);
                ApplicationBar.MenuItems.Add(signOut);
            }
        }

        private void ItemContent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (IsLoading) return;

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

        private void TaskFriendlyDueDate_Loaded(object sender, EventArgs e)
        {
            TextBlock target = (TextBlock)sender;

            Task task = (Task)target.DataContext;

            // set due today
            if (task.DueDateTime.HasValue &&
                task.DueDateTime.Value.Date <= DateTime.Now.Date)
                target.Foreground = (SolidColorBrush)Resources["PhoneAccentBrush"];
            else
                target.Foreground = (SolidColorBrush)Resources["PhoneSubtleBrush"];
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
            IsLoading = true;

            string input = smartAddText;
            if (input.Contains('#') == false)
            {
                if (App.RtmClient.UserSettings != null &&
                    String.IsNullOrEmpty(App.RtmClient.UserSettings.DefaultList) == false &&
                    App.RtmClient.UserSettings.DefaultList != "alltasks")
                {
                    input = input + " #" + App.RtmClient.TaskLists.SingleOrDefault(l => l.Id == App.RtmClient.UserSettings.DefaultList).Name;
                }
            }

            App.RtmClient.AddTask(input, true, null, () =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    IsLoading = false;
                });

                sReload = true;
                LoadData();
            });
        }

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
                    });

                    sReload = true;
                    LoadData();
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
                    });

                    sReload = true;
                    LoadData();
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
                    });

                    sReload = true;
                    LoadData();
                });
            });
        }

        #endregion
    }
}
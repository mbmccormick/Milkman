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
    public partial class LandingPage : PhoneApplicationPage
    {
        #region IsLoading Property

        public static bool sReload = true;

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(LandingPage),
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

        #region Task Lists Property

        public ObservableCollection<TaskList> TaskLists
        {
            get { return (ObservableCollection<TaskList>)GetValue(TaskListsProperty); }
            set { SetValue(TaskListsProperty, value); }
        }

        public static readonly DependencyProperty TaskListsProperty =
               DependencyProperty.Register("TaskLists", typeof(ObservableCollection<TaskList>), typeof(LandingPage),
                   new PropertyMetadata(new ObservableCollection<TaskList>()));

        #endregion

        #region Construction and Navigation

        ProgressIndicator progressIndicator;

        ApplicationBarIconButton add;
        ApplicationBarIconButton sync;

        // Constructor
        public LandingPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(LandingPage_Loaded);
            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            add = new ApplicationBarIconButton();
            add.IconUri = new Uri("/Resources/add.png", UriKind.RelativeOrAbsolute);
            add.Text = "add";
            add.Click += btnAdd_Click;

            sync = new ApplicationBarIconButton();
            sync.IconUri = new Uri("/Resources/retry.png", UriKind.RelativeOrAbsolute);
            sync.Text = "sync";
            sync.Click += btnSync_Click;

            IsLoading = false;
        }

        private void LandingPage_Loaded(object sender, RoutedEventArgs e)
        {
            progressIndicator = new ProgressIndicator();
            progressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator = progressIndicator;

            LoadData();

            SyncData();
        }

        private void App_UnhandledExceptionHandled(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = false;
            });
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

                foreach (TaskList l in App.RtmClient.TaskLists)
                {
                    if (l.Name.ToLower() == "all tasks")
                        tempTaskLists.Insert(0, l);
                    else
                        tempTaskLists.Add(l);
                }

                SmartDispatcher.BeginInvoke(() =>
                {
                    TaskLists = tempTaskLists;
                });

                ToggleLoadingText();
                ToggleEmptyText();
            }
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
                this.NavigationService.Navigate(new Uri("/AuthorizationPage.xaml", UriKind.Relative));
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
            if (IsLoading) return;

            TaskList item = ((FrameworkElement)sender).DataContext as TaskList;

            if (item != null)
                if (item.Name.ToLower() == "all tasks")
                    this.NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                else
                    this.NavigationService.Navigate(new Uri("/TaskListPage.xaml?id=" + item.Id, UriKind.Relative));
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

            if (target.Header.ToString() == "pin to start")
            {
                // do something here
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
                    input = input + " #" + TaskLists.SingleOrDefault(l => l.Id == App.RtmClient.UserSettings.DefaultList).Name;
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

                // SetupNotifications();
            });
        }

        #endregion
    }
}

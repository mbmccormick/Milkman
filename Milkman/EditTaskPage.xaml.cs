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
    public partial class EditTaskPage : PhoneApplicationPage
    {
        #region IsLoading Property

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(EditTaskPage),
                new PropertyMetadata((bool)false));

        private bool loadedDetails = false;

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

        #region Task Property

        public static readonly DependencyProperty TaskProperty =
            DependencyProperty.Register("Task", typeof(Task), typeof(EditTaskPage), new PropertyMetadata(new Task()));

        private Task CurrentTask
        {
            get { return (Task)GetValue(TaskProperty); }
            set { SetValue(TaskProperty, value); }
        }

        public static readonly DependencyProperty TaskListsProperty =
               DependencyProperty.Register("TaskLists", typeof(ObservableCollection<TaskList>), typeof(EditTaskPage),
                   new PropertyMetadata(new ObservableCollection<TaskList>()));

        public ObservableCollection<TaskList> TaskLists
        {
            get { return (ObservableCollection<TaskList>)GetValue(TaskListsProperty); }
            set { SetValue(TaskListsProperty, value); }
        }

        public static readonly DependencyProperty TaskLocationsProperty =
               DependencyProperty.Register("TaskLocations", typeof(ObservableCollection<Location>), typeof(EditTaskPage),
                   new PropertyMetadata(new ObservableCollection<Location>()));

        public ObservableCollection<Location> TaskLocations
        {
            get { return (ObservableCollection<Location>)GetValue(TaskLocationsProperty); }
            set { SetValue(TaskLocationsProperty, value); }
        }

        #endregion

        #region Construction and Navigation

        ProgressIndicator progressIndicator;

        public EditTaskPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EditTaskPage_Loaded);
        }

        private void EditTaskPage_Loaded(object sender, RoutedEventArgs e)
        {

        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            progressIndicator = new ProgressIndicator();
            progressIndicator.IsVisible = true;
            SystemTray.ProgressIndicator = progressIndicator;

            if (!loadedDetails)
            {
                IsLoading = true;
                
                ReloadTask();
                loadedDetails = true;
            }
        }

        #endregion

        #region Loading Data

        private void ReloadTask()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                TaskLists.Clear();
                foreach (TaskList l in App.RtmClient.GetParentableTaskLists(false))
                {
                    TaskLists.Add(l);
                }
                this.lstList.ItemsSource = TaskLists;

                TaskLocations.Clear();
                TaskLocations.Add(new Location("None"));
                foreach (Location l in App.RtmClient.Locations)
                {
                    TaskLocations.Add(l);
                }
                this.lstLocation.ItemsSource = TaskLocations;

                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    CurrentTask = App.RtmClient.GetTask(id);
                }

                if (CurrentTask == null)
                {
                    IsLoading = false;
                    return;
                }

                // name
                if (CurrentTask.Name != null)
                    this.txtName.Text = CurrentTask.Name;

                // due date
                if (CurrentTask.DueDateTime.HasValue)
                {
                    if (CurrentTask.HasDueTime)
                    {
                        this.dtpDueDate.Value = CurrentTask.DueDateTime;
                        this.dtpDueTime.Value = CurrentTask.DueDateTime;
                        this.lstDueDate.SelectedIndex = 2;
                    }
                    else
                    {
                        this.dtpDueDateNoTime.Value = CurrentTask.DueDateTime;
                        this.lstDueDate.SelectedIndex = 1;
                    }
                }

                // priority
                switch (CurrentTask.Priority)
                {
                    case TaskPriority.One:
                        this.lstPriority.SelectedIndex = 1;
                        break;
                    case TaskPriority.Two:
                        this.lstPriority.SelectedIndex = 2;
                        break;
                    case TaskPriority.Three:
                        this.lstPriority.SelectedIndex = 3;
                        break;
                }

                // list
                if (CurrentTask.Parent != null)
                    this.lstList.SelectedItem = CurrentTask.Parent;

                // tags
                if (CurrentTask.TagsString != null)
                    this.txtTags.Text = CurrentTask.TagsString;

                // reepeat
                if (CurrentTask.Recurrence != null)
                this.txtRepeat.Text = CurrentTask.Recurrence;

                // estimate
                if (CurrentTask.Estimate != null)
                    this.txtEstimate.Text = CurrentTask.Estimate;

                // location
                if (CurrentTask.Location != null)
                    this.lstLocation.SelectedItem = CurrentTask.Location;

                IsLoading = false;
            });
        }

        #endregion

        #region Event Handlers

        private void lstDueDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.lstDueDate != null)
            {
                if (this.lstDueDate.SelectedIndex == 0)
                {
                    if (dtpDueDateNoTime != null) dtpDueDateNoTime.Visibility = Visibility.Collapsed;
                    if (dtpDueDateTime != null) dtpDueDateTime.Visibility = Visibility.Collapsed;
                }
                else if (this.lstDueDate.SelectedIndex == 1)
                {
                    if (dtpDueDateNoTime != null) dtpDueDateNoTime.Visibility = Visibility.Visible;
                    if (dtpDueDateTime != null) dtpDueDateTime.Visibility = Visibility.Collapsed;
                }
                else if (this.lstDueDate.SelectedIndex == 2)
                {
                    if (dtpDueDateNoTime != null) dtpDueDateNoTime.Visibility = Visibility.Collapsed;
                    if (dtpDueDateTime != null) dtpDueDateTime.Visibility = Visibility.Visible;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!IsLoading)
            {
                IsLoading = true;

                // change name
                SmartDispatcher.BeginInvoke(() =>
                {
                    CurrentTask.ChangeName(this.txtName.Text, () =>
                    {
                        // change due date
                        SmartDispatcher.BeginInvoke(() =>
                        {
                            DateTime? due;
                            bool hasTime;
                            if (this.lstDueDate.SelectedIndex == 1)
                            {
                                hasTime = false;
                                due = this.dtpDueDateNoTime.Value.Value.Date;
                            }
                            else if (this.lstDueDate.SelectedIndex == 2)
                            {
                                hasTime = true;
                                due = this.dtpDueDate.Value.Value.Date + this.dtpDueTime.Value.Value.TimeOfDay;
                            }
                            else
                            {
                                hasTime = false;
                                due = null;
                            }

                            CurrentTask.ChangeDue(due, hasTime, () =>
                            {
                                // change priority
                                SmartDispatcher.BeginInvoke(() =>
                                {
                                    TaskPriority p;
                                    if (this.lstPriority.SelectedIndex == 1) p = TaskPriority.One;
                                    else if (this.lstPriority.SelectedIndex == 2) p = TaskPriority.Two;
                                    else if (this.lstPriority.SelectedIndex == 3) p = TaskPriority.Three;
                                    else p = TaskPriority.None;
                                    CurrentTask.ChangePriority(p, () =>
                                    {
                                        // change list
                                        SmartDispatcher.BeginInvoke(() =>
                                        {
                                            CurrentTask.ChangeList((TaskList)this.lstList.SelectedItem, () =>
                                            {
                                                // change tags
                                                SmartDispatcher.BeginInvoke(() =>
                                                {
                                                    string[] tags = this.txtTags.Text.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                                                    CurrentTask.ChangeTags(tags, () =>
                                                    {    
                                                        // change repeat
                                                        SmartDispatcher.BeginInvoke(() =>
                                                        {
                                                            CurrentTask.ChangeRecurrence(this.txtRepeat.Text, () =>
                                                            {
                                                                // change estimate
                                                                SmartDispatcher.BeginInvoke(() =>
                                                                {
                                                                    CurrentTask.ChangeEstimate(this.txtEstimate.Text, () =>
                                                                    {
                                                                        // change location
                                                                        SmartDispatcher.BeginInvoke(() =>
                                                                        {
                                                                            Location tmpLocation = (Location)this.lstLocation.SelectedItem;
                                                                            if (this.lstLocation.SelectedIndex == 0) tmpLocation = null;

                                                                            CurrentTask.ChangeLocation(tmpLocation, () =>
                                                                            {
                                                                                // sync all tasks
                                                                                App.RtmClient.CacheTasks(() =>
                                                                                {
                                                                                    // now we can go back to task page
                                                                                    SmartDispatcher.BeginInvoke(() =>
                                                                                    {
                                                                                        IsLoading = false;

                                                                                        if (this.NavigationService.CanGoBack)
                                                                                            this.NavigationService.GoBack();
                                                                                        else
                                                                                            NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + CurrentTask.Id, UriKind.Relative));
                                                                                    });
                                                                                });
                                                                            });
                                                                        });
                                                                    });
                                                                });
                                                            });
                                                        });
                                                    });
                                                });
                                            });
                                        });
                                    });
                                });
                            });
                        });
                    });
                });
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
            else
                NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + CurrentTask.Id, UriKind.Relative));
        }

        #endregion
    }
}
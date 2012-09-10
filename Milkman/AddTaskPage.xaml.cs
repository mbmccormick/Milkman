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

namespace Milkman
{
    public partial class AddTaskPage : PhoneApplicationPage
    {
        #region TaskLists Property

        public static readonly DependencyProperty TaskListsProperty =
            DependencyProperty.Register("TaskLists", typeof(ObservableCollection<TaskList>), typeof(EditTaskPage), new PropertyMetadata(new ObservableCollection<TaskList>()));

        private ObservableCollection<TaskList> TaskLists
        {
            get { return (ObservableCollection<TaskList>)GetValue(TaskListsProperty); }
            set { SetValue(TaskListsProperty, value); }
        }

        #endregion

        #region TaskLocations Property

        public static readonly DependencyProperty TaskLocationsProperty =
            DependencyProperty.Register("TaskLocations", typeof(ObservableCollection<Location>), typeof(EditTaskPage), new PropertyMetadata(new ObservableCollection<Location>()));

        private ObservableCollection<Location> TaskLocations
        {
            get { return (ObservableCollection<Location>)GetValue(TaskLocationsProperty); }
            set { SetValue(TaskLocationsProperty, value); }
        }

        #endregion

        #region Construction and Navigation

        ApplicationBarIconButton save;
        ApplicationBarIconButton cancel;

        public AddTaskPage()
        {
            InitializeComponent();

            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);

            this.BuildApplicationBar();
        }

        private void BuildApplicationBar()
        {
            save = new ApplicationBarIconButton();
            save.IconUri = new Uri("/Resources/save.png", UriKind.RelativeOrAbsolute);
            save.Text = Strings.SaveMenuLower;
            save.Click += btnSave_Click;

            cancel = new ApplicationBarIconButton();
            cancel.IconUri = new Uri("/Resources/cancel.png", UriKind.RelativeOrAbsolute);
            cancel.Text = Strings.CancelMenuLower;
            cancel.Click += btnCancel_Click;

            // build application bar
            ApplicationBar.Buttons.Add(save);
            ApplicationBar.Buttons.Add(cancel);
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
            LoadData();

            base.OnNavigatedTo(e);
        }

        #endregion

        #region Loading Data

        private void LoadData()
        {
            SmartDispatcher.BeginInvoke(() =>
            {
                // bind lists list picker
                TaskLists.Clear();
                foreach (TaskList l in App.RtmClient.GetParentableTaskLists(false))
                {
                    TaskLists.Add(l);
                }

                this.lstList.ItemsSource = TaskLists;

                // bind locations list picker
                TaskLocations.Clear();
                TaskLocations.Add(new Location(Strings.NoneLower));
                foreach (Location l in App.RtmClient.Locations)
                {
                    TaskLocations.Add(l);
                }

                this.lstLocation.ItemsSource = TaskLocations;

                GlobalLoading.Instance.IsLoading = false;
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
            if (!GlobalLoading.Instance.IsLoading)
            {
                GlobalLoading.Instance.IsLoadingText(Strings.SavingTask);

                Task task = new Task();
                
                // set name
                task.Name = this.txtName.Text;

                // set due date
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

                task.DueDateTime = due;
                
                // set priority
                TaskPriority p;
                if (this.lstPriority.SelectedIndex == 1) p = TaskPriority.One;
                else if (this.lstPriority.SelectedIndex == 2) p = TaskPriority.Two;
                else if (this.lstPriority.SelectedIndex == 3) p = TaskPriority.Three;
                else p = TaskPriority.None;

                task.Priority = p;

                // set repeat
                task.Recurrence = this.txtRepeat.Text;

                // set estimate
                task.Estimate = this.txtEstimate.Text;

                // set tags
                task.SetTags(this.txtTags.Text, new char[] { ',' });

                // set location
                Location tmpLocation = (Location)this.lstLocation.SelectedItem;
                if (this.lstLocation.SelectedIndex == 0) tmpLocation = null;

                task.Location = tmpLocation;

                // create task
                SmartDispatcher.BeginInvoke(() =>
                {
                    App.RtmClient.TaskLists.SingleOrDefault(z => z.Id == (this.lstList.SelectedItem as TaskList).Id).AddTask(task, () =>
                    {
                        
                    });
                });
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }

        #endregion
    }
}
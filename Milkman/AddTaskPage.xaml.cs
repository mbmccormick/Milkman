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
using System.Text;

namespace Milkman
{
    public partial class AddTaskPage : PhoneApplicationPage
    {
        private bool loadedDetails = false;

        #region TaskLists Property

        public static readonly DependencyProperty TaskListsProperty =
            DependencyProperty.Register("TaskLists", typeof(ObservableCollection<TaskList>), typeof(AddTaskPage), new PropertyMetadata(new ObservableCollection<TaskList>()));

        private ObservableCollection<TaskList> TaskLists
        {
            get { return (ObservableCollection<TaskList>)GetValue(TaskListsProperty); }
            set { SetValue(TaskListsProperty, value); }
        }

        #endregion

        #region TaskLocations Property

        public static readonly DependencyProperty TaskLocationsProperty =
            DependencyProperty.Register("TaskLocations", typeof(ObservableCollection<Location>), typeof(AddTaskPage), new PropertyMetadata(new ObservableCollection<Location>()));

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
            if (loadedDetails == false)
            {
                GlobalLoading.Instance.IsLoadingText(Strings.Loading);

                LoadData();
                loadedDetails = true;
            }

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

                ToggleLoadingText();

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
                    if (this.dtpDueDateNoTime != null) this.dtpDueDateNoTime.Visibility = Visibility.Collapsed;
                    if (this.grdDueDateTime != null) this.grdDueDateTime.Visibility = Visibility.Collapsed;
                }
                else if (this.lstDueDate.SelectedIndex == 1)
                {
                    if (this.dtpDueDateNoTime != null) this.dtpDueDateNoTime.Visibility = Visibility.Visible;
                    if (this.grdDueDateTime != null) this.grdDueDateTime.Visibility = Visibility.Collapsed;
                }
                else if (this.lstDueDate.SelectedIndex == 2)
                {
                    if (this.dtpDueDateNoTime != null) this.dtpDueDateNoTime.Visibility = Visibility.Collapsed;
                    if (this.grdDueDateTime != null) this.grdDueDateTime.Visibility = Visibility.Visible;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!GlobalLoading.Instance.IsLoading)
            {
                GlobalLoading.Instance.IsLoadingText(Strings.SavingTask);

                StringBuilder smartText = new StringBuilder();

                if (this.txtName.Text.Length > 0)
                {
                    // set name
                    smartText.Append(this.txtName.Text);
                    smartText.Append(" ");

                    // set date
                    if (this.lstDueDate.SelectedIndex == 1)
                    {
                        smartText.Append("^" + this.dtpDueDateNoTime.ValueString);
                        smartText.Append(" ");
                    }
                    else if (this.lstDueDate.SelectedIndex == 2)
                    {
                        smartText.Append("^" + this.dtpDueDate.ValueString + " " + this.dtpDueTime.ValueString);
                        smartText.Append(" ");
                    }

                    // set priority
                    if (this.lstPriority.SelectedIndex == 1)
                    {
                        smartText.Append("!1");
                        smartText.Append(" ");
                    }
                    else if (this.lstPriority.SelectedIndex == 2)
                    {
                        smartText.Append("!2");
                        smartText.Append(" ");
                    }
                    else if (this.lstPriority.SelectedIndex == 3)
                    {
                        smartText.Append("!3");
                        smartText.Append(" ");
                    }

                    // set list
                    smartText.Append("#" + (this.lstList.SelectedItem as TaskList).Name);
                    smartText.Append(" ");

                    // set repeat
                    if (this.txtRepeat.Text.Length > 0)
                    {
                        smartText.Append("*" + this.txtRepeat.Text);
                        smartText.Append(" ");
                    }

                    // set estimate
                    if (this.txtEstimate.Text.Length > 0)
                    {
                        smartText.Append("=" + this.txtEstimate.Text);
                        smartText.Append(" ");
                    }

                    // set tags
                    if (this.txtTags.Text.Length > 0)
                    {
                        foreach (string tag in this.txtTags.Text.Split(','))
                        {
                            smartText.Append("#" + tag.Trim());
                            smartText.Append(" ");
                        }
                    }

                    // set location
                    if (this.lstLocation.SelectedIndex > 0)
                    {
                        smartText.Append("@" + (this.lstLocation.SelectedItem as Location).Name);
                        smartText.Append(" ");
                    }
                }

                App.RtmClient.AddTask(smartText.ToString(), true, null, () =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GlobalLoading.Instance.IsLoading = false;

                        if (NavigationService.CanGoBack)
                            NavigationService.GoBack();
                        else
                            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                    });
                });
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        #endregion
    }
}
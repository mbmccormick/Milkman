using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Milkman.Common;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Milkman
{
    public partial class EditTaskPage : PhoneApplicationPage
    {
        private bool loadedDetails = false;

        #region Task Property

        private Task CurrentTask = null;

        #endregion

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

        public EditTaskPage()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(EditTaskPage_Loaded);

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

        private void EditTaskPage_Loaded(object sender, RoutedEventArgs e)
        {
            this.scvLayout.UpdateLayout();
            this.scvLayout.ScrollToVerticalOffset(0);
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
                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
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

                    CurrentTask = App.RtmClient.GetTask(id);

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
                    if (CurrentTask.Parent != null &&
                        this.lstList.Items.Contains(CurrentTask.Parent))
                        this.lstList.SelectedItem = CurrentTask.Parent;

                    // repeat
                    if (CurrentTask.Recurrence != null)
                        this.txtRepeat.Text = CurrentTask.Recurrence;

                    // estimate
                    if (CurrentTask.Estimate != null)
                        this.txtEstimate.Text = CurrentTask.Estimate;

                    // tags
                    if (CurrentTask.TagsString != null)
                        this.txtTags.Text = CurrentTask.TagsString;

                    // location
                    if (CurrentTask.Location != null &&
                        this.lstLocation.Items.Contains(CurrentTask.Location))
                        this.lstLocation.SelectedItem = CurrentTask.Location;

                    // url
                    if (CurrentTask.Url != null)
                        this.txtURL.Text = CurrentTask.Url;

                    ToggleLoadingText();
                }

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
                                                                // change tags
                                                                SmartDispatcher.BeginInvoke(() =>
                                                                {
                                                                    string[] tags = this.txtTags.Text.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                                                                    CurrentTask.ChangeTags(tags, () =>
                                                                    {
                                                                        // change location
                                                                        SmartDispatcher.BeginInvoke(() =>
                                                                        {
                                                                            Location tmpLocation = (Location)this.lstLocation.SelectedItem;
                                                                            if (this.lstLocation.SelectedIndex == 0) tmpLocation = null;

                                                                            CurrentTask.ChangeLocation(tmpLocation, () =>
                                                                            {
                                                                                // change url
                                                                                SmartDispatcher.BeginInvoke(() =>
                                                                                {
                                                                                    CurrentTask.ChangeUrl(this.txtURL.Text, () =>
                                                                                    {
                                                                                        // sync all tasks
                                                                                        App.RtmClient.CacheTasks(() =>
                                                                                        {
                                                                                            // now we can go back to task page
                                                                                            SmartDispatcher.BeginInvoke(() =>
                                                                                            {
                                                                                                GlobalLoading.Instance.IsLoading = false;

                                                                                                if (NavigationService.CanGoBack)
                                                                                                    NavigationService.GoBack();
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
                    });
                });
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + CurrentTask.Id, UriKind.Relative));
        }

        private void ListPicker_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (GlobalLoading.Instance.IsLoading == false)
            {
                try
                {
                    this.scvLayout.UpdateLayout();
                    this.scvLayout.ScrollToVerticalOffset(this.scvLayout.ScrollableHeight);
                }
                catch (Exception ex)
                {
                    // do nothing
                }
            }
        }

        #endregion
    }
}
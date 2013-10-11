using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Milkman.Common;
using System;
using System.Windows;

namespace Milkman
{
    public partial class AddNotePage : PhoneApplicationPage
    {
        private bool loadedDetails = false;

        #region Task Property

        private Task CurrentTask = null;

        #endregion

        #region Construction and Navigation

        ApplicationBarIconButton save;
        ApplicationBarIconButton cancel;

        public AddNotePage()
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
            if (!loadedDetails)
            {
                GlobalLoading.Instance.IsLoadingText(Strings.Loading);

                ReloadTask();
                loadedDetails = true;
            }

            base.OnNavigatedTo(e);
        }

        #endregion

        #region Loading Data

        private void ReloadTask()
        {
            // load task
            string taskId;
            if (NavigationContext.QueryString.TryGetValue("task", out taskId))
            {
                CurrentTask = App.RtmClient.GetTask(taskId);
            }

            GlobalLoading.Instance.IsLoading = false;
        }

        #endregion

        #region Event Handlers

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!GlobalLoading.Instance.IsLoading)
            {
                GlobalLoading.Instance.IsLoadingText(Strings.AddingNote);

                // add note
                SmartDispatcher.BeginInvoke(() =>
                {
                    this.txtBody.Text = this.txtBody.Text.Replace("\r", "\n");

                    CurrentTask.AddNote(this.txtTitle.Text, this.txtBody.Text, () =>
                    {
                        App.RtmClient.CacheTasks(() =>
                        {
                            Dispatcher.BeginInvoke(() =>
                            {
                                GlobalLoading.Instance.IsLoading = false;
                                NavigationService.GoBack();
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

        #endregion
    }
}
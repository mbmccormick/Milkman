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
    public partial class AddNotePage : PhoneApplicationPage
    {
        private bool loadedDetails = false;

        #region Task Property

        private Task CurrentTask = null;

        #endregion

        #region Construction and Navigation

        public AddNotePage()
        {
            InitializeComponent();
            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);
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
                GlobalLoading.Instance.IsLoading = true;

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
                GlobalLoading.Instance.IsLoading = true;

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
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
            else
                NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + CurrentTask.Id, UriKind.Relative));
        }

        #endregion
    }
}
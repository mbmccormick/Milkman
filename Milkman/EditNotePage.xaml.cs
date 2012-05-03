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
    public partial class EditNotePage : PhoneApplicationPage
    {
        private bool loadedDetails = false;

        #region Task Property

        private Task CurrentTask = null;
        private TaskNote CurrentNote = null;

        #endregion

        #region Construction and Navigation

        public EditNotePage()
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
                GlobalLoading.Instance.IsLoadingText("Loading...");

                ReloadNote();
                loadedDetails = true;
            }

            base.OnNavigatedTo(e);
        }

        #endregion

        #region Loading Data

        private void ReloadNote()
        {
            // load task
            string taskId;
            if (NavigationContext.QueryString.TryGetValue("task", out taskId))
            {
                CurrentTask = App.RtmClient.GetTask(taskId);

                // load note
                string id;
                if (NavigationContext.QueryString.TryGetValue("id", out id))
                {
                    CurrentNote = CurrentTask.GetNote(id);

                    // bind title
                    this.txtTitle.Text = CurrentNote.Title;

                    // bind body
                    this.txtBody.Text = CurrentNote.Body;
                }
            }

            GlobalLoading.Instance.IsLoading = false;
        }

        #endregion

        #region Event Handlers

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!GlobalLoading.Instance.IsLoading)
            {
                GlobalLoading.Instance.IsLoadingText("Loading...");

                // edit note
                SmartDispatcher.BeginInvoke(() =>
                {
                    this.txtBody.Text = this.txtBody.Text.Replace("\r", "\n");

                    CurrentTask.EditNote(CurrentNote, this.txtTitle.Text, this.txtBody.Text, () =>
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            GlobalLoading.Instance.IsLoading = false;
                            NavigationService.GoBack();
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

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!GlobalLoading.Instance.IsLoading)
            {
                if (MessageBox.Show("Are you sure you want to delete this note?", "Delete", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    GlobalLoading.Instance.IsLoadingText("Loading...");

                    // delete note
                    SmartDispatcher.BeginInvoke(() =>
                    {
                        CurrentTask.DeleteNote(CurrentNote, () =>
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
        }

        #endregion
    }
}
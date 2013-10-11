using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Milkman.Common;
using System;
using System.Windows;

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

        ApplicationBarIconButton save;
        ApplicationBarIconButton delete;
        ApplicationBarIconButton cancel;

        public EditNotePage()
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

            delete = new ApplicationBarIconButton();
            delete.IconUri = new Uri("/Resources/delete.png", UriKind.RelativeOrAbsolute);
            delete.Text = Strings.DeleteMenuLower;
            delete.Click += btnDelete_Click;

            cancel = new ApplicationBarIconButton();
            cancel.IconUri = new Uri("/Resources/cancel.png", UriKind.RelativeOrAbsolute);
            cancel.Text = Strings.CancelMenuLower;
            cancel.Click += btnCancel_Click;

            // build application bar
            ApplicationBar.Buttons.Add(save);
            ApplicationBar.Buttons.Add(delete);
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
                GlobalLoading.Instance.IsLoadingText(Strings.SavingNote);

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
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(new Uri("/TaskDetailsPage.xaml?id=" + CurrentTask.Id, UriKind.Relative));
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (!GlobalLoading.Instance.IsLoading)
            {
                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Caption = Strings.DeleteNoteDialogTitle,
                    Message = Strings.DeleteNoteDialog,
                    LeftButtonContent = Strings.YesLower,
                    RightButtonContent = Strings.NoLower,
                    IsFullScreen = false
                };

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            GlobalLoading.Instance.IsLoadingText(Strings.DeletingNote);

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

                            break;
                        default:
                            break;
                    }
                };

                messageBox.Show();
            }
        }

        #endregion
    }
}
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
using IronCow;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Milkman.Common;

namespace Milkman
{
    public partial class AuthorizationPage : PhoneApplicationPage
    {
        public static bool sReload = true;

        #region Construction and Navigation

        private string Frob { get; set; }

        public AuthorizationPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(AuthorizationPage_Loaded);
            App.UnhandledExceptionHandled += new EventHandler<ApplicationUnhandledExceptionEventArgs>(App_UnhandledExceptionHandled);
        }

        private void AuthorizationPage_Loaded(object sender, RoutedEventArgs e)
        {
            StartAuth();
            MessageBox.Show("Sign in to Remember The Milk to authorize Milkman. When you finish the authorization process, tap the Complete button to continue.", "Authorization", MessageBoxButton.OK);
        }

        private void App_UnhandledExceptionHandled(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                GlobalLoading.Instance.IsLoading = false;
            });
        }

        private void StartAuth()
        {
            GlobalLoading.Instance.IsLoading = true;

            App.RtmClient.GetFrob((string frob) =>
            {
                Frob = frob;
                string url = App.RtmClient.GetAuthenticationUrl(frob, AuthenticationPermissions.Delete);

                Dispatcher.BeginInvoke(() =>
                {
                    GlobalLoading.Instance.IsLoading = false;
                    webAuthorization.Navigate(new Uri(url));
                });
            });
        }

        #endregion

        #region Event Handling

        private void btnComplete_Click(object sender, EventArgs e)
        {
            // only do something if Frob is present
            if (!string.IsNullOrEmpty(Frob))
            {
                GlobalLoading.Instance.IsLoading = true;
                App.RtmClient.GetToken(Frob, (string token, User user) =>
                {
                    // create timeline
                    App.RtmClient.GetOrStartTimeline((int timeline) =>
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            GlobalLoading.Instance.IsLoading = false;

                            if (NavigationService.CanGoBack)
                            {
                                MainPage.sReload = true;

                                NavigationService.RemoveBackEntry();
                                NavigationService.Navigate(new Uri("/MainPage.xaml?IsFirstRun=true", UriKind.Relative));
                            }
                        });
                    });
                });
            }
        }

        private void btnRetry_Click(object sender, EventArgs e)
        {
            StartAuth();
        }

        private void webAuthorization_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            GlobalLoading.Instance.IsLoading = false;
        }

        private void webAuthorization_Navigating(object sender, NavigatingEventArgs e)
        {
            GlobalLoading.Instance.IsLoading = true;
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

        private void mnuDonate_Click(object sender, EventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();

            webBrowserTask.Uri = new Uri("http://mbmccormick.com/donate/", UriKind.Absolute);
            webBrowserTask.Show();
        }

        #endregion
    }
}
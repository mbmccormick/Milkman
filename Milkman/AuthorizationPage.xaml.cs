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

namespace Milkman
{
    public partial class AuthorizationPage : PhoneApplicationPage
    {
        #region IsLoading Property

        /// <summary>
        /// IsLoading Dependency Property
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register("IsLoading", typeof(bool), typeof(AuthorizationPage),
                new PropertyMetadata((bool)false));

        /// <summary>
        /// Gets or sets the IsLoading property. This dependency property 
        /// indicates whether we are currently loading.
        /// </summary>
        public bool IsLoading
        {
            get { return (bool)GetValue(IsLoadingProperty); }
            set { SetValue(IsLoadingProperty, value); }
        }

        #endregion

        private string Frob { get; set; }

        public AuthorizationPage()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(AuthorizationPage_Loaded);
        }

        private void StartAuth()
        {
            this.IsLoading = true;

            App.RtmClient.GetFrob((string frob) =>
            {
                Frob = frob;
                string url = App.RtmClient.GetAuthenticationUrl(frob, AuthenticationPermissions.Delete);

                Dispatcher.BeginInvoke(() =>
                {
                    this.IsLoading = false;
                    webAuthorization.Navigate(new Uri(url));
                });
            });
        }

        private void AuthorizationPage_Loaded(object sender, RoutedEventArgs e)
        {
            StartAuth();
            MessageBox.Show("Login to Remember The Milk to authorize Milkman. When you finish the authorization process, tap the Complete button to continue.", "Authorization", MessageBoxButton.OK);
        }

        #region Event Handling

        private void btnComplete_Click(object sender, EventArgs e)
        {
            // only do something if Frob is present
            if (!string.IsNullOrEmpty(Frob))
            {
                IsLoading = true;
                App.RtmClient.GetToken(Frob, (string token, User user) =>
                {
                    // create timeline
                    App.RtmClient.GetOrStartTimeline((int timeline) =>
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            IsLoading = false;

                            if (NavigationService.CanGoBack)
                            {
                                MainPage.sReload = true;
                                NavigationService.GoBack();
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
            this.IsLoading = false;
        }

        private void webAuthorization_Navigating(object sender, NavigatingEventArgs e)
        {
            this.IsLoading = true;
        }

        #endregion
    }
}
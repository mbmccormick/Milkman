using IronCow;
using IronCow.Resources;
using IronCow.Rest;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Microsoft.WindowsAzure.MobileServices;
using Milkman.Common;
using Milkman.Common.Models;
using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using Windows.Phone.Speech.VoiceCommands;

namespace Milkman
{
    public partial class App : Application
    {
        private static readonly string RtmApiKey = "09b03090fc9303804aedd945872fdefc";
        private static readonly string RtmSharedKey = "d2ffaf49356b07f9";

        public static Rtm RtmClient;
        public static Response TasksResponse;
        public static Response ListsResponse;
        public static Response LocationsResponse;
        public static DateTime LastUpdated;

        public static event EventHandler<ApplicationUnhandledExceptionEventArgs> UnhandledExceptionHandled;

        public static MobileServiceClient MobileService = new MobileServiceClient(
            "https://milkman.azure-mobile.net/",
            "dSiXpghIVfinlEYGqpGROOvclqFWnl56"
        );

        public static string VersionNumber
        {
            get
            {
                string assembly = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                string[] version = assembly.Split('=')[1].Split(',')[0].Split('.');

                return version[0] + "." + version[1];
            }
        }

        public static string ExtendedVersionNumber
        {
            get
            {
                string assembly = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                string[] version = assembly.Split('=')[1].Split(',')[0].Split('.');

                return version[0] + "." + version[1] + "." + version[2];
            }
        }

        public static string PlatformVersionNumber
        {
            get
            {
                return System.Environment.OSVersion.Version.ToString(3);
            }
        }

        public PhoneApplicationFrame RootFrame { get; private set; }

        public App()
        {
            UnhandledException += Application_UnhandledException;

            InitializeComponent();

            InitializePhoneApplication();

            RootFrame.Navigating += new NavigatingCancelEventHandler(RootFrame_Navigating);

            if (System.Diagnostics.Debugger.IsAttached)
                MetroGridHelper.IsVisible = true;
        }

        public static async void LoadData()
        {
            string RtmAuthToken = IsolatedStorageHelper.GetObject<string>("RtmAuthToken");
            int? Timeline = IsolatedStorageHelper.GetObject<int?>("RtmTimeline");
            TasksResponse = IsolatedStorageHelper.GetObject<Response>("TasksResponse");
            ListsResponse = IsolatedStorageHelper.GetObject<Response>("ListsResponse");
            LocationsResponse = IsolatedStorageHelper.GetObject<Response>("LocationsResponse");
            LastUpdated = IsolatedStorageHelper.GetObject<DateTime>("LastUpdated");

            if (!string.IsNullOrEmpty(RtmAuthToken))
            {
                RtmClient = new Rtm(RtmApiKey, RtmSharedKey, RtmAuthToken);
            }
            else
            {
                RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            }

            RtmClient.Client.UseHttps = true;

            if (Timeline.HasValue)
            {
                RtmClient.CurrentTimeline = Timeline.Value;
            }

            RtmClient.CacheTasksEvent += OnCacheTasks;
            RtmClient.CacheListsEvent += OnCacheLists;
            RtmClient.CacheLocationsEvent += OnCacheLocations;

            if (LocationsResponse != null)
            {
                RtmClient.LoadLocationsFromResponse(LocationsResponse);
            }
            if (ListsResponse != null)
            {
                RtmClient.LoadListsFromResponse(ListsResponse);
            }
            if (TasksResponse != null)
            {
                RtmClient.LoadTasksFromResponse(TasksResponse);
            }

            RtmClient.Resources = App.Current.Resources;

            await VoiceCommandService.InstallCommandSetsFromFileAsync(new Uri("ms-appx:///Commands.xml"));
        }

        public static void SaveData()
        {
            IsolatedStorageHelper.SaveObject<string>("RtmAuthToken", RtmClient.AuthToken);
            IsolatedStorageHelper.SaveObject<int?>("RtmTimeline", RtmClient.CurrentTimeline);
            IsolatedStorageHelper.SaveObject<Response>("TasksResponse", TasksResponse);
            IsolatedStorageHelper.SaveObject<Response>("ListsResponse", ListsResponse);
            IsolatedStorageHelper.SaveObject<Response>("LocationsResponse", LocationsResponse);
            IsolatedStorageHelper.SaveObject<DateTime>("LastUpdated", LastUpdated);

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static void DeleteData()
        {
            IsolatedStorageHelper.DeleteObject("RtmAuthToken");
            IsolatedStorageHelper.DeleteObject("RtmTimeline");
            IsolatedStorageHelper.DeleteObject("TasksResponse");
            IsolatedStorageHelper.DeleteObject("ListsResponse");
            IsolatedStorageHelper.DeleteObject("LocationsResponse");
            IsolatedStorageHelper.DeleteObject("LastUpdated");

            RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            TasksResponse = null;
            ListsResponse = null;
            LocationsResponse = null;

            RtmClient.Resources = App.Current.Resources;

            NotificationsManager.ResetLiveTiles();

            if (ScheduledActionService.Find("BackgroundWorker") != null)
                ScheduledActionService.Remove("BackgroundWorker");

            foreach (var item in ScheduledActionService.GetActions<Microsoft.Phone.Scheduler.Reminder>())
                ScheduledActionService.Remove(item.Name);

            ShellTile primaryTile = ShellTile.ActiveTiles.First();
            if (primaryTile != null)
            {
                StandardTileData data = new StandardTileData();
                primaryTile.Update(data);
            }
        }

        public static void OnCacheTasks(Response response)
        {
            TasksResponse = response;
            LastUpdated = DateTime.Now;
        }

        public static void OnCacheLists(Response response)
        {
            ListsResponse = response;
            LastUpdated = DateTime.Now;
        }

        public static void OnCacheLocations(Response response)
        {
            LocationsResponse = response;
            LastUpdated = DateTime.Now;
        }

        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            SmartDispatcher.Initialize(RootFrame.Dispatcher);

            LoadData();

            if (!string.IsNullOrEmpty(App.RtmClient.AuthToken))
            {
                AcquirePushChannel(0.0, 0.0);
            }
        }

        public static void PromptForMarketplaceReview()
        {
            string currentVersion = App.VersionNumber;
            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<string>("CurrentVersion", out currentVersion) == false)
                currentVersion = App.VersionNumber;

            DateTime installDate = DateTime.UtcNow;
            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<DateTime>("InstallDate", out installDate) == false)
                installDate = DateTime.UtcNow;

            if (currentVersion != App.VersionNumber) // override if this is a new version
                installDate = DateTime.UtcNow;

            if (DateTime.UtcNow.AddDays(-3) >= installDate) // prompt after 3 days
            {
                CustomMessageBox messageBox = new CustomMessageBox()
                {
                    Caption = Strings.MarketplaceDialogTitle,
                    Message = Strings.MarketplaceDialog,
                    LeftButtonContent = Strings.YesLower,
                    RightButtonContent = Strings.NoLower,
                    IsFullScreen = false
                };

                messageBox.Dismissed += (s1, e1) =>
                {
                    switch (e1.Result)
                    {
                        case CustomMessageBoxResult.LeftButton:
                            installDate = DateTime.MaxValue; // they have rated, don't prompt again

                            IsolatedStorageSettings.ApplicationSettings["CurrentVersion"] = currentVersion; // save current version of application
                            IsolatedStorageSettings.ApplicationSettings["InstallDate"] = installDate; // save install date

                            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                            marketplaceReviewTask.Show();

                            break;
                        default:
                            installDate = DateTime.UtcNow; // they did not rate, prompt again in 2 days

                            IsolatedStorageSettings.ApplicationSettings["CurrentVersion"] = currentVersion; // save current version of application
                            IsolatedStorageSettings.ApplicationSettings["InstallDate"] = installDate; // save install date

                            break;
                    }
                };

                messageBox.Show();
            }

            IsolatedStorageSettings.ApplicationSettings["CurrentVersion"] = currentVersion; // save current version of application
            IsolatedStorageSettings.ApplicationSettings["InstallDate"] = installDate; // save install date
        }

        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            LoadData();
        }

        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            SaveData();
        }

        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            SaveData();
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            LittleWatson.ReportException(e.Exception, "RootFrame_NavigationFailed()");

            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri.ToString().Contains("/MainPage.xaml") == false) return;

            if (string.IsNullOrEmpty(App.RtmClient.AuthToken))
            {
                e.Cancel = true;
                RootFrame.Dispatcher.BeginInvoke(delegate
                {
                    RootFrame.Navigate(new Uri("/WelcomePage.xaml", UriKind.Relative));
                });
            }
            else
            {
                RootFrame.Dispatcher.BeginInvoke(delegate
                {
                    RootFrame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                });
            }
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is RtmException)
            {
                var ex = (e.ExceptionObject as RtmException).ResponseError;
                RootFrame.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(ex.Message, Strings.Error + " " + ex.Code, MessageBoxButton.OK);
                });
            }
            else
            {
                LittleWatson.ReportException(e.ExceptionObject, null);

                RootFrame.Dispatcher.BeginInvoke(() =>
                {
                    LittleWatson.CheckForPreviousException(false);
                });
            }

            e.Handled = true;

            if (UnhandledExceptionHandled != null)
                UnhandledExceptionHandled(sender, e);

            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Push Notifications

        public static HttpNotificationChannel CurrentChannel { get; private set; }
        
        public static async void AcquirePushChannel(double latitude, double longitude)
        {
            CurrentChannel = HttpNotificationChannel.Find("MyPushChannel");
            
            if (CurrentChannel == null)
            {
                CurrentChannel = new HttpNotificationChannel("MyPushChannel");
                CurrentChannel.Open();
                CurrentChannel.BindToShellToast();
            }

            IMobileServiceTable<Registrations> registrationsTable = App.MobileService.GetTable<Registrations>();

            var existingRegistrations = await registrationsTable.Where(z => z.AuthenticationToken == App.RtmClient.AuthToken).ToCollectionAsync();

            if (existingRegistrations.Count > 0)
            {
                var registration = existingRegistrations.First();
                registration.Handle = CurrentChannel.ChannelUri.AbsoluteUri;
                registration.Latitude = latitude;
                registration.Longitude = longitude;
                registration.ReminderInterval = new AppSettings().TaskRemindersEnabled;
                                
                await registrationsTable.UpdateAsync(registration);
            }
            else
            {
                var registration = new Registrations
                {
                    AuthenticationToken = App.RtmClient.AuthToken,
                    Handle = CurrentChannel.ChannelUri.AbsoluteUri,
                    Latitude = latitude,
                    Longitude = longitude,
                    ReminderInterval = new AppSettings().TaskRemindersEnabled
                };

                await registrationsTable.InsertAsync(registration);
            }
        }

        #endregion

        #region Phone application initialization

        private bool phoneApplicationInitialized = false;

        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            GlobalLoading.Instance.Initialize(RootFrame);

            phoneApplicationInitialized = true;
        }

        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            Rtm.Dispatcher = App.Current.RootVisual.Dispatcher;
            SmartDispatcher.Initialize();

            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}
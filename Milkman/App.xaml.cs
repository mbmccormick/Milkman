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
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.Http;
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
        public static Response SettingsResponse;
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
            SettingsResponse = IsolatedStorageHelper.GetObject<Response>("SettingsResponse");
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
            RtmClient.CacheUserSettingsEvent += OnCacheSettings;

            if (SettingsResponse != null)
            {
                RtmClient.LoadUserSettingsFromResponse(SettingsResponse);
            }
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
            IsolatedStorageHelper.SaveObject<Response>("SettingsResponse", SettingsResponse);
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
            IsolatedStorageHelper.DeleteObject("SettingsResponse");
            IsolatedStorageHelper.DeleteObject("LastUpdated");

            RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            TasksResponse = null;
            ListsResponse = null;
            LocationsResponse = null;
            SettingsResponse = null;

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

        public static void OnCacheSettings(Response response)
        {
            SettingsResponse = response;
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

        #region Timezone

        public static void CheckTimezone()
        {
            string rtmTimezone = OlsonTimeZoneToTimeZoneInfo(App.RtmClient.UserSettings.TimeZone);

            if (rtmTimezone != TimeZoneInfo.Local.StandardName)
            {
                MessageBox.Show("The timezone on your Remember The Milk account does not match the timezone that your phone is set to. This may cause problems with due dates and reminders.", "Timezone mismatch", MessageBoxButton.OK);
            }
        }

        public static string OlsonTimeZoneToTimeZoneInfo(string olsonTimeZoneId)
        {
            var olsonWindowsTimes = new Dictionary<string, string>()
            {
                { "Africa/Bangui", "W. Central Africa Standard Time" },
                { "Africa/Cairo", "Egypt Standard Time" },
                { "Africa/Casablanca", "Morocco Standard Time" },
                { "Africa/Harare", "South Africa Standard Time" },
                { "Africa/Johannesburg", "South Africa Standard Time" },
                { "Africa/Lagos", "W. Central Africa Standard Time" },
                { "Africa/Monrovia", "Greenwich Standard Time" },
                { "Africa/Nairobi", "E. Africa Standard Time" },
                { "Africa/Windhoek", "Namibia Standard Time" },
                { "America/Anchorage", "Alaskan Standard Time" },
                { "America/Argentina/San_Juan", "Argentina Standard Time" },
                { "America/Asuncion", "Paraguay Standard Time" },
                { "America/Bahia", "Bahia Standard Time" },
                { "America/Bogota", "SA Pacific Standard Time" },
                { "America/Buenos_Aires", "Argentina Standard Time" },
                { "America/Caracas", "Venezuela Standard Time" },
                { "America/Cayenne", "SA Eastern Standard Time" },
                { "America/Chicago", "Central Standard Time" },
                { "America/Chihuahua", "Mountain Standard Time (Mexico)" },
                { "America/Cuiaba", "Central Brazilian Standard Time" },
                { "America/Denver", "Mountain Standard Time" },
                { "America/Fortaleza", "SA Eastern Standard Time" },
                { "America/Godthab", "Greenland Standard Time" },
                { "America/Guatemala", "Central America Standard Time" },
                { "America/Halifax", "Atlantic Standard Time" },
                { "America/Indianapolis", "US Eastern Standard Time" },
                { "America/La_Paz", "SA Western Standard Time" },
                { "America/Los_Angeles", "Pacific Standard Time" },
                { "America/Mexico_City", "Mexico Standard Time" },
                { "America/Montevideo", "Montevideo Standard Time" },
                { "America/New_York", "Eastern Standard Time" },
                { "America/Noronha", "UTC-02" },
                { "America/Phoenix", "US Mountain Standard Time" },
                { "America/Regina", "Canada Central Standard Time" },
                { "America/Santa_Isabel", "Pacific Standard Time (Mexico)" },
                { "America/Santiago", "Pacific SA Standard Time" },
                { "America/Sao_Paulo", "E. South America Standard Time" },
                { "America/St_Johns", "Newfoundland Standard Time" },
                { "America/Tijuana", "Pacific Standard Time" },
                { "Antarctica/McMurdo", "New Zealand Standard Time" },
                { "Atlantic/South_Georgia", "UTC-02" },
                { "Asia/Almaty", "Central Asia Standard Time" },
                { "Asia/Amman", "Jordan Standard Time" },
                { "Asia/Baghdad", "Arabic Standard Time" },
                { "Asia/Baku", "Azerbaijan Standard Time" },
                { "Asia/Bangkok", "SE Asia Standard Time" },
                { "Asia/Beirut", "Middle East Standard Time" },
                { "Asia/Calcutta", "India Standard Time" },
                { "Asia/Colombo", "Sri Lanka Standard Time" },
                { "Asia/Damascus", "Syria Standard Time" },
                { "Asia/Dhaka", "Bangladesh Standard Time" },
                { "Asia/Dubai", "Arabian Standard Time" },
                { "Asia/Irkutsk", "North Asia East Standard Time" },
                { "Asia/Jerusalem", "Israel Standard Time" },
                { "Asia/Kabul", "Afghanistan Standard Time" },
                { "Asia/Kamchatka", "Kamchatka Standard Time" },
                { "Asia/Karachi", "Pakistan Standard Time" },
                { "Asia/Katmandu", "Nepal Standard Time" },
                { "Asia/Kolkata", "India Standard Time" },
                { "Asia/Krasnoyarsk", "North Asia Standard Time" },
                { "Asia/Kuala_Lumpur", "Singapore Standard Time" },
                { "Asia/Kuwait", "Arab Standard Time" },
                { "Asia/Magadan", "Magadan Standard Time" },
                { "Asia/Muscat", "Arabian Standard Time" },
                { "Asia/Novosibirsk", "N. Central Asia Standard Time" },
                { "Asia/Oral", "West Asia Standard Time" },
                { "Asia/Rangoon", "Myanmar Standard Time" },
                { "Asia/Riyadh", "Arab Standard Time" },
                { "Asia/Seoul", "Korea Standard Time" },
                { "Asia/Shanghai", "China Standard Time" },
                { "Asia/Singapore", "Singapore Standard Time" },
                { "Asia/Taipei", "Taipei Standard Time" },
                { "Asia/Tashkent", "West Asia Standard Time" },
                { "Asia/Tbilisi", "Georgian Standard Time" },
                { "Asia/Tehran", "Iran Standard Time" },
                { "Asia/Tokyo", "Tokyo Standard Time" },
                { "Asia/Ulaanbaatar", "Ulaanbaatar Standard Time" },
                { "Asia/Vladivostok", "Vladivostok Standard Time" },
                { "Asia/Yakutsk", "Yakutsk Standard Time" },
                { "Asia/Yekaterinburg", "Ekaterinburg Standard Time" },
                { "Asia/Yerevan", "Armenian Standard Time" },
                { "Atlantic/Azores", "Azores Standard Time" },
                { "Atlantic/Cape_Verde", "Cape Verde Standard Time" },
                { "Atlantic/Reykjavik", "Greenwich Standard Time" },
                { "Australia/Adelaide", "Cen. Australia Standard Time" },
                { "Australia/Brisbane", "E. Australia Standard Time" },
                { "Australia/Darwin", "AUS Central Standard Time" },
                { "Australia/Hobart", "Tasmania Standard Time" },
                { "Australia/Perth", "W. Australia Standard Time" },
                { "Australia/Sydney", "AUS Eastern Standard Time" },
                { "Etc/GMT", "UTC" },
                { "Etc/GMT+11", "UTC-11" },
                { "Etc/GMT+12", "Dateline Standard Time" },
                { "Etc/GMT+2", "UTC-02" },
                { "Etc/GMT-12", "UTC+12" },
                { "Europe/Amsterdam", "W. Europe Standard Time" },
                { "Europe/Athens", "GTB Standard Time" },
                { "Europe/Belgrade", "Central Europe Standard Time" },
                { "Europe/Berlin", "W. Europe Standard Time" },
                { "Europe/Brussels", "Romance Standard Time" },
                { "Europe/Budapest", "Central Europe Standard Time" },
                { "Europe/Dublin", "GMT Standard Time" },
                { "Europe/Helsinki", "FLE Standard Time" },
                { "Europe/Istanbul", "GTB Standard Time" },
                { "Europe/Kiev", "FLE Standard Time" },
                { "Europe/London", "GMT Standard Time" },
                { "Europe/Minsk", "E. Europe Standard Time" },
                { "Europe/Moscow", "Russian Standard Time" },
                { "Europe/Paris", "Romance Standard Time" },
                { "Europe/Sarajevo", "Central European Standard Time" },
                { "Europe/Warsaw", "Central European Standard Time" },
                { "Indian/Mauritius", "Mauritius Standard Time" },
                { "Pacific/Apia", "Samoa Standard Time" },
                { "Pacific/Auckland", "New Zealand Standard Time" },
                { "Pacific/Fiji", "Fiji Standard Time" },
                { "Pacific/Guadalcanal", "Central Pacific Standard Time" },
                { "Pacific/Guam", "West Pacific Standard Time" },
                { "Pacific/Honolulu", "Hawaiian Standard Time" },
                { "Pacific/Pago_Pago", "UTC-11" },
                { "Pacific/Port_Moresby", "West Pacific Standard Time" },
                { "Pacific/Tongatapu", "Tonga Standard Time" }
            };

            var windowsTimeZoneId = default(string);
            if (olsonWindowsTimes.TryGetValue(olsonTimeZoneId, out windowsTimeZoneId))
            {
                return windowsTimeZoneId;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region Push Notifications

        public static HttpNotificationChannel CurrentChannel { get; private set; }
        
        public static async void AcquirePushChannel(double latitude, double longitude)
        {
            try
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
                    registration.ReminderInterval = new AppSettings().FriendlyReminderInterval;
                    registration.NearbyInterval = new AppSettings().FriendlyNearbyRadius;

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
                        ReminderInterval = new AppSettings().FriendlyReminderInterval,
                        NearbyInterval = new AppSettings().FriendlyNearbyRadius
                    };

                    await registrationsTable.InsertAsync(registration);
                }
            }
            catch (NullReferenceException ex)
            {
                // ignore these errors
            }
            catch (HttpRequestException ex)
            {
                // ignore these errors
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                // ignore these errors
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
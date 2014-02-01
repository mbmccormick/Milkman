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
            try
            {
                string rtmTimezone = OlsonTimeZoneToTimeZoneInfo(App.RtmClient.UserSettings.TimeZone);

                if (rtmTimezone != TimeZoneInfo.Local.StandardName)
                {
                    MessageBox.Show(Strings.TimezoneMismatchDialog, Strings.TimezoneMismatchDialogTitle, MessageBoxButton.OK);
                }
            }
            catch (Exception ex)
            {
                // do nothing
            }
        }

        public static string OlsonTimeZoneToTimeZoneInfo(string olsonTimeZoneId)
        {
            var olsonWindowsTimes = new Dictionary<string, string>()
            {
                { "Pacific/Midway", "UTC-11:00" },
                { "Pacific/Niue", "UTC-11:00" },
                { "Pacific/Pago_Pago", "UTC-11:00" },
                { "America/Adak", "Hawaiian Standard Time" },
                { "Pacific/Fakaofo", "Hawaiian Standard Time" },
                { "Pacific/Honolulu", "Hawaiian Standard Time" },
                { "Pacific/Johnston", "Hawaiian Standard Time" },
                { "Pacific/Rarotonga", "Hawaiian Standard Time" },
                { "Pacific/Tahiti", "Hawaiian Standard Time" },
                { "Pacific/Marquesas", "UTC-9:30" },
                { "America/Anchorage", "Alaskan Standard Time" },
                { "America/Juneau", "Alaskan Standard Time" },
                { "America/Nome", "Alaskan Standard Time" },
                { "America/Yakutat", "Alaskan Standard Time" },
                { "Pacific/Gambier", "Alaskan Standard Time" },
                { "America/Dawson", "Pacific Standard Time" },
                { "America/Los_Angeles", "Pacific Standard Time" },
                { "America/Tijuana", "Pacific Standard Time" },
                { "America/Vancouver", "Pacific Standard Time" },
                { "America/Whitehorse", "Pacific Standard Time" },
                { "Pacific/Pitcairn", "Pacific Standard Time" },
                { "America/Boise", "Mountain Standard Time" },
                { "America/Cambridge_Bay", "Mountain Standard Time" },
                { "America/Chihuahua", "Mountain Standard Time" },
                { "America/Dawson_Creek", "Mountain Standard Time" },
                { "America/Denver", "Mountain Standard Time" },
                { "America/Edmonton", "Mountain Standard Time" },
                { "America/Hermosillo", "Mountain Standard Time" },
                { "America/Inuvik", "Mountain Standard Time" },
                { "America/Mazatlan", "Mountain Standard Time" },
                { "America/Phoenix", "Mountain Standard Time" },
                { "America/Shiprock", "Mountain Standard Time" },
                { "America/Yellowknife", "Mountain Standard Time" },
                { "America/Belize", "Central Standard Time" },
                { "America/Cancun", "Central Standard Time" },
                { "America/Chicago", "Central Standard Time" },
                { "America/Costa_Rica", "Central Standard Time" },
                { "America/El_Salvador", "Central Standard Time" },
                { "America/Guatemala", "Central Standard Time" },
                { "America/Indiana/Knox", "Central Standard Time" },
                { "America/Managua", "Central Standard Time" },
                { "America/Menominee", "Central Standard Time" },
                { "America/Merida", "Central Standard Time" },
                { "America/Mexico_City", "Central Standard Time" },
                { "America/Monterrey", "Central Standard Time" },
                { "America/North_Dakota/Center", "Central Standard Time" },
                { "America/North_Dakota/New_Salem", "Central Standard Time" },
                { "America/Rainy_River", "Central Standard Time" },
                { "America/Rankin_Inlet", "Central Standard Time" },
                { "America/Regina", "Central Standard Time" },
                { "America/Swift_Current", "Central Standard Time" },
                { "America/Tegucigalpa", "Central Standard Time" },
                { "America/Winnipeg", "Central Standard Time" },
                { "Pacific/Easter", "Central Standard Time" },
                { "Pacific/Galapagos", "Central Standard Time" },
                { "America/Atikokan", "US Eastern Standard Time" },
                { "America/Bogota", "US Eastern Standard Time" },
                { "America/Cayman", "US Eastern Standard Time" },
                { "America/Detroit", "US Eastern Standard Time" },
                { "America/Grand_Turk", "US Eastern Standard Time" },
                { "America/Guayaquil", "US Eastern Standard Time" },
                { "America/Havana", "US Eastern Standard Time" },
                { "America/Indiana/Indianapolis", "US Eastern Standard Time" },
                { "America/Indiana/Marengo", "US Eastern Standard Time" },
                { "America/Indiana/Petersburg", "US Eastern Standard Time" },
                { "America/Indiana/Vevay", "US Eastern Standard Time" },
                { "America/Indiana/Vincennes", "US Eastern Standard Time" },
                { "America/Iqaluit", "US Eastern Standard Time" },
                { "America/Jamaica", "US Eastern Standard Time" },
                { "America/Kentucky/Louisville", "US Eastern Standard Time" },
                { "America/Kentucky/Monticello", "US Eastern Standard Time" },
                { "America/Lima", "US Eastern Standard Time" },
                { "America/Montreal", "US Eastern Standard Time" },
                { "America/Nassau", "US Eastern Standard Time" },
                { "America/New_York", "US Eastern Standard Time" },
                { "America/Nipigon", "US Eastern Standard Time" },
                { "America/Panama", "US Eastern Standard Time" },
                { "America/Pangnirtung", "US Eastern Standard Time" },
                { "America/Port-au-Prince", "US Eastern Standard Time" },
                { "America/Thunder_Bay", "US Eastern Standard Time" },
                { "America/Toronto", "US Eastern Standard Time" },
                { "America/Caracas", "Venezuela Standard Time" },
                { "America/Anguilla", "Venezuela Standard Time" },
                { "America/Antigua", "Atlantic Standard Time" },
                { "America/Aruba", "Atlantic Standard Time" },
                { "America/Asuncion", "Atlantic Standard Time" },
                { "America/Barbados", "Atlantic Standard Time" },
                { "America/Blanc-Sablon", "Atlantic Standard Time" },
                { "America/Boa_Vista", "Atlantic Standard Time" },
                { "America/Campo_Grande", "Atlantic Standard Time" },
                { "America/Cuiaba", "Atlantic Standard Time" },
                { "America/Curacao", "Atlantic Standard Time" },
                { "America/Dominica", "Atlantic Standard Time" },
                { "America/Eirunepe", "Atlantic Standard Time" },
                { "America/Glace_Bay", "Atlantic Standard Time" },
                { "America/Goose_Bay", "Atlantic Standard Time" },
                { "America/Grenada", "Atlantic Standard Time" },
                { "America/Guadeloupe", "Atlantic Standard Time" },
                { "America/Guyana", "Atlantic Standard Time" },
                { "America/Halifax", "Atlantic Standard Time" },
                { "America/La_Paz", "Atlantic Standard Time" },
                { "America/Manaus", "Atlantic Standard Time" },
                { "America/Martinique", "Atlantic Standard Time" },
                { "America/Moncton", "Atlantic Standard Time" },
                { "America/Montserrat", "Atlantic Standard Time" },
                { "America/Port_of_Spain", "Atlantic Standard Time" },
                { "America/Porto_Velho", "Atlantic Standard Time" },
                { "America/Puerto_Rico", "Atlantic Standard Time" },
                { "America/Rio_Branco", "Atlantic Standard Time" },
                { "America/Santiago", "Atlantic Standard Time" },
                { "America/Santo_Domingo", "Atlantic Standard Time" },
                { "America/St_Kitts", "Atlantic Standard Time" },
                { "America/St_Lucia", "Atlantic Standard Time" },
                { "America/St_Thomas", "Atlantic Standard Time" },
                { "America/St_Vincent", "Atlantic Standard Time" },
                { "America/Thule", "Atlantic Standard Time" },
                { "America/Tortola", "Atlantic Standard Time" },
                { "Antarctica/Palmer", "Atlantic Standard Time" },
                { "Atlantic/Bermuda", "Atlantic Standard Time" },
                { "Atlantic/Stanley", "Atlantic Standard Time" },
                { "America/St_Johns", "Newfoundland Standard Time" },
                { "America/Araguaina", "Argentina Standard Time" },
                { "America/Argentina/Buenos_Aires", "Argentina Standard Time" },
                { "America/Argentina/Catamarca", "Argentina Standard Time" },
                { "America/Argentina/Cordoba", "Argentina Standard Time" },
                { "America/Argentina/Jujuy", "Argentina Standard Time" },
                { "America/Argentina/La_Rioja", "Argentina Standard Time" },
                { "America/Argentina/Mendoza", "Argentina Standard Time" },
                { "America/Argentina/Rio_Gallegos", "Argentina Standard Time" },
                { "America/Argentina/San_Juan", "Argentina Standard Time" },
                { "America/Argentina/Tucuman", "Argentina Standard Time" },
                { "America/Argentina/Ushuaia", "Argentina Standard Time" },
                { "America/Bahia", "Bahia Standard Time" },
                { "America/Belem", "Bahia Standard Time" },
                { "America/Cayenne", "SA Eastern Standard Time" },
                { "America/Fortaleza", "SA Eastern Standard Time" },
                { "America/Godthab", "Greenland Standard Time" },
                { "America/Maceio", "Greenland Standard Time" },
                { "America/Miquelon", "Greenland Standard Time" },
                { "America/Montevideo", "Montevideo Standard Time" },
                { "America/Paramaribo", "Montevideo Standard Time" },
                { "America/Recife", "Montevideo Standard Time" },
                { "America/Sao_Paulo", "E. South America Standard Time" },
                { "Antarctica/Rothera", "E. South America Standard Time" },
                { "America/Noronha", "UTC-02:00" },
                { "Atlantic/South_Georgia", "UTC-02:00" },
                { "America/Scoresbysund", "UTC-01:00" },
                { "Atlantic/Azores", "UTC-01:00" },
                { "Atlantic/Cape_Verde", "UTC-01:00" },
                { "Africa/Abidjan", "Greenwich Standard Time" },
                { "Africa/Accra", "Greenwich Standard Time" },
                { "Africa/Bamako", "Greenwich Standard Time" },
                { "Africa/Banjul", "Greenwich Standard Time" },
                { "Africa/Bissau", "Greenwich Standard Time" },
                { "Africa/Casablanca", "Greenwich Standard Time" },
                { "Africa/Conakry", "Greenwich Standard Time" },
                { "Africa/Dakar", "Greenwich Standard Time" },
                { "Africa/El_Aaiun", "Greenwich Standard Time" },
                { "Africa/Freetown", "Greenwich Standard Time" },
                { "Africa/Lome", "Greenwich Standard Time" },
                { "Africa/Monrovia", "Greenwich Standard Time" },
                { "Africa/Nouakchott", "Greenwich Standard Time" },
                { "Africa/Ouagadougou", "Greenwich Standard Time" },
                { "Africa/Sao_Tome", "Greenwich Standard Time" },
                { "America/Danmarkshavn", "Greenwich Standard Time" },
                { "Atlantic/Canary", "Greenwich Standard Time" },
                { "Atlantic/Faroe", "Greenwich Standard Time" },
                { "Atlantic/Madeira", "Greenwich Standard Time" },
                { "Atlantic/Reykjavik", "Greenwich Standard Time" },
                { "Atlantic/St_Helena", "Greenwich Standard Time" },
                { "Europe/Dublin", "Greenwich Standard Time" },
                { "Europe/Guernsey", "Greenwich Standard Time" },
                { "Europe/Isle_of_Man", "Greenwich Standard Time" },
                { "Europe/Jersey", "Greenwich Standard Time" },
                { "Europe/Lisbon", "Greenwich Standard Time" },
                { "Europe/London", "Greenwich Standard Time" },
                { "Africa/Algiers", "Namibia Standard Time" },
                { "Africa/Bangui", "Namibia Standard Time" },
                { "Africa/Brazzaville", "Namibia Standard Time" },
                { "Africa/Ceuta", "Namibia Standard Time" },
                { "Africa/Douala", "Namibia Standard Time" },
                { "Africa/Kinshasa", "Namibia Standard Time" },
                { "Africa/Lagos", "Namibia Standard Time" },
                { "Africa/Libreville", "Namibia Standard Time" },
                { "Africa/Luanda", "Namibia Standard Time" },
                { "Africa/Malabo", "Namibia Standard Time" },
                { "Africa/Ndjamena", "Namibia Standard Time" },
                { "Africa/Niamey", "Namibia Standard Time" },
                { "Africa/Porto-Novo", "Namibia Standard Time" },
                { "Africa/Tunis", "Namibia Standard Time" },
                { "Africa/Windhoek", "Namibia Standard Time" },
                { "Arctic/Longyearbyen", "W. Europe Standard Time" },
                { "Atlantic/Jan_Mayen", "W. Europe Standard Time" },
                { "Europe/Amsterdam", "W. Europe Standard Time" },
                { "Europe/Andorra", "W. Europe Standard Time" },
                { "Europe/Belgrade", "W. Europe Standard Time" },
                { "Europe/Berlin", "W. Europe Standard Time" },
                { "Europe/Bratislava", "W. Europe Standard Time" },
                { "Europe/Brussels", "Romance Standard Time" },
                { "Europe/Budapest", "Central Europe Standard Time" },
                { "Europe/Copenhagen", "Central Europe Standard Time" },
                { "Europe/Gibraltar", "Central Europe Standard Time" },
                { "Europe/Ljubljana", "Central Europe Standard Time" },
                { "Europe/Luxembourg", "Central Europe Standard Time" },
                { "Europe/Madrid", "Central Europe Standard Time" },
                { "Europe/Malta", "Central Europe Standard Time" },
                { "Europe/Monaco", "Central Europe Standard Time" },
                { "Europe/Oslo", "Central Europe Standard Time" },
                { "Europe/Paris", "Romance Standard Time" },
                { "Europe/Podgorica", "Romance Standard Time" },
                { "Europe/Prague", "Romance Standard Time" },
                { "Europe/Rome", "Romance Standard Time" },
                { "Europe/San_Marino", "Romance Standard Time" },
                { "Europe/Sarajevo", "Romance Standard Time" },
                { "Europe/Skopje", "Romance Standard Time" },
                { "Europe/Stockholm", "Romance Standard Time" },
                { "Europe/Tirane", "Romance Standard Time" },
                { "Europe/Vaduz", "Romance Standard Time" },
                { "Europe/Vatican", "Romance Standard Time" },
                { "Europe/Vienna", "Romance Standard Time" },
                { "Europe/Warsaw", "Central Europe Standard Time" },
                { "Europe/Zagreb", "Central Europe Standard Time" },
                { "Europe/Zurich", "Central Europe Standard Time" },
                { "Africa/Blantyre", "Egypt Standard Time" },
                { "Africa/Bujumbura", "Egypt Standard Time" },
                { "Africa/Cairo", "Egypt Standard Time" },
                { "Africa/Gaborone", "Egypt Standard Time" },
                { "Africa/Harare", "South Africa Standard Time" },
                { "Africa/Johannesburg", "South Africa Standard Time" },
                { "Africa/Kigali", "South Africa Standard Time" },
                { "Africa/Lubumbashi", "South Africa Standard Time" },
                { "Africa/Lusaka", "South Africa Standard Time" },
                { "Africa/Maputo", "South Africa Standard Time" },
                { "Africa/Maseru", "South Africa Standard Time" },
                { "Africa/Mbabane", "South Africa Standard Time" },
                { "Africa/Tripoli", "South Africa Standard Time" },
                { "Asia/Amman", "Jordan Standard Time" },
                { "Asia/Beirut", "Middle East Standard Time" },
                { "Asia/Damascus", "Syria Standard Time" },
                { "Asia/Gaza", "Syria Standard Time" },
                { "Asia/Istanbul", "Syria Standard Time" },
                { "Asia/Jerusalem", "Israel Standard Time" },
                { "Asia/Nicosia", "Israel Standard Time" },
                { "Europe/Athens", "GTB Standard Time" },
                { "Europe/Bucharest", "GTB Standard Time" },
                { "Europe/Chisinau", "GTB Standard Time" },
                { "Europe/Helsinki", "FLE Standard Time" },
                { "Europe/Istanbul", "GTB Standard Time" },
                { "Europe/Kiev", "FLE Standard Time" },
                { "Europe/Mariehamn", "FLE Standard Time" },
                { "Europe/Nicosia", "FLE Standard Time" },
                { "Europe/Riga", "FLE Standard Time" },
                { "Europe/Simferopol", "FLE Standard Time" },
                { "Europe/Sofia", "FLE Standard Time" },
                { "Europe/Tallinn", "FLE Standard Time" },
                { "Europe/Uzhgorod", "FLE Standard Time" },
                { "Europe/Vilnius", "FLE Standard Time" },
                { "Europe/Zaporozhye", "FLE Standard Time" },
                { "Africa/Addis_Ababa", "E. Africa Standard Time" },
                { "Africa/Asmara", "E. Africa Standard Time" },
                { "Africa/Dar_es_Salaam", "E. Africa Standard Time" },
                { "Africa/Djibouti", "E. Africa Standard Time" },
                { "Africa/Kampala", "E. Africa Standard Time" },
                { "Africa/Khartoum", "E. Africa Standard Time" },
                { "Africa/Mogadishu", "E. Africa Standard Time" },
                { "Africa/Nairobi", "E. Africa Standard Time" },
                { "Antarctica/Syowa", "E. Africa Standard Time" },
                { "Asia/Aden", "E. Africa Standard Time" },
                { "Asia/Baghdad", "Arabic Standard Time" },
                { "Asia/Bahrain", "Arabic Standard Time" },
                { "Asia/Kuwait", "Arab Standard Time" },
                { "Asia/Qatar", "Arab Standard Time" },
                { "Asia/Riyadh", "Arab Standard Time" },
                { "Europe/Kaliningrad", "Arab Standard Time" },
                { "Europe/Minsk", "E. Europe Standard Time" },
                { "Indian/Antananarivo", "E. Europe Standard Time" },
                { "Indian/Comoro", "E. Europe Standard Time" },
                { "Indian/Mayotte", "E. Europe Standard Time" },
                { "Asia/Tehran", "Iran Standard Time" },
                { "Asia/Baku", "Azerbaijan Standard Time" },
                { "Asia/Dubai", "Arabian Standard Time" },
                { "Asia/Muscat", "Arabian Standard Time" },
                { "Asia/Tbilisi", "Georgian Standard Time" },
                { "Asia/Yerevan", "Armenian Standard Time" },
                { "Europe/Moscow", "Russian Standard Time" },
                { "Europe/Samara", "Russian Standard Time" },
                { "Europe/Volgograd", "Russian Standard Time" },
                { "Indian/Mahe", "Mauritius Standard Time" },
                { "Indian/Mauritius", "Mauritius Standard Time" },
                { "Indian/Reunion", "Mauritius Standard Time" },
                { "Asia/Kabul", "Afghanistan Standard Time" },
                { "Antarctica/Mawson", "Pakistan Standard Time" },
                { "Asia/Aqtau", "Pakistan Standard Time" },
                { "Asia/Aqtobe", "Pakistan Standard Time" },
                { "Asia/Ashgabat", "Pakistan Standard Time" },
                { "Asia/Dushanbe", "Pakistan Standard Time" },
                { "Asia/Karachi", "Pakistan Standard Time" },
                { "Asia/Oral", "West Asia Standard Time" },
                { "Asia/Samarkand", "West Asia Standard Time" },
                { "Asia/Tashkent", "West Asia Standard Time" },
                { "Indian/Kerguelen", "West Asia Standard Time" },
                { "Indian/Maldives", "West Asia Standard Time" },
                { "Asia/Colombo", "Sri Lanka Standard Time" },
                { "Asia/Kolkata", "India Standard Time" },
                { "Asia/Katmandu", "Nepal Standard Time" },
                { "Antarctica/Vostok", "Central Asia Standard Time" },
                { "Asia/Almaty", "Central Asia Standard Time" },
                { "Asia/Bishkek", "Central Asia Standard Time" },
                { "Asia/Dhaka", "Bangladesh Standard Time" },
                { "Asia/Qyzylorda", "Bangladesh Standard Time" },
                { "Asia/Thimphu", "Bangladesh Standard Time" },
                { "Asia/Yekaterinburg", "Ekaterinburg Standard Time" },
                { "Indian/Chagos", "Ekaterinburg Standard Time" },
                { "Asia/Rangoon", "Myanmar Standard Time" },
                { "Indian/Cocos", "Myanmar Standard Time" },
                { "Antarctica/Davis", "SE Asia Standard Time" },
                { "Asia/Bangkok", "SE Asia Standard Time" },
                { "Asia/Ho_Chi_Minh", "SE Asia Standard Time" },
                { "Asia/Hovd", "SE Asia Standard Time" },
                { "Asia/Jakarta", "SE Asia Standard Time" },
                { "Asia/Novosibirsk", "N. Central Asia Standard Time" },
                { "Asia/Omsk", "N. Central Asia Standard Time" },
                { "Asia/Phnom_Penh", "N. Central Asia Standard Time" },
                { "Asia/Pontianak", "N. Central Asia Standard Time" },
                { "Asia/Vientiane", "N. Central Asia Standard Time" },
                { "Indian/Christmas", "N. Central Asia Standard Time" },
                { "Antarctica/Casey", "Singapore Standard Time" },
                { "Asia/Brunei", "Singapore Standard Time" },
                { "Asia/Choibalsan", "Singapore Standard Time" },
                { "Asia/Chongqing", "Singapore Standard Time" },
                { "Asia/Harbin", "Singapore Standard Time" },
                { "Asia/Hong_Kong", "Singapore Standard Time" },
                { "Asia/Kashgar", "Singapore Standard Time" },
                { "Asia/Krasnoyarsk", "Singapore Standard Time" },
                { "Asia/Kuala_Lumpur", "Singapore Standard Time" },
                { "Asia/Kuching", "Singapore Standard Time" },
                { "Asia/Macau", "Singapore Standard Time" },
                { "Asia/Makassar", "Singapore Standard Time" },
                { "Asia/Manila", "Singapore Standard Time" },
                { "Asia/Shanghai", "China Standard Time" },
                { "Asia/Singapore", "Singapore Standard Time" },
                { "Asia/Taipei", "Taipei Standard Time" },
                { "Asia/Ulaanbaatar", "Ulaanbaatar Standard Time" },
                { "Asia/Urumqi", "Ulaanbaatar Standard Time" },
                { "Australia/Perth", "W. Australia Standard Time" },
                { "Australia/Eucla", "UTC+8:45" },
                { "Asia/Dili", "UTC+9:00" },
                { "Asia/Irkutsk", "North Asia East Standard Time" },
                { "Asia/Jayapura", "North Asia East Standard Time" },
                { "Asia/Pyongyang", "North Asia East Standard Time" },
                { "Asia/Seoul", "Korea Standard Time" },
                { "Asia/Tokyo", "Tokyo Standard Time" },
                { "Pacific/Palau", "UTC+9:00" },
                { "Australia/Adelaide", "Cen. Australia Standard Time" },
                { "Australia/Broken_Hill", "UTC+9:30" },
                { "Australia/Darwin", "AUS Central Standard Time" },
                { "Antarctica/DumontDUrville", "UTC+10:00" },
                { "Asia/Yakutsk", "Yakutsk Standard Time" },
                { "Australia/Brisbane", "E. Australia Standard Time" },
                { "Australia/Currie", "AUS Eastern Standard Time" },
                { "Australia/Hobart", "Tasmania Standard Time" },
                { "Australia/Lindeman", "AUS Eastern Standard Time" },
                { "Australia/Lord_Howe", "UTC+10:00" },
                { "Australia/Melbourne", "AUS Eastern Standard Time" },
                { "Australia/Sydney", "AUS Eastern Standard Time" },
                { "Pacific/Guam", "West Pacific Standard Time" },
                { "Pacific/Port_Moresby", "West Pacific Standard Time" },
                { "Pacific/Saipan", "West Pacific Standard Time" },
                { "Pacific/Truk", "West Pacific Standard Time" },
                { "Asia/Sakhalin", "Vladivostok Standard Time" },
                { "Asia/Vladivostok", "Vladivostok Standard Time" },
                { "Pacific/Efate", "UTC+11:00" },
                { "Pacific/Guadalcanal", "Central Pacific Standard Time" },
                { "Pacific/Kosrae", "UTC+11:00" },
                { "Pacific/Noumea", "UTC+11:00" },
                { "Pacific/Ponape", "UTC+11:00" },
                { "Pacific/Norfolk", "UTC+11:30" },
                { "Antarctica/McMurdo", "New Zealand Standard Time" },
                { "Antarctica/South_Pole", "UTC+12:00" },
                { "Asia/Anadyr", "UTC+12:00" },
                { "Asia/Kamchatka", "Kamchatka Standard Time" },
                { "Asia/Magadan", "Magadan Standard Time" },
                { "Pacific/Auckland", "New Zealand Standard Time" },
                { "Pacific/Fiji", "Fiji Standard Time" },
                { "Pacific/Funafuti", "UTC+12:00" },
                { "Pacific/Kwajalein", "UTC+12:00" },
                { "Pacific/Majuro", "UTC+12:00" },
                { "Pacific/Nauru", "UTC+12:00" },
                { "Pacific/Tarawa", "UTC+12:00" },
                { "Pacific/Wake", "UTC+12:00" },
                { "Pacific/Wallis", "UTC+12:00" },
                { "Pacific/Chatham", "UTC+12:45" },
                { "Pacific/Apia", "Samoa Standard Time" },
                { "Pacific/Enderbury", "UTC+13:00" },
                { "Pacific/Tongatapu", "Tonga Standard Time" },
                { "Pacific/Kiritimati", "UTC+14:00" }
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
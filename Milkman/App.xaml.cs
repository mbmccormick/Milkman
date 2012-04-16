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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Milkman.Common;
using IronCow;
using IronCow.Rest;
using Microsoft.Phone.Scheduler;

namespace Milkman
{
    public partial class App : Application
    {
        private static readonly string RtmApiKey = "09b03090fc9303804aedd945872fdefc";
        private static readonly string RtmSharedKey = "d2ffaf49356b07f9";

        public static Rtm RtmClient;
        public static Response ListsResponse;
        public static Response TasksResponse;

        public static event EventHandler<ApplicationUnhandledExceptionEventArgs> UnhandledExceptionHandled;

        public static string VersionNumber
        {
            get
            {
                string assembly = System.Reflection.Assembly.GetExecutingAssembly().FullName;
                return assembly.Split('=')[1].Split(',')[0];
            }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            AppSettings settings = new AppSettings();

            if (settings.LightThemeEnabled == true)
                ThemeManager.ToLightTheme();
        }

        public static void LoadData()
        {
            string RtmAuthToken = IsolatedStorageHelper.GetObject<string>("RtmAuthToken");
            int? Timeline = IsolatedStorageHelper.GetObject<int?>("RtmTimeline");
            ListsResponse = IsolatedStorageHelper.GetObject<Response>("ListsResponse");
            TasksResponse = IsolatedStorageHelper.GetObject<Response>("TasksResponse");

            if (!string.IsNullOrEmpty(RtmAuthToken))
            {
                RtmClient = new Rtm(RtmApiKey, RtmSharedKey, RtmAuthToken);
            }
            else
            {
                RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            }

            if (Timeline.HasValue)
            {
                RtmClient.CurrentTimeline = Timeline.Value;
            }

            RtmClient.CacheListsEvent += OnCacheLists;
            RtmClient.CacheTasksEvent += OnCacheTasks;

            if (ListsResponse != null)
            {
                RtmClient.LoadListsFromResponse(ListsResponse);
            }
            if (TasksResponse != null)
            {
                RtmClient.LoadTasksFromResponse(TasksResponse);
            }
        }

        public static void SaveData()
        {
            IsolatedStorageHelper.SaveObject<string>("RtmAuthToken", RtmClient.AuthToken);
            IsolatedStorageHelper.SaveObject<int?>("RtmTimeline", RtmClient.CurrentTimeline);
            IsolatedStorageHelper.SaveObject<Response>("ListsResponse", ListsResponse);
            IsolatedStorageHelper.SaveObject<Response>("TasksResponse", TasksResponse);
        }

        public static void DeleteData()
        {
            IsolatedStorageHelper.DeleteObject("RtmAuthToken");
            IsolatedStorageHelper.DeleteObject("ListsResponse");
            IsolatedStorageHelper.DeleteObject("TasksResponse");
            IsolatedStorageHelper.DeleteObject("RtmTimeline");
            RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            ListsResponse = null;
            TasksResponse = null;

            // stop background worker
            if (ScheduledActionService.Find("BackgroundWorker") != null)
                ScheduledActionService.Remove("BackgroundWorker");

            // delete all existing reminders
            foreach (var item in ScheduledActionService.GetActions<Reminder>())
                ScheduledActionService.Remove(item.Name);

            // reset live tile data
            ShellTile primaryTile = ShellTile.ActiveTiles.First();
            if (primaryTile != null)
            {
                StandardTileData data = new StandardTileData();
                primaryTile.Update(data);
            }
        }

        public static void OnCacheLists(Response response)
        {
            ListsResponse = response;
        }

        public static void OnCacheTasks(Response response)
        {
            TasksResponse = response;
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            // initialize smart dispatcher
            SmartDispatcher.Initialize(RootFrame.Dispatcher);

            LoadData();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            LoadData();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            SaveData();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            // Ensure that required application state is persisted here.
            SaveData();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            LittleWatson.ReportException(e.Exception, "RootFrame_NavigationFailed()");

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is RtmException)
            {
                var ex = (e.ExceptionObject as RtmException).ResponseError;
                RootFrame.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(ex.Message, "Error " + ex.Code, MessageBoxButton.OK);
                });
            }
            else if (e.ExceptionObject is WebException)
            {
                WebException ex = e.ExceptionObject as WebException;

                RootFrame.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(ex.Message + "\n\nAre you connected to the network?", "Error Contacting Server", MessageBoxButton.OK);
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
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            Rtm.Dispatcher = App.Current.RootVisual.Dispatcher;
            SmartDispatcher.Initialize();

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}
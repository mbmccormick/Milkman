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
using IronCow.Resources;
using IronCow.Rest;
using Microsoft.Phone.Scheduler;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Tasks;
using System.Net.NetworkInformation;
using Windows.Phone.Speech.VoiceCommands;

namespace Milkman
{
    public partial class App : Application
    {
        private static readonly string RtmApiKey = "09b03090fc9303804aedd945872fdefc";
        private static readonly string RtmSharedKey = "d2ffaf49356b07f9";

        public static Rtm RtmClient;
        public static Response ListsResponse;
        public static Response TasksResponse;
        public static DateTime LastUpdated;

        public static string LastUpdatedText
        {
            get
            {
                if (RtmClient.UserSettings != null &&
                    RtmClient.UserSettings.DateFormat == DateFormat.European)
                {
                    if (RtmClient.UserSettings != null &&
                        RtmClient.UserSettings.TimeFormat == TimeFormat.TwentyFourHours)
                        return LastUpdated.ToString("d/M/yyyy H:mm").ToLower();
                    else
                        return LastUpdated.ToString("d/M/yyyy h:mmtt").ToLower(); 
                }
                else
                {
                    if (RtmClient.UserSettings != null &&
                        RtmClient.UserSettings.TimeFormat == TimeFormat.TwentyFourHours)
                        return LastUpdated.ToString("M/d/yyyy H:mm").ToLower();
                    else
                        return LastUpdated.ToString("M/d/yyyy h:mmtt").ToLower();
                }
            }
        }

        public static event EventHandler<ApplicationUnhandledExceptionEventArgs> UnhandledExceptionHandled;
        public static event EventHandler<EventArgs> SyncingDisabled;

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
            ListsResponse = IsolatedStorageHelper.GetObject<Response>("ListsResponse");
            TasksResponse = IsolatedStorageHelper.GetObject<Response>("TasksResponse");
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

            RtmClient.Resources = App.Current.Resources;

            await VoiceCommandService.InstallCommandSetsFromFileAsync(new Uri("ms-appx:///Commands.xml"));
        }

        public static void SaveData()
        {
            IsolatedStorageHelper.SaveObject<string>("RtmAuthToken", RtmClient.AuthToken);
            IsolatedStorageHelper.SaveObject<int?>("RtmTimeline", RtmClient.CurrentTimeline);
            IsolatedStorageHelper.SaveObject<Response>("ListsResponse", ListsResponse);
            IsolatedStorageHelper.SaveObject<Response>("TasksResponse", TasksResponse);
            IsolatedStorageHelper.SaveObject<DateTime>("LastUpdated", LastUpdated);

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static void DeleteData()
        {
            IsolatedStorageHelper.DeleteObject("RtmAuthToken");
            IsolatedStorageHelper.DeleteObject("RtmTimeline");
            IsolatedStorageHelper.DeleteObject("ListsResponse");
            IsolatedStorageHelper.DeleteObject("TasksResponse");
            IsolatedStorageHelper.DeleteObject("LastUpdated");

            RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            ListsResponse = null;
            TasksResponse = null;

            RtmClient.Resources = App.Current.Resources;

            NotificationsManager.ClearNotifications();

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

        public static void OnCacheLists(Response response)
        {
            ListsResponse = response;
            LastUpdated = DateTime.Now;
        }

        public static void OnCacheTasks(Response response)
        {
            TasksResponse = response;
            LastUpdated = DateTime.Now;
        }

        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            SmartDispatcher.Initialize(RootFrame.Dispatcher);

            LoadData();

            this.PromptForMarketplaceReview();
        }

        private void PromptForMarketplaceReview()
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
                            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                            marketplaceReviewTask.Show();

                            installDate = DateTime.MaxValue; // they have rated, don't prompt again

                            break;
                        default:
                            installDate = DateTime.UtcNow; // they did not rate, prompt again in 2 days
                            break;
                    }
                };

                messageBox.Show();
            }

            IsolatedStorageSettings.ApplicationSettings["CurrentVersion"] = App.VersionNumber; // save current version of application
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
            else if (e.ExceptionObject is WebException)
            {
                WebException ex = e.ExceptionObject as WebException;

                if (RtmClient.Syncing == true)
                {
                    RootFrame.Dispatcher.BeginInvoke(() =>
                    {
                        CustomMessageBox messageBox = new CustomMessageBox()
                        {
                            Caption = Strings.OfflineConnectionDialogTitle,
                            Message = Strings.OfflineConnectionDialog,
                            LeftButtonContent = Strings.YesLower,
                            RightButtonContent = Strings.NoLower,
                            IsFullScreen = false
                        };

                        messageBox.Dismissed += (s1, e1) =>
                        {
                            switch (e1.Result)
                            {
                                case CustomMessageBoxResult.LeftButton:
                                    RtmClient.DisableSyncing();
                                    GlobalLoading.Instance.StatusText(Strings.WorkingOffline);

                                    if (SyncingDisabled != null)
                                        SyncingDisabled(sender, null);

                                    break;
                                default:
                                    break;
                            }
                        };

                        messageBox.Show();
                    });
                }
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
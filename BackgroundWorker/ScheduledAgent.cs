using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Collections.ObjectModel;
using BackgroundWorker.Common;
using IronCow;
using System.ComponentModel;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System.Device.Location;

namespace BackgroundWorker
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        private static volatile bool _classInitialized;

        public ScheduledAgent()
        {
            if (!_classInitialized)
            {
                _classInitialized = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate
                {
                    Application.Current.UnhandledException += ScheduledAgent_UnhandledException;
                });
            }
        }

        private void ScheduledAgent_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        GeoCoordinateWatcher _watcher = null;

        protected override void OnInvoke(ScheduledTask task)
        {
            // load cached data
            App.LoadData();

            AppSettings settings = new AppSettings();

            // update current location
            if (settings.LocationNotificationsEnabled == true)
            {
                _watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                _watcher.Start();
            }

            // sync data
            if (settings.BackgroundWorkerEnabled == true)
            {
                SyncData();
            }
            else
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    ScheduledActionService.LaunchForTest("BackgroundWorker", new TimeSpan(0, 0, 1, 0)); // every minute

                NotifyComplete();
            }
        }

        private void SyncData()
        {
            if (!string.IsNullOrEmpty(App.RtmClient.AuthToken))
            {
                App.RtmClient.SyncEverything(() =>
                {
                    LoadData();

                    if (System.Diagnostics.Debugger.IsAttached)
                        ScheduledActionService.LaunchForTest("BackgroundWorker", new TimeSpan(0, 0, 1, 0)); // every minute

                    NotifyComplete();
                });
            }
            else
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    ScheduledActionService.LaunchForTest("BackgroundWorker", new TimeSpan(0, 0, 1, 0)); // every minute

                NotifyComplete();
            }
        }

        private void LoadData()
        {
            LoadDataInBackground();
        }

        private void LoadDataInBackground()
        {
            if (App.RtmClient.TaskLists != null)
            {
                var tempTaskLists = new SortableObservableCollection<TaskList>();

                foreach (TaskList l in App.RtmClient.TaskLists)
                {
                    if (l.Name.ToLower() == "all tasks")
                        tempTaskLists.Insert(0, l);
                    else
                        tempTaskLists.Add(l);
                }

                // check for nearby tasks
                if (_watcher != null &&
                    _watcher.Status != GeoPositionStatus.Disabled)
                {
                    int attempts;

                    // wait for location service to initialize
                    attempts = 0;
                    while ((_watcher.Status != GeoPositionStatus.Ready) &&
                           attempts < 5)
                    {
                        attempts++;
                        System.Threading.Thread.Sleep(1000);
                    }

                    if (attempts < 5)
                    {
                        // wait for accuracy to be within 1 mile
                        attempts = 0;
                        while ((_watcher.Position.Location.HorizontalAccuracy > 1609.344 ||
                                _watcher.Position.Location.VerticalAccuracy > 1609.344) &&
                               attempts < 5)
                        {
                            attempts++;
                            System.Threading.Thread.Sleep(1000);
                        }

                        if (attempts < 5)
                        {
                            foreach (Task item in App.RtmClient.Tasks)
                            {
                                if (item.Location != null)
                                {
                                    if (LocationHelper.Distance(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, item.Location.Latitude, item.Location.Longitude) < 1609.344)
                                    {
                                        ShellToast toast = new ShellToast();
                                        toast.Title = item.Location.Name;
                                        toast.Content = item.Name;
                                        toast.NavigationUri = new Uri("/TaskDetailsPage.xaml?id=" + item.Id, UriKind.Relative);

                                        toast.Show();
                                    }
                                }
                            }
                        }
                    }
                }

                // update live tiles
                foreach (ShellTile tile in ShellTile.ActiveTiles)
                {
                    StandardTileData data = new StandardTileData();

                    string id = tile.NavigationUri.ToString().Split('=')[1];
                    TaskList list = App.RtmClient.TaskLists.SingleOrDefault(l => l.Id == id);

                    int tasksDueToday = list.Tasks.Where(z => z.DueDateTime.Value.Date == DateTime.Now.Date).Count();

                    if (tasksDueToday == 0)
                        data.BackContent = "No tasks due today";
                    else if (tasksDueToday == 1)
                        data.BackContent = tasksDueToday + " task due today";
                    else
                        data.BackContent = tasksDueToday + " tasks due today";

                    tile.Update(data);
                }
            }
        }
    }
}
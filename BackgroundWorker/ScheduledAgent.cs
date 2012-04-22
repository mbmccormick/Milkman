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
            if (settings.LocationServiceEnabled > 0)
            {
                _watcher = new GeoCoordinateWatcher();
                _watcher.Start();
            }

            // sync data
            SyncData();
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
            AppSettings settings = new AppSettings();

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

                if (settings.LocationServiceEnabled > 0)
                {
                    // check for nearby tasks
                    foreach (Task item in App.RtmClient.Tasks)
                    {
                        if (item.Location != null)
                        {
                            double radius;
                            if (settings.LocationServiceEnabled == 1)
                                radius = 1.0;
                            else if (settings.LocationServiceEnabled == 2)
                                radius = 2.0;
                            else if (settings.LocationServiceEnabled == 3)
                                radius = 5.0;
                            else
                                radius = 1.0;

                            if (LocationHelper.Distance(_watcher.Position.Location.Latitude, _watcher.Position.Location.Longitude, item.Location.Latitude, item.Location.Longitude) <= radius)
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
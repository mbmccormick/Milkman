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

                var tempOverdueTasks = new SortableObservableCollection<Task>();
                var tempTodayTasks = new SortableObservableCollection<Task>();
                var tempTomorrowTasks = new SortableObservableCollection<Task>();
                var tempWeekTasks = new SortableObservableCollection<Task>();
                var tempNoDueTasks = new SortableObservableCollection<Task>();

                var tempTags = new SortableObservableCollection<string>();

                foreach (TaskList l in App.RtmClient.TaskLists)
                {
                    tempTaskLists.Add(l);

                    if (l.IsNormal && l.Tasks != null)
                    {
                        foreach (Task task in l.Tasks)
                        {
                            // add tags
                            foreach (string tag in task.Tags)
                            {
                                if (!tempTags.Contains(tag)) tempTags.Add(tag);
                            }

                            if (task.IsIncomplete)
                            {
                                if (task.DueDateTime.HasValue)
                                {
                                    // overdue
                                    if (task.DueDateTime.Value < DateTime.Today || (task.HasDueTime && task.DueDateTime.Value < DateTime.Now))
                                    {
                                        tempOverdueTasks.Add(task);
                                    }
                                    // today
                                    else if (task.DueDateTime.Value.Date == DateTime.Today)
                                    {
                                        tempTodayTasks.Add(task);
                                    }
                                    // tomorrow
                                    else if (task.DueDateTime.Value.Date == DateTime.Today.AddDays(1))
                                    {
                                        tempTomorrowTasks.Add(task);
                                    }
                                    // this week
                                    else if (task.DueDateTime.Value.Date > DateTime.Today.AddDays(1) && task.DueDateTime.Value.Date <= DateTime.Today.AddDays(6))
                                    {
                                        tempWeekTasks.Add(task);
                                    }
                                }
                                else
                                {
                                    // no due
                                    tempNoDueTasks.Add(task);
                                }
                            }
                        }
                    }
                }

                tempOverdueTasks.Sort();
                tempTodayTasks.Sort();
                tempTomorrowTasks.Sort();
                tempWeekTasks.Sort();
                tempNoDueTasks.Sort();

                tempTags.Sort();

                // check for nearby tasks
                if (_watcher != null &&
                    _watcher.Status != GeoPositionStatus.Disabled)
                {
                    foreach (var item in tempTodayTasks.Concat(tempTomorrowTasks).Concat(tempOverdueTasks).Concat(tempWeekTasks).Concat(tempNoDueTasks))
                    {
                        if (item.Location != null)
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

                            if (attempts == 5) break;

                            // wait for accuracy to be within 1 mile
                            attempts = 0;
                            while ((_watcher.Position.Location.HorizontalAccuracy > 1609.344 ||
                                    _watcher.Position.Location.VerticalAccuracy > 1609.344) &&
                                   attempts < 5)
                            {
                                attempts++;
                                System.Threading.Thread.Sleep(1000);
                            }

                            if (attempts == 5) break;

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

                // update live tile data
                ShellTile primaryTile = ShellTile.ActiveTiles.First();
                if (primaryTile != null)
                {
                    StandardTileData data = new StandardTileData();

                    int tasksDueToday = tempTodayTasks.Count + tempOverdueTasks.Where(z => z.DueDateTime.Value.Date == DateTime.Now.Date).Count();

                    data.BackTitle = "Milkman";
                    if (tasksDueToday == 0)
                        data.BackContent = "No tasks due today";
                    else if (tasksDueToday == 1)
                        data.BackContent = tasksDueToday + " task due today";
                    else
                        data.BackContent = tasksDueToday + " tasks due today";

                    primaryTile.Update(data);
                }
            }
        }
    }
}
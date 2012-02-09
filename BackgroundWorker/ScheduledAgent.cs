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

        GeoPosition<GeoCoordinate> _position;

        protected override void OnInvoke(ScheduledTask task)
        {
            // load cached data
            App.LoadData();

            AppSettings settings = new AppSettings();

            // update current location
            if (settings.LocationNotificationsEnabled == true)
            {
                GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
                watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>((object sender, GeoPositionChangedEventArgs<GeoCoordinate> e) =>
                {
                    _position = e.Position;
                });
                watcher.Start();
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

                AppSettings settings = new AppSettings();

                // check for nearby tasks
                if (_position != null)
                {
                    foreach (var item in tempTodayTasks.Concat(tempTomorrowTasks).Concat(tempWeekTasks).Concat(tempNoDueTasks))
                    {
                        if (item.Location != null)
                        {
                            if (LocationHelper.Distance(_position.Location.Latitude, _position.Location.Longitude, item.Location.Latitude, item.Location.Longitude) < 1609.344)
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

                    data.BackTitle = "Milkman";
                    if (tempTodayTasks.Count == 0)
                        data.BackContent = "No tasks due today";
                    else if (tempTodayTasks.Count == 1)
                        data.BackContent = tempTodayTasks.Count + " task due today";
                    else
                        data.BackContent = tempTodayTasks.Count + " tasks due today";

                    primaryTile.Update(data);
                }
            }
        }
    }
}
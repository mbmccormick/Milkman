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
using System.Collections.ObjectModel;
using IronCow;
using System.ComponentModel;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Scheduler;
using System.Device.Location;
using IronCow.Resources;

namespace BackgroundWorker.Common
{
    public class NotificationsManager
    {
        public static void SetupNotifications(GeoCoordinate location)
        {
            AppSettings settings = new AppSettings();

            double radius;
            if (settings.NearbyRadius == 0)
                radius = 1.0;
            else if (settings.NearbyRadius == 1)
                radius = 2.0;
            else if (settings.NearbyRadius == 2)
                radius = 5.0;
            else if (settings.NearbyRadius == 3)
                radius = 10.0;
            else if (settings.NearbyRadius == 4)
                radius = 20.0;
            else
                radius = 0.0;

            // setup location notifications
            if (settings.LocationRemindersEnabled == true &&
                location != null)
            {
                // check for nearby tasks
                UpdateLocationNotifications(location, radius);
            }

            // update live tiles
            UpdateLiveTiles(location, radius);
        }

        public static void ClearNotifications()
        {
            ResetLiveTiles();
        }

        public static void UpdateLocationNotifications(GeoCoordinate location, double radius)
        {
            foreach (Task t in App.RtmClient.GetNearbyTasks(location.Latitude, location.Longitude, radius))
            {
                if (t.HasDueTime &&
                    t.DueDateTime.Value <= DateTime.Now)
                {
                    ShellToast toast = new ShellToast();

                    toast.Title = t.Location.Name;
                    toast.Content = t.Name;
                    toast.NavigationUri = new Uri("/TaskDetailsPage.xaml?id=" + t.Id, UriKind.Relative);
                    toast.Show();
                }
            }
        }

        public static void UpdateLiveTiles(GeoCoordinate location, double radius)
        {
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.ToString() == "/") // application tile
                {
                    FlipTileData data = new FlipTileData();

                    int tasksDueToday = 0;
                    int tasksDueTomorrow = 0;
                    int tasksOverdue = 0;

                    if (App.RtmClient.TaskLists != null)
                    {
                        var tempAllTasks = new SortableObservableCollection<Task>();

                        foreach (TaskList l in App.RtmClient.TaskLists)
                        {
                            if (l.IsSmart == false &&
                                l.Tasks != null)
                            {
                                foreach (Task t in l.Tasks)
                                {
                                    tempAllTasks.Add(t);
                                }
                            }
                        }

                        tasksDueToday = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                                z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                        tasksDueTomorrow = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                                   z.DueDateTime.Value.Date == DateTime.Now.Date.AddDays(1)).Count();
                        tasksOverdue = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                               z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                    }

                    data.BackTitle = Strings.AllTasks;

                    if (tasksDueToday == 0)
                        data.BackContent = Strings.LiveTileTodayEmpty;
                    else if (tasksDueToday == 1)
                        data.BackContent = tasksDueToday + " " + Strings.LiveTileTodaySingle;
                    else
                        data.BackContent = tasksDueToday + " " + Strings.LiveTileTodayPlural;

                    if (tasksDueTomorrow > 0)
                        data.BackContent += ", " + tasksDueTomorrow + " " + Strings.LiveTileTomorrow;

                    if (tasksOverdue > 0)
                        data.BackContent += ", " + tasksOverdue + " " + Strings.LiveTileOverdue;

                    tile.Update(data);
                }
                else if (tile.NavigationUri.ToString().StartsWith("/TaskListByLocationPage.xaml") == true) // nearby task list
                {
                    IconicTileData data = new IconicTileData();

                    int tasksNearby = 0;

                    if (App.RtmClient.TaskLists != null)
                    {
                        tasksNearby = App.RtmClient.GetNearbyTasks(location.Latitude, location.Longitude, radius).Count;
                    }

                    data.Title = Strings.Nearby;
                    data.Count = tasksNearby;

                    if (tasksNearby == 0)
                        data.WideContent1 = Strings.LiveTileNearbyEmpty;
                    else if (tasksNearby == 1)
                        data.WideContent1 = tasksNearby + " " + Strings.LiveTileNearbySingle;
                    else
                        data.WideContent1 = tasksNearby + " " + Strings.LiveTileNearbyPlural;

                    tile.Update(data);
                }
                else if (tile.NavigationUri.ToString().StartsWith("/TaskListByTagPage.xaml") == true) // tag task list
                {
                    IconicTileData data = new IconicTileData();

                    string taskListName = null;
                    int tasksDueToday = 0;
                    int tasksDueTomorrow = 0;
                    int tasksOverdue = 0;

                    if (App.RtmClient.TaskLists != null)
                    {
                        string id = tile.NavigationUri.ToString().Split('=')[1];
                        var tasks = App.RtmClient.GetTasksByTag()[id];

                        taskListName = id;
                        if (tasks != null)
                        {
                            tasksDueToday = tasks.Where(z => z.DueDateTime.HasValue &&
                                                             z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                            tasksDueTomorrow = tasks.Where(z => z.DueDateTime.HasValue &&
                                                                z.DueDateTime.Value.Date == DateTime.Now.Date.AddDays(1)).Count();
                            tasksOverdue = tasks.Where(z => z.DueDateTime.HasValue &&
                                                            z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                        }
                    }

                    data.Title = taskListName;
                    data.Count = tasksDueToday;

                    if (tasksDueToday == 0)
                        data.WideContent1 = Strings.LiveTileTodayEmpty;
                    else if (tasksDueToday == 1)
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTileTodaySingle;
                    else
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTileTodayPlural;

                    if (tasksDueTomorrow == 0)
                        data.WideContent2 = Strings.LiveTileTomorrowEmpty;
                    else if (tasksDueTomorrow == 1)
                        data.WideContent2 = tasksDueTomorrow + " " + Strings.LiveTileTomorrowSingle;
                    else
                        data.WideContent2 = tasksDueTomorrow + " " + Strings.LiveTileTomorrowPlural;

                    if (tasksOverdue == 0)
                        data.WideContent3 = Strings.LiveTileOverdueEmpty;
                    else if (tasksOverdue == 1)
                        data.WideContent3 = tasksOverdue + " " + Strings.LiveTileOverdueSingle;
                    else
                        data.WideContent3 = tasksOverdue + " " + Strings.LiveTileOverduePlural;

                    tile.Update(data);
                }
                else // standard task list
                {
                    IconicTileData data = new IconicTileData();

                    string taskListName = null;
                    int tasksDueToday = 0;
                    int tasksDueTomorrow = 0;
                    int tasksOverdue = 0;

                    if (App.RtmClient.TaskLists != null)
                    {
                        string id = tile.NavigationUri.ToString().Split('=')[1];
                        TaskList list = App.RtmClient.TaskLists.SingleOrDefault(l => l.Id == id);

                        taskListName = list.Name;
                        if (list.Tasks != null)
                        {
                            tasksDueToday = list.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                                  z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                            tasksDueTomorrow = list.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                                     z.DueDateTime.Value.Date == DateTime.Now.Date.AddDays(1)).Count();
                            tasksOverdue = list.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                                 z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                        }
                    }

                    data.Title = taskListName;
                    data.Count = tasksDueToday;

                    if (tasksDueToday == 0)
                        data.WideContent1 = Strings.LiveTileTodayEmpty;
                    else if (tasksDueToday == 1)
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTileTodaySingle;
                    else
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTileTodayPlural;

                    if (tasksDueTomorrow == 0)
                        data.WideContent2 = Strings.LiveTileTomorrowEmpty;
                    else if (tasksDueTomorrow == 1)
                        data.WideContent2 = tasksDueTomorrow + " " + Strings.LiveTileTomorrowSingle;
                    else
                        data.WideContent2 = tasksDueTomorrow + " " + Strings.LiveTileTomorrowPlural;

                    if (tasksOverdue == 0)
                        data.WideContent3 = Strings.LiveTileOverdueEmpty;
                    else if (tasksOverdue == 1)
                        data.WideContent3 = tasksOverdue + " " + Strings.LiveTileOverdueSingle;
                    else
                        data.WideContent3 = tasksOverdue + " " + Strings.LiveTileOverduePlural;

                    tile.Update(data);
                }
            }
        }

        public static void ResetLiveTiles()
        {
            // remove live tiles
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.ToString() == "/")
                {
                    StandardTileData data = new StandardTileData();

                    data.BackTitle = null;
                    data.BackContent = null;

                    tile.Update(data);
                }
                else
                {
                    tile.Delete();
                }
            }
        }
    }
}

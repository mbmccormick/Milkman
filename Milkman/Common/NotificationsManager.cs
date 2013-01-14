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
using Milkman.Common;
using IronCow;
using IronCow.Resources;
using System.ComponentModel;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Scheduler;
using System.Device.Location;

namespace Milkman.Common
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
            else if (settings.NearbyRadius == 3)
                radius = 20.0;
            else
                radius = 0.0;

            double interval;
            if (settings.TaskRemindersEnabled == 1)
                interval = -0.5;
            else if (settings.TaskRemindersEnabled == 2)
                interval = -1.0;
            else if (settings.TaskRemindersEnabled == 3)
                interval = -2.0;
            else
                interval = -1.0;

            // create background worker, if necessary
            if (ScheduledActionService.Find("BackgroundWorker") == null)
            {
                PeriodicTask task = new PeriodicTask("BackgroundWorker");
                task.Description = Strings.PeriodicTaskDescription;

                ScheduledActionService.Add(task);
            }

            // increase background worder interval for debug mode
            if (System.Diagnostics.Debugger.IsAttached)
                ScheduledActionService.LaunchForTest("BackgroundWorker", new TimeSpan(0, 0, 1, 0)); // every minute

            // remove existing reminders
            ResetReminders();

            // setup task reminders
            if (settings.TaskRemindersEnabled > 0)
            {
                UpdateReminders(interval);
            }

            // update live tiles
            UpdateLiveTiles(location, radius);
        }

        public static void ClearNotifications()
        {
            ResetReminders();
            ResetLiveTiles();
        }

        public static void UpdateReminders(double interval)
        {
            // create new reminders
                if (App.RtmClient.TaskLists != null)
                {
                    foreach (TaskList l in App.RtmClient.TaskLists)
                    {
                        if (l.IsSmart == false &&
                            l.Tasks != null)
                        {
                            foreach (Task t in l.Tasks)
                            {
                                if (t.HasDueTime &&
                                    t.DueDateTime.Value.AddHours(interval) >= DateTime.Now)
                                {
                                    Reminder r = new Reminder(t.Id);

                                    if (t.Name.Length > 63)
                                        r.Title = t.Name.Substring(0, 60) + "...";
                                    else
                                        r.Title = t.Name;

                                    r.Content = Strings.TaskReminderPrefix + " " + t.FriendlyDueDate.Replace(Strings.Due + " ", "") + ".";
                                    r.NavigationUri = new Uri("/TaskDetailsPage.xaml?id=" + t.Id, UriKind.Relative);
                                    r.BeginTime = t.DueDateTime.Value.AddHours(interval);

                                    try
                                    {
                                        ScheduledActionService.Add(r);
                                    }
                                    catch (Exception ex)
                                    {
                                        // do nothing
                                    }
                                }
                            }
                        }
                    }
                }
        }

        public static void ResetReminders()
        {
            try
            {
                // delete all existing reminders
                foreach (var item in ScheduledActionService.GetActions<Reminder>())
                    ScheduledActionService.Remove(item.Name);
            }
            catch (Exception ex)
            {
                // do nothing
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
                        tasksOverdue = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                               z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                    }

                    data.BackTitle = Strings.AllTasks;

                    if (tasksDueToday == 0)
                        data.BackContent = Strings.LiveTileEmpty;
                    else if (tasksDueToday == 1)
                        data.BackContent = tasksDueToday + " " + Strings.LiveTileSingle;
                    else
                        data.BackContent = tasksDueToday + " " + Strings.LiveTilePlural;

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
                        data.WideContent1 = Strings.LiveTileEmpty;
                    else if (tasksDueToday == 1)
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTileSingle;
                    else
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTilePlural;

                    if (tasksDueTomorrow > 0)
                        data.WideContent2 = tasksDueTomorrow + " tasks due tomorrow"; // TODO: fix this
                    else if (tasksDueTomorrow == 1)
                        data.WideContent2 = tasksDueTomorrow + " task due tomorrow";
                    else
                        data.WideContent2 = "No tasks due tomorrow";

                    if (tasksOverdue > 0)
                        data.WideContent3 = tasksOverdue + " tasks overdue"; // TODO: fix this
                    else if (tasksOverdue == 1)
                        data.WideContent3 = tasksOverdue + " task overdue";
                    else
                        data.WideContent3 = "";

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
                        data.WideContent1 = Strings.LiveTileEmpty;
                    else if (tasksDueToday == 1)
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTileSingle;
                    else
                        data.WideContent1 = tasksDueToday + " " + Strings.LiveTilePlural;

                    if (tasksDueTomorrow > 0)
                        data.WideContent2 = tasksDueTomorrow + " tasks due tomorrow"; // TODO: fix this
                    else if (tasksDueTomorrow == 1)
                        data.WideContent2 = tasksDueTomorrow + " task due tomorrow";
                    else
                        data.WideContent2 = "No tasks due tomorrow";

                    if (tasksOverdue > 0)
                        data.WideContent3 = tasksOverdue + " tasks overdue"; // TODO: fix this
                    else if (tasksOverdue == 1)
                        data.WideContent3 = tasksOverdue + " task overdue";
                    else
                        data.WideContent3 = "";

                    tile.Update(data);
                }
            }
        }

        public static void ResetLiveTiles()
        {
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

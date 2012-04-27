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
using System.ComponentModel;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Scheduler;

namespace Milkman.Common
{
    public class NotificationsManager
    {
        public static void SetupNotifications()
        {
            AppSettings settings = new AppSettings();

            // stop and restart background worker
            if (ScheduledActionService.Find("BackgroundWorker") != null)
                ScheduledActionService.Remove("BackgroundWorker");

            PeriodicTask task = new PeriodicTask("BackgroundWorker");
            task.Description = "Manages task reminders, location notifications, and live tile updates.";

            ScheduledActionService.Add(task);

            if (System.Diagnostics.Debugger.IsAttached)
                ScheduledActionService.LaunchForTest("BackgroundWorker", new TimeSpan(0, 0, 1, 0)); // every minute

            // setup task reminders
            if (settings.TaskRemindersEnabled > 0)
            {
                // delete all existing reminders
                try
                {
                    foreach (var item in ScheduledActionService.GetActions<Reminder>())
                        ScheduledActionService.Remove(item.Name);
                }
                catch (Exception ex)
                {
                    // do nothing
                }

                // create new reminders
                if (App.RtmClient.TaskLists != null)
                {
                    List<string> alreadyCounted = new List<string>();

                    foreach (TaskList l in App.RtmClient.TaskLists)
                    {
                        if (l.Tasks != null)
                        {
                            foreach (Task t in l.Tasks)
                            {
                                if (alreadyCounted.Contains(t.Id))
                                    continue;
                                else
                                    alreadyCounted.Add(t.Id);

                                double interval;
                                if (settings.TaskRemindersEnabled == 1)
                                    interval = -0.5;
                                else if (settings.TaskRemindersEnabled == 2)
                                    interval = -1.0;
                                else if (settings.TaskRemindersEnabled == 3)
                                    interval = -2.0;
                                else
                                    interval = -1.0;

                                if (t.HasDueTime &&
                                    t.DueDateTime.Value.AddHours(interval) >= DateTime.Now)
                                {
                                    Reminder r = new Reminder(t.Id);

                                    if (t.Name.Length > 63)
                                        r.Title = t.Name.Substring(0, 60) + "...";
                                    else
                                        r.Title = t.Name;

                                    r.Content = "This task is due " + t.FriendlyDueDate.Replace("Due ", "") + ".";
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
            else
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

            // update live tiles
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                StandardTileData data = new StandardTileData();

                string tasksListName = null;
                int tasksDueToday = 0;

                if (tile.NavigationUri.ToString() == "/")
                {
                    var tempAllTasks = new SortableObservableCollection<Task>();

                    foreach (TaskList l in App.RtmClient.TaskLists)
                    {
                        if (l.Tasks != null)
                        {
                            foreach (Task t in l.Tasks)
                            {
                                if (tempAllTasks.Contains(t))
                                    continue;
                                else
                                    tempAllTasks.Add(t);
                            }
                        }
                    }

                    tasksListName = "All Tasks";
                    tasksDueToday = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                            z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                }
                else
                {
                    string id = tile.NavigationUri.ToString().Split('=')[1];
                    TaskList list = App.RtmClient.TaskLists.SingleOrDefault(l => l.Id == id);

                    tasksListName = list.Name;
                    tasksDueToday = list.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                          z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                }

                data.BackTitle = tasksListName;

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

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

namespace BackgroundWorker.Common
{
    public class NotificationsManager
    {
        public static void SetupNotifications(GeoCoordinate location)
        {
            AppSettings settings = new AppSettings();

            // setup location notifications
            if (settings.LocationServiceEnabled > 0)
            {
                // check for nearby tasks
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

                                double radius;
                                if (settings.LocationServiceEnabled == 1)
                                    radius = 1.0;
                                else if (settings.LocationServiceEnabled == 2)
                                    radius = 2.0;
                                else if (settings.LocationServiceEnabled == 3)
                                    radius = 5.0;
                                else
                                    radius = 1.0;

                                if (t.Location != null)
                                {
                                    if (LocationHelper.Distance(location.Latitude, location.Longitude, t.Location.Latitude, t.Location.Longitude) <= radius)
                                    {
                                        ShellToast toast = new ShellToast();

                                        toast.Title = t.Location.Name;
                                        toast.Content = t.Name;
                                        toast.NavigationUri = new Uri("/TaskDetailsPage.xaml?id=" + t.Id, UriKind.Relative);
                                        toast.Show();
                                    }
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

                string tasksListName = null;
                int tasksDueToday = 0;
                int tasksOverdue = 0;

                if (tile.NavigationUri.ToString() == "/")
                {
                    if (App.RtmClient.TaskLists != null)
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
                        tasksOverdue = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                               z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                    }
                }
                else
                {
                    if (App.RtmClient.TaskLists != null)
                    {
                        string id = tile.NavigationUri.ToString().Split('=')[1];
                        TaskList list = App.RtmClient.TaskLists.SingleOrDefault(l => l.Id == id);

                        tasksListName = list.Name;
                        tasksDueToday = list.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                              z.DueDateTime.Value.Date == DateTime.Now.Date).Count();
                        tasksOverdue = list.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                             z.DueDateTime.Value.Date < DateTime.Now.Date).Count();
                    }
                }

                data.BackTitle = tasksListName;

                if (tasksDueToday == 0)
                    data.BackContent = "No tasks due today";
                else if (tasksDueToday == 1)
                    data.BackContent = tasksDueToday + " task due today";
                else
                    data.BackContent = tasksDueToday + " tasks due today";

                if (tasksOverdue > 0)
                    data.BackContent += ", " + tasksOverdue + " overdue";

                tile.Update(data);
            }
        }

        public static void ClearNotifications()
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

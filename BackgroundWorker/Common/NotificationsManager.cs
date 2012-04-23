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
                // delete all existing reminders
                foreach (var item in ScheduledActionService.GetActions<Reminder>())
                    ScheduledActionService.Remove(item.Name);

                // create new reminders
                if (App.RtmClient.TaskLists != null)
                {
                    foreach (TaskList l in App.RtmClient.TaskLists)
                    {
                        if (l.Tasks != null)
                        {
                            foreach (Task t in l.Tasks)
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
                if (tile.NavigationUri.ToString() == "/") continue;

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

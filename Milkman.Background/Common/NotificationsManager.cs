using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Shell;
using Milkman.Common;
using System;
using System.Device.Location;
using System.Linq;

namespace Milkman.Background.Common
{
    public class NotificationsManager
    {
        public static void SetupNotifications(GeoCoordinate location)
        {
            AppSettings settings = new AppSettings();

            // check for task reminders
            CheckForReminderNotifications();

            if (settings.LocationRemindersEnabled == true &&
                location != null)
            {
                // check for location reminders
                CheckForLocationNotifications(location);
            }

            // update live tiles
            UpdateLiveTiles(location);
        }

        public static void CheckForReminderNotifications()
        {
            AppSettings settings = new AppSettings();

            double interval = settings.FriendlyReminderInterval;

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
                                t.DueDateTime.Value <= DateTime.Now.AddMinutes(interval) &&
                                t.DueDateTime.Value.ToUniversalTime() >= ScheduledAgent.LastBackgroundExecutionTime)
                            {
                                ShellToast toast = new ShellToast();

                                toast.Title = t.Name;
                                toast.Content = Strings.TaskReminderPrefix + " " + t.FriendlyDueDate.Replace(Strings.Due + " ", "") + ".";
                                toast.NavigationUri = new Uri("/TaskDetailsPage.xaml?id=" + t.Id, UriKind.Relative);
                                toast.Show();
                            }
                        }
                    }
                }
            }
        }

        public static void CheckForLocationNotifications(GeoCoordinate location)
        {
            AppSettings settings = new AppSettings();

            double radius = settings.FriendlyNearbyRadius;

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

        public static void UpdateLiveTiles(GeoCoordinate location)
        {
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.ToString() == "/") // application tile
                {
                    FlipTileData data = LiveTileManager.RenderApplicationLiveTile();

                    tile.Update(data);
                }
                else if (tile.NavigationUri.ToString().StartsWith("/TaskListByLocationPage.xaml") == true) // nearby task list
                {
                    FlipTileData data = LiveTileManager.RenderNearbyLiveTile(location);

                    tile.Update(data);
                }
                else if (tile.NavigationUri.ToString().StartsWith("/TaskListByTagPage.xaml") == true) // tag task list
                {
                    if (App.RtmClient.TaskLists != null)
                    {
                        string id = tile.NavigationUri.ToString().Split('=')[1];

                        FlipTileData data = LiveTileManager.RenderLiveTile(id);

                        tile.Update(data);
                    }
                }
                else // standard task list
                {
                    if (App.RtmClient.TaskLists != null)
                    {
                        string id = tile.NavigationUri.ToString().Split('=')[1];
                        TaskList list = App.RtmClient.TaskLists.SingleOrDefault(l => l.Id == id);

                        FlipTileData data = LiveTileManager.RenderLiveTile(list);

                        tile.Update(data);
                    }
                }
            }
        }

        public static void ResetLiveTiles()
        {
            foreach (ShellTile tile in ShellTile.ActiveTiles)
            {
                if (tile.NavigationUri.ToString() == "/")
                {
                    FlipTileData data = new FlipTileData();

                    data.BackgroundImage = new Uri("/Assets/FlipCycleTileMedium.png", UriKind.Relative);
                    data.SmallBackgroundImage = new Uri("/Assets/FlipCycleTileSmall.png", UriKind.Relative);
                    data.WideBackgroundImage = new Uri("/Assets/FlipCycleTileWide.png", UriKind.Relative);
                    data.Title = Strings.Milkman;
                    data.Count = 0;

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

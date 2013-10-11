using IronCow;
using Microsoft.Phone.Shell;
using Milkman.Common;
using System;
using System.Device.Location;
using System.Linq;

namespace BackgroundWorker.Common
{
    public class NotificationsManager
    {
        public static void SetupNotifications(GeoCoordinate location)
        {
            AppSettings settings = new AppSettings();

            // setup location notifications
            if (settings.LocationRemindersEnabled == true &&
                location != null)
            {
                // check for nearby tasks
                UpdateLocationNotifications(location);
            }

            // update live tiles
            UpdateLiveTiles(location);
        }

        public static void ClearNotifications()
        {
            ResetLiveTiles();
        }

        public static void UpdateLocationNotifications(GeoCoordinate location)
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
                    StandardTileData data = new StandardTileData();

                    data.BackTitle = "";
                    data.BackContent = "";

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

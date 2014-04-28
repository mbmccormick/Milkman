using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using System.Device.Location;
using System.Linq;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Devices.Geolocation;
using Windows.ApplicationModel.Background;

namespace Milkman.Common
{
    public class NotificationsManager
    {
        public async static void SetupNotifications(GeoCoordinate location)
        {
            AppSettings settings = new AppSettings();

            // update geofences
            UpdateGeofences();

            // update live tiles
            UpdateLiveTiles(location);

            // create background worker, if necessary
            if (ScheduledActionService.Find("BackgroundTask") == null)
            {
                PeriodicTask task = new PeriodicTask("BackgroundTask");
                task.Description = Strings.PeriodicTaskDescription;

                ScheduledActionService.Add(task);
            }

            // increase background worder interval for debug mode
            if (System.Diagnostics.Debugger.IsAttached)
                ScheduledActionService.LaunchForTest("BackgroundTask", new TimeSpan(0, 0, 1, 0)); // every minute

            // register for background geofence updates
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            var geofenceTaskBuilder = new BackgroundTaskBuilder
            {
                Name = "GeofenceAgent",
                TaskEntryPoint = "AgHost.BackgroundTask"
            };

            var trigger = new LocationTrigger(LocationTriggerType.Geofence);
            geofenceTaskBuilder.SetTrigger(trigger);

            var geofenceTask = geofenceTaskBuilder.Register();
            geofenceTask.Completed += (sender, args) =>
            {
                // do nothing
            };
        }

        public static void UpdateGeofences()
        {
            var geofenceMonitor = GeofenceMonitor.Current;

            AppSettings settings = new AppSettings();

            while (geofenceMonitor.Geofences.Count > 0)
            {
                geofenceMonitor.Geofences.RemoveAt(0);
            }
            
            if (App.RtmClient.Locations != null)
            {
                foreach (Location l in App.RtmClient.Locations)
                {
                    var location = new Geopoint(new BasicGeoposition()
                    {
                        Latitude = l.Latitude,
                        Longitude = l.Longitude
                    });

                    var radius = settings.FriendlyNearbyRadius * 1609; // convert to meters

                    var geofence = new Geofence(l.Id, new Geocircle(location.Position, radius), MonitoredGeofenceStates.Entered | MonitoredGeofenceStates.Exited, false, TimeSpan.FromSeconds(10));

                    geofenceMonitor.Geofences.Add(geofence);
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

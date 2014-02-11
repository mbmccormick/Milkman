using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using System;
using System.Device.Location;
using System.Linq;

namespace Milkman.Common
{
    public class NotificationsManager
    {
        public static void SetupNotifications(GeoCoordinate location)
        {
            AppSettings settings = new AppSettings();

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
            UpdateReminders();

            // update live tiles
            UpdateLiveTiles(location);
        }

        public static void UpdateReminders()
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
                                t.DueDateTime.Value.AddMinutes(interval) >= DateTime.Now)
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

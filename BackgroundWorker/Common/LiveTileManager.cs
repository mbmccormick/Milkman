using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;

namespace BackgroundWorker.Common
{
    public class LiveTileManager
    {
        public static FlipTileData RenderApplicationLiveTile()
        {
            FlipTileData tile = new FlipTileData();

            List<Task> tasksOverdue = new List<Task>();
            List<Task> tasksDueToday = new List<Task>();

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

                tasksOverdue = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                       z.DueDateTime.Value.Date < DateTime.Now.Date).ToList();
                tasksDueToday = tempAllTasks.Where(z => z.DueDateTime.HasValue &&
                                                        z.DueDateTime.Value.Date == DateTime.Now.Date).ToList();
            }

            tile.Title = Strings.Milkman;
            tile.Count = tasksOverdue.Count + tasksDueToday.Count;

            if (tasksDueToday.Count > 0)
            {
                tile.BackContent = tasksDueToday.First().Name;

                if (tasksDueToday.Count > 1)
                {
                    tile.BackTitle = (tasksDueToday.Count - 1) + " " + Strings.LiveTileMoreDueToday;
                }
                else
                {
                    tile.BackTitle = Strings.LiveTileDueToday;
                }
            }
            else if (tasksOverdue.Count > 0)
            {
                tile.BackContent = tasksOverdue.First().Name;

                if (tasksOverdue.Count > 1)
                {
                    tile.BackTitle = (tasksOverdue.Count - 1) + " " + Strings.LiveTileMoreOverdue;
                }
                else
                {
                    tile.BackTitle = Strings.LiveTileOverdue;
                }
            }
            else
            {
                tile.BackContent = "";
                tile.BackTitle = "";
            }

            return tile;
        }

        public static FlipTileData RenderLiveTile(TaskList data)
        {
            FlipTileData tile = new FlipTileData();

            tile.BackgroundImage = new Uri("/Assets/FlipCycleTileMedium.png", UriKind.Relative);
            tile.SmallBackgroundImage = new Uri("/Assets/FlipCycleTileSmall.png", UriKind.Relative);

            List<Task> tasksOverdue = new List<Task>();
            List<Task> tasksDueToday = new List<Task>();

            if (data.Tasks != null)
            {
                tasksOverdue = data.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                     z.DueDateTime.Value.Date < DateTime.Now.Date).ToList();
                tasksDueToday = data.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                      z.DueDateTime.Value.Date == DateTime.Now.Date).ToList();
            }

            tile.Title = data.Name;
            tile.Count = tasksOverdue.Count + tasksDueToday.Count;

            if (tasksDueToday.Count > 0)
            {
                tile.BackContent = tasksDueToday.First().Name;

                if (tasksDueToday.Count > 1)
                {
                    tile.BackTitle = (tasksDueToday.Count - 1) + " " + Strings.LiveTileMoreDueToday;
                }
                else
                {
                    tile.BackTitle = Strings.LiveTileDueToday;
                }
            }
            else if (tasksOverdue.Count > 0)
            {
                tile.BackContent = tasksOverdue.First().Name;

                if (tasksOverdue.Count > 1)
                {
                    tile.BackTitle = (tasksOverdue.Count - 1) + " " + Strings.LiveTileMoreOverdue;
                }
                else
                {
                    tile.BackTitle = Strings.LiveTileOverdue;
                }
            }
            else
            {
                tile.BackContent = "";
                tile.BackTitle = "";
            }

            return tile;
        }

        public static FlipTileData RenderLiveTile(string tagName)
        {
            FlipTileData tile = new FlipTileData();

            tile.BackgroundImage = new Uri("/Assets/FlipCycleTileMedium.png", UriKind.Relative);
            tile.SmallBackgroundImage = new Uri("/Assets/FlipCycleTileSmall.png", UriKind.Relative);

            List<Task> tasksOverdue = new List<Task>();
            List<Task> tasksDueToday = new List<Task>();

            if (App.RtmClient.TaskLists != null)
            {
                var tasks = App.RtmClient.GetTasksByTag()[tagName];

                if (tasks != null)
                {
                    tasksOverdue = tasks.Where(z => z.DueDateTime.HasValue &&
                                                    z.DueDateTime.Value.Date < DateTime.Now.Date).ToList();
                    tasksDueToday = tasks.Where(z => z.DueDateTime.HasValue &&
                                                     z.DueDateTime.Value.Date == DateTime.Now.Date).ToList();
                }
            }

            tile.Title = tagName;
            tile.Count = tasksOverdue.Count + tasksDueToday.Count;

            if (tasksDueToday.Count > 0)
            {
                tile.BackContent = tasksDueToday.First().Name;

                if (tasksDueToday.Count > 1)
                {
                    tile.BackTitle = (tasksDueToday.Count - 1) + " " + Strings.LiveTileMoreDueToday;
                }
                else
                {
                    tile.BackTitle = Strings.LiveTileDueToday;
                }
            }
            else if (tasksOverdue.Count > 0)
            {
                tile.BackContent = tasksOverdue.First().Name;

                if (tasksOverdue.Count > 1)
                {
                    tile.BackTitle = (tasksOverdue.Count - 1) + " " + Strings.LiveTileMoreOverdue;
                }
                else
                {
                    tile.BackTitle = Strings.LiveTileOverdue;
                }
            }
            else
            {
                tile.BackContent = "";
                tile.BackTitle = "";
            }

            return tile;
        }

        public static FlipTileData RenderNearbyLiveTile(GeoCoordinate location)
        {
            FlipTileData tile = new FlipTileData();

            tile.BackgroundImage = new Uri("/Assets/FlipCycleTileMedium.png", UriKind.Relative);
            tile.SmallBackgroundImage = new Uri("/Assets/FlipCycleTileSmall.png", UriKind.Relative);

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

            List<Task> tasksNearby = new List<Task>();

            if (App.RtmClient.TaskLists != null)
            {
                tasksNearby = App.RtmClient.GetNearbyTasks(location.Latitude, location.Longitude, radius).ToList();
            }

            tile.Title = Strings.Nearby;
            tile.Count = tasksNearby.Count;

            if (tasksNearby.Count > 0)
            {
                tile.BackContent = tasksNearby.First().Name;

                if (tasksNearby.Count > 1)
                {
                    tile.BackTitle = (tasksNearby.Count - 1) + " " + Strings.LiveTileMoreNearby;
                }
                else
                {
                    tile.BackTitle = Strings.LiveTileNearby;
                }
            }
            else
            {
                tile.BackContent = "";
                tile.BackTitle = "";
            }

            return tile;
        }
    }
}

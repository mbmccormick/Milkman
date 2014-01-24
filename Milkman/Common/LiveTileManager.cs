using IronCow;
using IronCow.Resources;
using Microsoft.Phone.Shell;
using Milkman.Imaging;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;

namespace Milkman.Common
{
    public class LiveTileManager
    {
        public static FlipTileData RenderApplicationLiveTile()
        {
            FlipTileData tile = new FlipTileData();

            tile.BackgroundImage = new Uri("/Assets/FlipCycleTileMedium.png", UriKind.Relative);
            tile.SmallBackgroundImage = new Uri("/Assets/FlipCycleTileSmall.png", UriKind.Relative);

            List<Task> tasksDueToday = new List<Task>();
            List<Task> tasksOverdue = new List<Task>();

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

            FlipTileTemplate image = new FlipTileTemplate();
            string imagePath = "/Shared/ShellContent/default.png";

            FlipTileTemplateWide imageWide = new FlipTileTemplateWide();
            string imageWidePath = "/Shared/ShellContent/defaultwide.png";

            if (tasksDueToday.Count > 0)
            {
                image.RenderLiveTileImage(imagePath, Strings.LiveTileDueToday + ":", FormatFlipTileTemplateContent(tasksDueToday));
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, Strings.LiveTileDueToday + ":", FormatFlipTileTemplateWideContent(tasksDueToday));
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }
            else if (tasksOverdue.Count > 0)
            {
                image.RenderLiveTileImage(imagePath, Strings.LiveTileOverdue + ":", FormatFlipTileTemplateContent(tasksOverdue));
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, Strings.LiveTileOverdue + ":", FormatFlipTileTemplateWideContent(tasksOverdue));
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }
            else
            {
                image.RenderLiveTileImage(imagePath, "", Strings.EmptyTaskList);
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, "", Strings.EmptyTaskList);
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }

            return tile;
        }

        public static FlipTileData RenderLiveTile(TaskList data)
        {
            FlipTileData tile = new FlipTileData();

            tile.BackgroundImage = new Uri("/Assets/FlipCycleTileMedium.png", UriKind.Relative);
            tile.SmallBackgroundImage = new Uri("/Assets/FlipCycleTileSmall.png", UriKind.Relative);

            List<Task> tasksDueToday = new List<Task>();
            List<Task> tasksOverdue = new List<Task>();
            
            if (data.Tasks != null)
            {
                tasksDueToday = data.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                      z.DueDateTime.Value.Date == DateTime.Now.Date).ToList();
                tasksOverdue = data.Tasks.Where(z => z.DueDateTime.HasValue &&
                                                     z.DueDateTime.Value.Date < DateTime.Now.Date).ToList();
            }

            tile.Title = data.Name;
            tile.Count = tasksOverdue.Count + tasksDueToday.Count;

            FlipTileTemplate image = new FlipTileTemplate();
            string imagePath = "/Shared/ShellContent/" + data.Id + ".png";
            
            FlipTileTemplateWide imageWide = new FlipTileTemplateWide();
            string imageWidePath = "/Shared/ShellContent/" + data.Id + "wide.png";

            if (tasksDueToday.Count > 0)
            {
                image.RenderLiveTileImage(imagePath, Strings.LiveTileDueToday + ":", FormatFlipTileTemplateContent(tasksDueToday));
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, Strings.LiveTileDueToday + ":", FormatFlipTileTemplateWideContent(tasksDueToday));
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }
            else if (tasksOverdue.Count > 0)
            {
                image.RenderLiveTileImage(imagePath, Strings.LiveTileOverdue + ":", FormatFlipTileTemplateContent(tasksOverdue));
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, Strings.LiveTileOverdue + ":", FormatFlipTileTemplateWideContent(tasksOverdue));
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }
            else
            {
                image.RenderLiveTileImage(imagePath, "", Strings.EmptyTaskList);
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, "", Strings.EmptyTaskList);
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }

            return tile;
        }

        public static FlipTileData RenderLiveTile(string tagName)
        {
            FlipTileData tile = new FlipTileData();

            tile.BackgroundImage = new Uri("/Assets/FlipCycleTileMedium.png", UriKind.Relative);
            tile.SmallBackgroundImage = new Uri("/Assets/FlipCycleTileSmall.png", UriKind.Relative);

            List<Task> tasksDueToday = new List<Task>();
            List<Task> tasksOverdue = new List<Task>();
            
            if (App.RtmClient.TaskLists != null)
            {
                var tasks = App.RtmClient.GetTasksByTag()[tagName];

                if (tasks != null)
                {
                    tasksDueToday = tasks.Where(z => z.DueDateTime.HasValue &&
                                                     z.DueDateTime.Value.Date == DateTime.Now.Date).ToList();
                    tasksOverdue = tasks.Where(z => z.DueDateTime.HasValue &&
                                                    z.DueDateTime.Value.Date < DateTime.Now.Date).ToList();
                }
            }

            tile.Title = tagName;
            tile.Count = tasksOverdue.Count + tasksDueToday.Count;

            FlipTileTemplate image = new FlipTileTemplate();
            string imagePath = "/Shared/ShellContent/" + tagName + ".png";

            FlipTileTemplateWide imageWide = new FlipTileTemplateWide();
            string imageWidePath = "/Shared/ShellContent/" + tagName + "wide.png";

            if (tasksDueToday.Count > 0)
            {
                image.RenderLiveTileImage(imagePath, Strings.LiveTileDueToday + ":", FormatFlipTileTemplateContent(tasksDueToday));
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, Strings.LiveTileDueToday + ":", FormatFlipTileTemplateWideContent(tasksDueToday));
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }
            else if (tasksOverdue.Count > 0)
            {
                image.RenderLiveTileImage(imagePath, Strings.LiveTileOverdue + ":", FormatFlipTileTemplateContent(tasksOverdue));
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, Strings.LiveTileOverdue + ":", FormatFlipTileTemplateWideContent(tasksOverdue));
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }
            else
            {
                image.RenderLiveTileImage(imagePath, "", Strings.EmptyTaskList);
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, "", Strings.EmptyTaskList);
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
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

            FlipTileTemplate image = new FlipTileTemplate();
            string imagePath = "/Shared/ShellContent/nearby.png";

            FlipTileTemplateWide imageWide = new FlipTileTemplateWide();
            string imageWidePath = "/Shared/ShellContent/nearbywide.png";

            if (tasksNearby.Count > 0)
            {
                image.RenderLiveTileImage(imagePath, Strings.LiveTileNearby + ":", FormatFlipTileTemplateContent(tasksNearby));
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, Strings.LiveTileNearby + ":", FormatFlipTileTemplateWideContent(tasksNearby));
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }
            else
            {
                image.RenderLiveTileImage(imagePath, "", Strings.EmptyNearbyTaskList);
                tile.BackgroundImage = new Uri("isostore:" + imagePath, UriKind.Absolute);

                imageWide.RenderLiveTileImage(imageWidePath, "", Strings.EmptyNearbyTaskList);
                tile.WideBackgroundImage = new Uri("isostore:" + imageWidePath, UriKind.Absolute);
            }

            return tile;
        }

        private static string FormatFlipTileTemplateContent(List<Task> data)
        {
            return data.First().Name;
        }

        private static string FormatFlipTileTemplateWideContent(List<Task> data)
        {
            string content = "";
            foreach (Task item in data.Take(3))
            {
                content += item.Name + "\n";
            }

            return content;
        }
    }
}

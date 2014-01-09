using IronCow;
using IronCow.Rest;
using Microsoft.WindowsAzure.MobileServices;
using Milkman.Background.Common;
using Milkman.Background.Common.Models;
using Milkman.Common;
using System;
using System.IO.IsolatedStorage;
using System.Linq;

namespace Milkman.Background
{
    public partial class App
    {
        private static readonly string RtmApiKey = "09b03090fc9303804aedd945872fdefc";
        private static readonly string RtmSharedKey = "d2ffaf49356b07f9";

        public static Rtm RtmClient;
        public static Response TasksResponse;
        public static Response ListsResponse;
        public static Response LocationsResponse;
        public static DateTime LastUpdated;

        public static MobileServiceClient MobileService = new MobileServiceClient(
            "https://milkman.azure-mobile.net/",
            "dSiXpghIVfinlEYGqpGROOvclqFWnl56"
        );
                
        public static void LoadData()
        {
            string RtmAuthToken = IsolatedStorageHelper.GetObject<string>("RtmAuthToken");
            int? Timeline = IsolatedStorageHelper.GetObject<int?>("RtmTimeline");
            TasksResponse = IsolatedStorageHelper.GetObject<Response>("TasksResponse");
            ListsResponse = IsolatedStorageHelper.GetObject<Response>("ListsResponse");
            LocationsResponse = IsolatedStorageHelper.GetObject<Response>("LocationsResponse");
            LastUpdated = IsolatedStorageHelper.GetObject<DateTime>("LastUpdated");

            if (!string.IsNullOrEmpty(RtmAuthToken))
            {
                RtmClient = new Rtm(RtmApiKey, RtmSharedKey, RtmAuthToken);
            }
            else
            {
                RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            }

            if (Timeline.HasValue)
            {
                RtmClient.CurrentTimeline = Timeline.Value;
            }

            RtmClient.CacheTasksEvent += OnCacheTasks;
            RtmClient.CacheListsEvent += OnCacheLists;
            RtmClient.CacheLocationsEvent += OnCacheLocations;

            if (LocationsResponse != null)
            {
                RtmClient.LoadLocationsFromResponse(LocationsResponse);
            }
            if (ListsResponse != null)
            {
                RtmClient.LoadListsFromResponse(ListsResponse);
            }
            if (TasksResponse != null)
            {
                RtmClient.LoadTasksFromResponse(TasksResponse);
            }
        }

        public static void SaveData()
        {
            IsolatedStorageHelper.SaveObject<string>("RtmAuthToken", RtmClient.AuthToken);
            IsolatedStorageHelper.SaveObject<int?>("RtmTimeline", RtmClient.CurrentTimeline);
            IsolatedStorageHelper.SaveObject<Response>("TasksResponse", TasksResponse);
            IsolatedStorageHelper.SaveObject<Response>("ListsResponse", ListsResponse);
            IsolatedStorageHelper.SaveObject<Response>("LocationsResponse", LocationsResponse);
            IsolatedStorageHelper.SaveObject<DateTime>("LastUpdated", LastUpdated);

            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        public static void DeleteData()
        {
            IsolatedStorageHelper.DeleteObject("RtmAuthToken");
            IsolatedStorageHelper.DeleteObject("RtmTimeline");
            IsolatedStorageHelper.DeleteObject("TasksResponse");
            IsolatedStorageHelper.DeleteObject("ListsResponse");
            IsolatedStorageHelper.DeleteObject("LocationsResponse");
            IsolatedStorageHelper.DeleteObject("LastUpdated");

            RtmClient = new Rtm(RtmApiKey, RtmSharedKey);
            TasksResponse = null;
            ListsResponse = null;
            LocationsResponse = null;
        }

        public static void OnCacheTasks(Response response)
        {
            TasksResponse = response;
            LastUpdated = DateTime.Now;
        }

        public static void OnCacheLists(Response response)
        {
            ListsResponse = response;
            LastUpdated = DateTime.Now;
        }

        public static void OnCacheLocations(Response response)
        {
            LocationsResponse = response;
            LastUpdated = DateTime.Now;
        }

        #region Push Notifications

        public static async void AcquirePushChannel(double latitude, double longitude)
        {
            IMobileServiceTable<Registrations> registrationsTable = App.MobileService.GetTable<Registrations>();

            var existingRegistrations = await registrationsTable.Where(z => z.AuthenticationToken == App.RtmClient.AuthToken).ToCollectionAsync();

            if (existingRegistrations.Count > 0)
            {
                var registration = existingRegistrations.First();

                registration.Latitude = latitude;
                registration.Longitude = longitude;
                registration.ReminderInterval = new AppSettings().FriendlyReminderInterval;
                registration.NearbyInterval = new AppSettings().FriendlyNearbyRadius;

                await registrationsTable.UpdateAsync(registration);
            }
            else
            {
                var registration = new Registrations
                {
                    AuthenticationToken = App.RtmClient.AuthToken,
                    Latitude = latitude,
                    Longitude = longitude,
                    ReminderInterval = new AppSettings().FriendlyReminderInterval,
                    NearbyInterval = new AppSettings().FriendlyNearbyRadius
                };

                await registrationsTable.InsertAsync(registration);
            }
        }

        #endregion
    }
}
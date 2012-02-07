
namespace IronCow
{
    public class LocationCollection : RtmCollection<Location>
    {
        internal LocationCollection(Rtm owner)
            : base(owner)
        {
        }

        internal void Resync(SyncCallback callback)
        {
            if (Owner.Syncing)
            {
                Items.Clear();
                var request = new RestRequest("rtm.locations.getList");
                request.Callback = response =>
                    {
                        if (response.Locations != null)
                        {
                            foreach (var location in response.Locations)
                            {
                                Items.Add(new Location(location));
                            }
                        }

                        callback();
                    };
                Owner.ExecuteRequest(request);
            }
        }
    }
}

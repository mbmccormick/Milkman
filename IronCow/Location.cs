using IronCow.Rest;

namespace IronCow
{
    public class Location : RtmFatElement
    {
        public string Name { get; private set; }
        public float Longitude { get; private set; }
        public float Latitude { get; private set; }
        public int Zoom { get; private set; }
        public string Address { get; private set; }
        public bool Viewable { get; private set; }

        internal Location(RawLocation location)
        {
            Sync(location);
        }

        public Location(string name)
        {
            Name = name;
        }

        protected override void DoSync(bool firstSync, RawRtmElement element)
        {
            base.DoSync(firstSync, element);

            RawLocation location = (RawLocation)element;
            Name = location.Name;
            Longitude = location.Longitude;
            Latitude = location.Latitude;
            Zoom = location.Zoom;
            Address = location.Address;
            Viewable = (location.Viewable != 0);
        }
    }
}

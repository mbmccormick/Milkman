using IronCow.Rest;

namespace IronCow
{
    public class User
    {
        public string Id { get; private set; }
        public string UserName { get; private set; }
        public string FullName { get; private set; }

        public User(string userName, string fullName)
        {
            Id = RtmElement.UnsyncedId;
            UserName = userName;
            FullName = fullName;
        }

        internal User(RawUser user)
        {
            Id = user.Id;
            UserName = user.UserName;
            FullName = user.FullName;
        }
    }
}

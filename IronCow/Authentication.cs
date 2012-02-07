using IronCow.Rest;

namespace IronCow
{
    public class Authentication
    {
        public string Token { get; private set; }
        public AuthenticationPermissions Permissions { get; private set; }
        public User User { get; private set; }

        internal Authentication(RawAuthentication authentication)
        {
            Token = authentication.Token;
            Permissions = authentication.Permissions;
            User = new User(authentication.User);
        }
    }
}

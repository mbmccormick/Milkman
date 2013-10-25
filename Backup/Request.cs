
namespace IronCow
{
    public abstract class Request
    {
        protected Request()
        {
        }

        public abstract void Execute(Rtm rtm);
    }
}

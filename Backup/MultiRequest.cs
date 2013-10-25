using System.Collections.Generic;

namespace IronCow
{
    public class MultiRequest : Request
    {
        public RequestCollection Requests { get; private set; }

        public MultiRequest()
        {
            Requests = new RequestCollection();
        }

        public MultiRequest(IEnumerable<Request> requests)
            : this()
        {
            foreach (var request in requests)
            {
                Requests.Add(request);
            }
        }

        public override void Execute(Rtm rtm)
        {
            foreach (var request in Requests)
            {
                request.Execute(rtm);
            }
        }
    }
}

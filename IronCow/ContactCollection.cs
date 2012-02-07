using System;

namespace IronCow
{
    public class ContactCollection : SynchronizedRtmCollection<Contact>
    {
        internal ContactCollection(Rtm owner)
            : base(owner)
        {
        }

        protected override void DoResync(SyncCallback callback)
        {
            Clear();
            var request = new RestRequest("rtm.contacts.getList");
            request.Callback = response =>
                {
                    if (response.Contacts != null)
                    {
                        using (new UnsyncedScope(this))
                        {
                            foreach (var contact in response.Contacts)
                            {
                                Add(new Contact(contact));
                            }
                        }
                    }

                    callback();
                };
            Owner.ExecuteRequest(request);
        }

        protected override void ExecuteAddElementRequest(Contact item, SyncCallback callback)
        {
            if (item == null)
                throw new ArgumentNullException("contact");
            RestRequest request = new RestRequest("rtm.contacts.add", r => item.Sync(r.Contact));
            request.Parameters.Add("contact", item.UserName);
            request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
            request.Callback = r => { callback(); };
            Owner.ExecuteRequest(request);
        }

        protected override void ExecuteRemoveElementRequest(Contact item, SyncCallback callback)
        {
            if (item == null)
                throw new ArgumentNullException("contact");
            RestRequest request = new RestRequest("rtm.contacts.delete", r => item.Unsync());
            request.Parameters.Add("contact_id", item.Id.ToString());
            request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
            request.Callback = r => { callback(); };
            Owner.ExecuteRequest(request);
        }
    }
}

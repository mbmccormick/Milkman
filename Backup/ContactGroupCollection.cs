using System;

namespace IronCow
{
    public class ContactGroupCollection : SynchronizedRtmCollection<ContactGroup>
    {
        internal ContactGroupCollection(Rtm owner)
            : base(owner)
        {
        }

        protected override void DoResync(SyncCallback callback)
        {
            Clear();
            var request = new RestRequest("rtm.groups.getList");
            request.Callback = response =>
                {
                    if (response.Groups != null)
                    {
                        using (new UnsyncedScope(this))
                        {
                            foreach (var group in response.Groups)
                            {
                                var contactGroup = new ContactGroup(null);
                                // We need to add the contact group to this collection first so that it
                                // sets its Owner RTM instance. This is because just after that, when syncing
                                // to the RawGroup, it will need to go get the Contacts from the RTM object.
                                Add(contactGroup);
                                contactGroup.Sync(group);
                            }
                        }
                    }

                    callback();
                };
            Owner.ExecuteRequest(request);
        }

        protected override void ExecuteAddElementRequest(ContactGroup item, SyncCallback callback)
        {
            if (item == null)
                throw new ArgumentNullException("contactGroup");
            RestRequest request = new RestRequest("rtm.groups.add", r => item.Sync(r.Group));
            request.Parameters.Add("group", item.Name);
            request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
            request.Callback = r => { callback(); };
            Owner.ExecuteRequest(request);
        }

        protected override void ExecuteRemoveElementRequest(ContactGroup item, SyncCallback callback)
        {
            if (item == null)
                throw new ArgumentNullException("contactGroup");
            RestRequest request = new RestRequest("rtm.groups.delete", r => item.Unsync());
            request.Parameters.Add("group_id", item.Id.ToString());
            request.Parameters.Add("timeline", Owner.GetTimeline().ToString());
            request.Callback = r => { callback(); };
            Owner.ExecuteRequest(request);
        }
    }
}

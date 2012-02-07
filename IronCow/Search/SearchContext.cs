using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronCow.Search
{
    public class SearchContext
    {
        public Task Task { get; private set; }
        public DateFormat DateFormat { get; private set; }
        public bool ShouldExecuteOnServer { get; set; }

        public SearchContext(Task task, DateFormat dateFormat)
        {
            Task = task;
            DateFormat = dateFormat;
            ShouldExecuteOnServer = false;
        }
    }
}

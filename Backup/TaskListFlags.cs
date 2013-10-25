using System;

namespace IronCow
{
    [Flags]
    public enum TaskListFlags
    {
        None = 0,
        Deleted = 1 << 0,
        Locked = 1 << 1,
        Archived = 1 << 2,
        Smart = 1 << 3
    }
}

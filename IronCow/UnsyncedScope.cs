using System;

namespace IronCow
{
    internal struct UnsyncedScope : IDisposable
    {
        private bool mPreviousSyncing;
        private ISyncing mTarget;

        public UnsyncedScope(ISyncing target)
        {
            mTarget = target;
            mPreviousSyncing = target.Syncing;
            mTarget.Syncing = false;
        }

        #region IDisposable Members

        public void Dispose()
        {
            mTarget.Syncing = mPreviousSyncing;
        }

        #endregion
    }
}

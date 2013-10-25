
namespace IronCow
{
    public struct Recurrence
    {
        private string mRepeat;
        public string Repeat { get { return mRepeat; } }

        private bool mIsEvery;
        public bool IsEvery { get { return mIsEvery; } }

        public Recurrence(string repeat, bool isEvery)
        {
            mRepeat = repeat;
            mIsEvery = isEvery;
        }
    }
}

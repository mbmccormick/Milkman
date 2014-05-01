using System;

namespace IronCow
{
    public struct FuzzyDateTime : IComparable, IComparable<FuzzyDateTime>, IEquatable<FuzzyDateTime>
    {
        public static FuzzyDateTime Today
        {
            get { return new FuzzyDateTime(DateTime.Today, false); }
        }

        public static FuzzyDateTime Now
        {
            get { return new FuzzyDateTime(DateTime.Now, true); }
        }

        public static bool operator ==(FuzzyDateTime d1, FuzzyDateTime d2)
        {
            return d1.Equals(d2);
        }

        public static bool operator !=(FuzzyDateTime d1, FuzzyDateTime d2)
        {
            return !d1.Equals(d2);
        }

        public static bool operator <=(FuzzyDateTime d1, FuzzyDateTime d2)
        {
            return d1.CompareTo(d2) <= 0;
        }

        public static bool operator >=(FuzzyDateTime d1, FuzzyDateTime d2)
        {
            return d1.CompareTo(d2) >= 0;
        }

        private static DateTime GetComparableDateTime(DateTime dateTime, bool hasTime)
        {
            if (hasTime)
                return dateTime;
            else
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
        }

        private DateTime mDateTime;
        public DateTime DateTime { get { return mDateTime; } }

        private bool mHasTime;
        public bool HasTime { get { return mHasTime; } }

        public FuzzyDateTime(DateTime dateTime, bool hasTime)
        {
            mHasTime = hasTime;
            mDateTime = GetComparableDateTime(dateTime, hasTime);
        }

        public override bool Equals(object obj)
        {
            return (this.CompareTo(obj) == 0);
        }

        public override int GetHashCode()
        {
            return mDateTime.GetHashCode();
        }

        public override string ToString()
        {
            if (mHasTime)
                return mDateTime.ToString();
            else
                return mDateTime.ToString("yyyy-MM-dd");
        }

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            if (!(obj is FuzzyDateTime))
                throw new ArgumentException("Argument is not a FuzzyDateTime.");
            return this.CompareTo((FuzzyDateTime)obj);
        }

        #endregion

        #region IComparable<FuzzyDateTime> Members

        public int CompareTo(FuzzyDateTime other)
        {
            if (mHasTime == other.mHasTime)
                return mDateTime.CompareTo(other.mDateTime);

            var thisDateTime = GetComparableDateTime(mDateTime, false);
            var otherDateTime = GetComparableDateTime(other.mDateTime, false);
            return thisDateTime.CompareTo(otherDateTime);
        }

        #endregion

        #region IEquatable<FuzzyDateTime> Members

        public bool Equals(FuzzyDateTime other)
        {
            if (mHasTime == other.mHasTime)
                return mDateTime.Equals(other.mDateTime);

            var thisDateTime = GetComparableDateTime(mDateTime, false);
            var otherDateTime = GetComparableDateTime(other.mDateTime, false);
            return thisDateTime.Equals(otherDateTime);
        }

        #endregion
    }
}

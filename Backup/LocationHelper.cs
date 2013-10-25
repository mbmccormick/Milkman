using System;

namespace IronCow.Common
{
    public static class LocationHelper
    {
        private static double ConvertToRadians(double val)
        {
            return val * (Math.PI / 180);
        }

        private static double DifferenceInRadians(double val1, double val2)
        {
            return ConvertToRadians(val2) - ConvertToRadians(val1);
        }

        public static double Distance(double lat1, double lng1, double lat2, double lng2)
        {
            double radius = 6367000.0;
            double meters = radius * 2 * Math.Asin(Math.Min(1, Math.Sqrt((Math.Pow(Math.Sin((DifferenceInRadians(lat1, lat2)) / 2.0), 2.0) + Math.Cos(ConvertToRadians(lat1)) * Math.Cos(ConvertToRadians(lat2)) * Math.Pow(Math.Sin((DifferenceInRadians(lng1, lng2)) / 2.0), 2.0)))));

            return meters / 1609.344;
        }
    }
}

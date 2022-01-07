namespace GeoTimeZone
{
    /// <inheritdoc cref="TimeZoneLookup"/>
    public interface ITimeZoneLookup
    {
        /// <inheritdoc cref="TimeZoneLookup.GetTimeZone(double,double)"/>
        TimeZoneResult GetTimeZone(double latitude, double longitude);
    }
}

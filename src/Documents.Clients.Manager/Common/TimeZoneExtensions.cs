
namespace Documents.Clients.Manager.Common
{
    using Documents.Clients.Manager.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public static class TimeZoneExtensions
    {
        public static DateTime ConvertToLocal(this DateTime utcTime, string userTimeZone)
        {
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

            TimeZoneInfo timeZoneInfo = TimeZoneConverter.TZConvert.GetTimeZoneInfo(userTimeZone);

            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZoneInfo);
        }

        public static DateTime ConvertToUtc(this DateTime localTime, string userTimeZone)
        {
            localTime = DateTime.SpecifyKind(localTime, DateTimeKind.Local);

            TimeZoneInfo timeZoneInfo = TimeZoneConverter.TZConvert.GetTimeZoneInfo(userTimeZone);

            return TimeZoneInfo.ConvertTimeToUtc(localTime, timeZoneInfo);
        }
    }
}
/*
 * A libray of time related routines since I'm a spoiled VFP developer
 * 
 */
using System.Globalization;

namespace JAXBase.Utilities
{
    public class TimeLib
    {
        private static DateTime epoch = new DateTime(1970, 1, 1).ToUniversalTime();
        //private static readonly DateTimeSpan oDateTimeSpan = new();
        public static CultureInfo oCulture = new("en-US");

        public static int ErrNo { get; private set; } = 0;
        public static string ErrMessage { get; private set; } = string.Empty;
        public static string ErrProcedure { get; private set; } = string.Empty;


        // TODO - add ability to use other than US format
        public static DateTime GMT() { return GMT(DateTime.Now); }
        public static DateTime GMT(DateTime now) { return now.ToUniversalTime(); }
        public static DateTime Local() { return DateTime.Now.ToLocalTime(); }

        // Return date string in format of MM/DD/YYYY
        public static string ToDateString(DateTime dateTime) { return dateTime.ToString("MM/dd/yyyy"); }

        // Return date string in format of YYYY-MM-DDTHH:MM:SS.mmmmmmm+/-HH:MM
        public static string ToSQLDateString(DateTime dateTime) { return dateTime.ToString("yyyy-MM-ddTHH:mm:ss"); }

        // Return date string in format of YYYYMMDDHHMMSS military time
        public static string ToIDXDateString(DateTime dateTime) { return dateTime.ToString("yyyyMMddHHmmss"); }

        // Return date string in format of YYYYMMDD
        public static string DToS(DateTime dateTime) { return dateTime.ToString("yyyyMMdd"); }

        // Return time string in format of HH:MM:SS military time
        public static string MilitaryTimeString(DateTime dateTime) { return dateTime.ToString("HH:mm:ss"); }

        // Return zone offset
        public static string ZoneOffset(DateTime dateTime) { return dateTime.ToString("zzz"); }

        // Return date string in format of MM-DD-YYYY HH:MM
        public static string MilitaryDateString(DateTime dateTime) { return dateTime.ToString("yyyy-MM-dd HH:mm"); }

        // Convert DateTime to DateOnly
        public static DateOnly TtoD(DateTime dt) { return new DateOnly(dt.Year, dt.Month, dt.Day); }

        // Convert DateOnly to DateTime
        public static DateTime DtoT(DateOnly dt) { return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0); }

        // Get midnight for today
        public static DateTime Midnight(DateTime Now) { return Midnight(Now, 0); }

        // Get midnight for today +/- number of days
        public static DateTime Midnight(DateTime Now, int AddDays)
        {
            return new DateTime(Now.Year, Now.Month, Now.Day, 0, 0, 0).AddDays(AddDays);
        }


        public static string ToSQLDate(DateTime ttDate)
        {
            return ttDate.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK");
        }


        // ****************************************************************************
        // ****************************************************************************
        // Convert a string to a datetime value (assumes GMT time)
        // Will accept any valid date in formats of
        //      M/d/yyyy, MM/dd/yyyy, yyyy/MM/dd, or yyyy/M/d
        //      The date can be delimited by dashes instead of slashes
        //
        //      Time format can be 24 or 12 hour format with or without seconds
        //      Timezone info, if it exists, must start with + or - and be in correct format
        //
        // If the second parameter is true, then it will convert the GMT time, if
        // the second parameter is false, it just returns the GMT time
        // ============================================================================
        public static DateTime? CToT(string sTime) { return CToT(sTime, true); }
        public static DateTime? CToT(string sTime, bool assumeLocalTime)
        {
            CultureInfo enUS = new("en-US");
            DateTime? tResult = null;
            string sDatePart;
            string sTimePart = string.Empty;
            string sZonePart = string.Empty;
            string sFormat;

            if (sTime.Length > 7)
            {
                // Valid time String so fix it
                sDatePart = sTime.Replace('T', ' ');

                if (sDatePart.Contains(' '))
                {
                    sTimePart = sDatePart[sDatePart.IndexOf(' ')..].Trim().ToUpper();
                    sDatePart = sDatePart[..sDatePart.IndexOf(' ')].Trim();


                    if (sTimePart.Contains("AM") || sTimePart.Contains("PM"))
                    {
                        // AM/PM timepart
                        sZonePart = sTimePart[(sTimePart.IndexOf("M") + 1)..].Trim();
                        sTimePart = sTimePart[..(sTimePart.IndexOf("M") + 1)].Trim();
                    }
                    else
                    {
                        if (sTimePart.Contains('-'))
                        {
                            sZonePart = sTimePart[sTimePart.IndexOf('-')..].Trim();
                            sTimePart = sTimePart[..(sTimePart.IndexOf('-') - 1)].Trim();
                        }
                        else if (sTimePart.Contains('+'))
                        {
                            sZonePart = sTimePart[sTimePart.IndexOf('+')..].Trim();
                            sTimePart = sTimePart[..(sTimePart.IndexOf('+') - 1)].Trim();
                        }
                        else if (sTimePart.Contains('Z', StringComparison.OrdinalIgnoreCase))
                        {
                            sZonePart = "00:00";
                            sTimePart = sTimePart[..sTimePart.IndexOf("Z", StringComparison.CurrentCultureIgnoreCase)].Trim();
                        }

                        if (sTimePart.Contains('.'))
                            sTimePart = sTimePart[..sTimePart.IndexOf('.')].Trim();

                    }
                }


                sDatePart = sDatePart.Replace('/', '-');
                var aDate = sDatePart.Split('-');

                if (aDate.Length == 3)
                {
                    // Possible valid date
                    if (aDate[0].Length == 4)
                    {
                        sFormat = (aDate[1].Length == 2 ? "yyyy-MM" : "yyyy-M");
                        sFormat += (aDate[2].Length == 2 ? "-dd" : "-d");
                    }
                    else
                    {
                        sFormat = (aDate[0].Length == 2 ? "MM" : "M");
                        sFormat += (aDate[1].Length == 2 ? "-dd" : "-d");
                        sFormat += "-yyyy";
                    }

                    if (sTimePart.Length > 0)
                    {
                        aDate = sTimePart.Split(":");
                        var sHour = (aDate[0].Length == 2 ? "hh" : "h");

                        if (sTimePart.Contains("AM") || sTimePart.Contains("PM"))
                        {
                            // 12 hour format (AM/PM)
                            sFormat += (aDate.Length == 3 ? " {0}:mm:ss" : " {0}:mm") + " tt";
                            sFormat = string.Format(sFormat, sHour);
                        }
                        else
                        {
                            // 24 hour format (Military)
                            sFormat += (aDate.Length == 3 ? " {0}:mm:ss" : " {0}:mm");
                            sFormat = string.Format(sFormat, sHour.ToUpper());
                        }

                        if (sZonePart.Length > 0)
                        {
                            // Fix zone offsets so it reads +/-hh:mm
                            var sZSign = sZonePart.Contains('+') ? "+" : "-";
                            sZonePart = sZonePart.Replace("-", "").Replace("+", "").Trim();
                            aDate = (sZonePart + ":").Split(":");
                            aDate[0] = aDate[0].Length == 1 ? "0" + aDate[0] : aDate[0];
                            aDate[1] = aDate[1].Length == 1 ? "0" + aDate[1] : aDate[1];
                            sZonePart = string.Format("{0}{1}:{2}", sZSign, aDate[0], aDate[1]);

                            sFormat += " zzz";
                        }
                    }

                    sDatePart = string.Format("{0} {1} {2}", sDatePart, sTimePart, sZonePart).Trim();
                    if (DateTime.TryParseExact(sDatePart, sFormat, enUS, DateTimeStyles.None, out DateTime dtt) == false)
                        tResult = null;
                    else
                    {
                        // Last Step - do we convert it to local time?
                        if (sZonePart.Length > 0 && assumeLocalTime)
                            tResult = dtt.ToLocalTime();
                        else
                            tResult = dtt;
                    }
                }
            }

            return tResult;
        }


        // ****************************************************************************
        // ****************************************************************************
        // UTC Time functions based off this server's clock
        // ============================================================================
        public static DateTime TodayUTC()
        {
            return DateTime.Today.ToUniversalTime(); // Day starts at midnight (00:00)
        }

        public static DateTime TomorrowUTC()
        {
            return DateTime.Today.ToUniversalTime().AddDays(1); // Tonight at midnight
        }

        // Get number of seconds since epoch
        public static long SecondsSinceEpochUTC()
        {
            TimeSpan t = DateTime.UtcNow - epoch;
            return (int)t.TotalSeconds;
        }

        public static int SecondsSinceMidnightUTC()
        {
            TimeSpan t = DateTime.UtcNow - DateTime.Today.ToUniversalTime();
            return (int)t.TotalSeconds;
        }

        // ****************************************************************************
        // ****************************************************************************
        // Server Local Time
        // ============================================================================
        public static DateTime UTCtoLocal(DateTime ttUTC, TimeZoneInfo otZone)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(ttUTC, otZone);
        }

        public static DateTime LocaltoUCT(DateTime ttLocal, TimeZoneInfo otZone)
        {
            return TimeZoneInfo.ConvertTimeToUtc(ttLocal, otZone);
        }

        public static DateTime TodayLocal()
        {
            return DateTime.Today; // Day starts at midnight (00:00)
        }

        public static DateTime TomorrowLocal()
        {
            return DateTime.Today.AddDays(1); // Tonight at midnight
        }

        public static int SecondsSinceMidnightLocal()
        {
            TimeSpan t = DateTime.Now - TodayLocal();
            return (int)t.TotalSeconds;
        }
    }
}

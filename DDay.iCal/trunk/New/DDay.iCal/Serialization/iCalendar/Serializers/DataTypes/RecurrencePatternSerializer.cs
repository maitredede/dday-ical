﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace DDay.iCal.Serialization.iCalendar
{
    public class RecurrencePatternSerializer :
        EncodableDataTypeSerializer
    {
        #region Static Public Methods

        static public DayOfWeek GetDayOfWeek(string value)
        {
            switch (value.ToUpper())
            {
                case "SU": return DayOfWeek.Sunday;
                case "MO": return DayOfWeek.Monday;
                case "TU": return DayOfWeek.Tuesday;
                case "WE": return DayOfWeek.Wednesday;
                case "TH": return DayOfWeek.Thursday;
                case "FR": return DayOfWeek.Friday;
                case "SA": return DayOfWeek.Saturday;
            }
            throw new ArgumentException(value + " is not a valid iCal day-of-week indicator.");
        }        

        #endregion

        #region Static Protected Methods

        static protected void AddInt32Values(IList<int> list, string value)
        {
            string[] values = value.Split(',');
            foreach (string v in values)
                list.Add(Convert.ToInt32(v));
        }

        #endregion

        #region Private Methods

        private void SerializeByValue(List<string> aggregate, IList<int> byValue, string name)
        {
            if (byValue.Count > 0)
            {
                List<string> byValues = new List<string>();
                foreach (int i in byValue)
                    byValues.Add(i.ToString());

                aggregate.Add(name + "=" + string.Join(",", byValues.ToArray()));
            }
        }

        #endregion

        #region Overrides

        public override Type TargetType
        {
            get { return typeof(RecurrencePattern); }
        }

        public override string SerializeToString(object obj)
        {
            IRecurrencePattern recur = obj as IRecurrencePattern;
            if (recur != null)
            {
                List<string> values = new List<string>();

                values.Add("FREQ=" + Enum.GetName(typeof(FrequencyType), recur.Frequency).ToUpper());

                //-- FROM RFC2445 --
                //The INTERVAL rule part contains a positive integer representing how
                //often the recurrence rule repeats. The default value is "1", meaning
                //every second for a SECONDLY rule, or every minute for a MINUTELY
                //rule, every hour for an HOURLY rule, every day for a DAILY rule,
                //every week for a WEEKLY rule, every month for a MONTHLY rule and
                //every year for a YEARLY rule.
                int interval = recur.Interval;
                if (interval == int.MinValue)
                    interval = 1;

                if (interval != 1)
                    values.Add("INTERVAL=" + interval);

                if (recur.Until.IsAssigned)
                {
                    IStringSerializer serializer = new DateTimeSerializer();
                    if (serializer != null)
                        values.Add("UNTIL=" + serializer.SerializeToString(recur.Until));
                }

                if (recur.WeekStart != DayOfWeek.Monday)
                    values.Add("WKST=" + Enum.GetName(typeof(DayOfWeek), recur.WeekStart).ToUpper().Substring(0, 2));

                if (recur.Count != int.MinValue)
                    values.Add("COUNT=" + recur.Count);

                if (recur.ByDay.Count > 0)
                {
                    List<string> bydayValues = new List<string>();

                    IStringSerializer serializer = new DaySpecifierSerializer();
                    if (serializer != null)
                    {
                        foreach (DaySpecifier byday in recur.ByDay)
                            bydayValues.Add(serializer.SerializeToString(byday));
                    }

                    values.Add("BYDAY=" + string.Join(",", bydayValues.ToArray()));
                }

                SerializeByValue(values, recur.ByHour, "BYHOUR");
                SerializeByValue(values, recur.ByMinute, "BYMINUTE");
                SerializeByValue(values, recur.ByMonth, "BYMONTH");
                SerializeByValue(values, recur.ByMonthDay, "BYMONTHDAY");
                SerializeByValue(values, recur.BySecond, "BYSECOND");
                SerializeByValue(values, recur.BySetPosition, "BYSETPOS");
                SerializeByValue(values, recur.ByWeekNo, "BYWEEKNO");
                SerializeByValue(values, recur.ByYearDay, "BYYEARDAY");
                
                return Encode(recur, string.Join(";", values.ToArray()));
            }
            return null;
        }

        public override object Deserialize(TextReader tr)
        {
            string value = tr.ReadToEnd();

            // Instantiate the data type
            IRecurrencePattern r = CreateAndAssociate() as IRecurrencePattern;

            // Decode the value, if necessary
            value = Decode(r, value);

            Match match = Regex.Match(value, @"FREQ=(SECONDLY|MINUTELY|HOURLY|DAILY|WEEKLY|MONTHLY|YEARLY);?(.*)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                // Parse the frequency type
                r.Frequency = (FrequencyType)Enum.Parse(typeof(FrequencyType), match.Groups[1].Value, true);

                // NOTE: fixed a bug where the group 2 match
                // resulted in an empty string, which caused
                // an error.
                if (match.Groups[2].Success &&
                    match.Groups[2].Length > 0)
                {
                    string[] keywordPairs = match.Groups[2].Value.Split(';');
                    foreach (string keywordPair in keywordPairs)
                    {
                        string[] keyValues = keywordPair.Split('=');
                        string keyword = keyValues[0];
                        string keyValue = keyValues[1];

                        switch (keyword.ToUpper())
                        {
                            case "UNTIL":
                                {
                                    DateTimeSerializer serializer = new DateTimeSerializer();
                                    r.Until = (iCalDateTime)serializer.Deserialize(new StringReader(keyValue));
                                } break;
                            case "COUNT": r.Count = Convert.ToInt32(keyValue); break;
                            case "INTERVAL": r.Interval = Convert.ToInt32(keyValue); break;
                            case "BYSECOND": AddInt32Values(r.BySecond, keyValue); break;
                            case "BYMINUTE": AddInt32Values(r.ByMinute, keyValue); break;
                            case "BYHOUR": AddInt32Values(r.ByHour, keyValue); break;
                            case "BYDAY":
                                {
                                    string[] days = keyValue.Split(',');
                                    foreach (string day in days)
                                        r.ByDay.Add(new DaySpecifier(day));
                                } break;
                            case "BYMONTHDAY": AddInt32Values(r.ByMonthDay, keyValue); break;
                            case "BYYEARDAY": AddInt32Values(r.ByYearDay, keyValue); break;
                            case "BYWEEKNO": AddInt32Values(r.ByWeekNo, keyValue); break;
                            case "BYMONTH": AddInt32Values(r.ByMonth, keyValue); break;
                            case "BYSETPOS": AddInt32Values(r.BySetPosition, keyValue); break;
                            case "WKST": r.WeekStart = GetDayOfWeek(keyValue); break;
                        }
                    }
                }
            }
            else if ((match = Regex.Match(value, @"every\s+(?<Interval>other|\d+)?\w{0,2}\s*(?<Freq>second|minute|hour|day|week|month|year)s?,?\s*(?<More>.+)", RegexOptions.IgnoreCase)).Success)
            {
                r = new RecurrencePattern();

                if (match.Groups["Interval"].Success)
                {
                    int interval;
                    if (!int.TryParse(match.Groups["Interval"].Value, out interval))
                        r.Interval = 2; // "other"
                    else r.Interval = interval;
                }
                else r.Interval = 1;

                switch (match.Groups["Freq"].Value.ToLower())
                {
                    case "second": r.Frequency = FrequencyType.Secondly; break;
                    case "minute": r.Frequency = FrequencyType.Minutely; break;
                    case "hour": r.Frequency = FrequencyType.Hourly; break;
                    case "day": r.Frequency = FrequencyType.Daily; break;
                    case "week": r.Frequency = FrequencyType.Weekly; break;
                    case "month": r.Frequency = FrequencyType.Monthly; break;
                    case "year": r.Frequency = FrequencyType.Yearly; break;
                }

                string[] values = match.Groups["More"].Value.Split(',');
                foreach (string item in values)
                {
                    if ((match = Regex.Match(item, @"(?<Num>\d+)\w\w\s+(?<Type>second|minute|hour|day|week|month)", RegexOptions.IgnoreCase)).Success ||
                        (match = Regex.Match(item, @"(?<Type>second|minute|hour|day|week|month)\s+(?<Num>\d+)", RegexOptions.IgnoreCase)).Success)
                    {
                        int num;
                        if (int.TryParse(match.Groups["Num"].Value, out num))
                        {
                            switch (match.Groups["Type"].Value.ToLower())
                            {
                                case "second":
                                    r.BySecond.Add(num);
                                    break;
                                case "minute":
                                    r.ByMinute.Add(num);
                                    break;
                                case "hour":
                                    r.ByHour.Add(num);
                                    break;
                                case "day":
                                    switch (r.Frequency)
                                    {
                                        case FrequencyType.Yearly:
                                            r.ByYearDay.Add(num);
                                            break;
                                        case FrequencyType.Monthly:
                                            r.ByMonthDay.Add(num);
                                            break;
                                    }
                                    break;
                                case "week":
                                    r.ByWeekNo.Add(num);
                                    break;
                                case "month":
                                    r.ByMonth.Add(num);
                                    break;
                            }
                        }
                    }
                    else if ((match = Regex.Match(item, @"(?<Num>\d+\w{0,2})?(\w|\s)+?(?<First>first)?(?<Last>last)?\s*((?<Day>sunday|monday|tuesday|wednesday|thursday|friday|saturday)\s*(and|or)?\s*)+", RegexOptions.IgnoreCase)).Success)
                    {
                        int num = int.MinValue;
                        if (match.Groups["Num"].Success)
                        {
                            if (int.TryParse(match.Groups["Num"].Value, out num))
                            {
                                if (match.Groups["Last"].Success)
                                {
                                    // Make number negative
                                    num *= -1;
                                }
                            }
                        }
                        else if (match.Groups["Last"].Success)
                            num = -1;
                        else if (match.Groups["First"].Success)
                            num = 1;

                        foreach (Capture capture in match.Groups["Day"].Captures)
                        {
                            DaySpecifier ds = new DaySpecifier((DayOfWeek)Enum.Parse(typeof(DayOfWeek), capture.Value, true));
                            ds.Num = num;
                            r.ByDay.Add(ds);
                        }
                    }
                    else if ((match = Regex.Match(item, @"at\s+(?<Hour>\d{1,2})(:(?<Minute>\d{2})((:|\.)(?<Second>\d{2}))?)?\s*(?<Meridian>(a|p)m?)?", RegexOptions.IgnoreCase)).Success)
                    {
                        int hour, minute, second;

                        if (int.TryParse(match.Groups["Hour"].Value, out hour))
                        {
                            // Adjust for PM
                            if (match.Groups["Meridian"].Success &&
                                match.Groups["Meridian"].Value.ToUpper().StartsWith("P"))
                                hour += 12;

                            r.ByHour.Add(hour);

                            if (match.Groups["Minute"].Success &&
                                int.TryParse(match.Groups["Minute"].Value, out minute))
                            {
                                r.ByMinute.Add(minute);
                                if (match.Groups["Second"].Success &&
                                    int.TryParse(match.Groups["Second"].Value, out second))
                                    r.BySecond.Add(second);
                            }
                        }
                    }
                    else if ((match = Regex.Match(item, @"^\s*until\s+(?<DateTime>.+)$", RegexOptions.IgnoreCase)).Success)
                    {
                        DateTime dt = DateTime.Parse(match.Groups["DateTime"].Value);
                        DateTime.SpecifyKind(dt, DateTimeKind.Utc);

                        r.Until = new iCalDateTime(dt);
                    }
                    else if ((match = Regex.Match(item, @"^\s*for\s+(?<Count>\d+)\s+occurrences\s*$", RegexOptions.IgnoreCase)).Success)
                    {
                        int count;
                        if (!int.TryParse(match.Groups["Count"].Value, out count))
                            return false;
                        else r.Count = count;
                    }
                }
            }
            else
            {
                // Couldn't parse the object, return null!
                r = null;
            }

            if (r != null)
            {
                CheckMutuallyExclusive("COUNT", "UNTIL", r.Count, r.Until);
                CheckRange("INTERVAL", r.Interval, 1, int.MaxValue);
                CheckRange("COUNT", r.Count, 1, int.MaxValue);
                CheckRange("BYSECOND", r.BySecond, 0, 59);
                CheckRange("BYMINUTE", r.ByMinute, 0, 59);
                CheckRange("BYHOUR", r.ByHour, 0, 23);
                CheckRange("BYMONTHDAY", r.ByMonthDay, -31, 31);
                CheckRange("BYYEARDAY", r.ByYearDay, -366, 366);
                CheckRange("BYWEEKNO", r.ByWeekNo, -53, 53);
                CheckRange("BYMONTH", r.ByMonth, 1, 12);
                CheckRange("BYSETPOS", r.BySetPosition, -366, 366);
            }

            return r;            
        }

        #endregion
    }
}

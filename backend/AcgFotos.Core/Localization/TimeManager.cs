using AcgFotos.Core.Localization;
using System;

namespace AcgFotos.Core.Utils
{

    public class TimeManager : ITimeManager
    {

        public DateTime DateTimeNow
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        public DateTime ArgentinDateTimeNow
        {
            get
            {
                return DateTime.UtcNow.AddHours(-3);
            }
        }

        public DateTime ToArgentinaTime(DateTime date)
        {
            return date.AddHours(-3);
        }

        public DateTime ToUniversalTime(DateTime date)
        {
            return date.AddHours(3);
        }

    }
}

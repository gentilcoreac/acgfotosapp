using System;
using System.Collections.Generic;
using System.Text;

namespace AcgFotos.Core.Localization
{
    public interface ITimeManager
    {
        DateTime DateTimeNow { get; }

        DateTime ArgentinDateTimeNow { get; }

        DateTime ToArgentinaTime(DateTime date);

        DateTime ToUniversalTime(DateTime date);
    }
}

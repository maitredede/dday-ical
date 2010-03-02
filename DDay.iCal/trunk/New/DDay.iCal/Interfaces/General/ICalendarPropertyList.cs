﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DDay.iCal
{
    public interface ICalendarPropertyList : 
        IKeyedList<ICalendarProperty, string>
    {
        void Set(string name, object value);
        T Get<T>(string name);
        T[] GetAll<T>(string name);
    }
}

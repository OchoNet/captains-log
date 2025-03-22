using System;
using System.Collections.Generic;

namespace CaptainsLog.Models
{
    // holds the Log info for a single day
    public class DailyLog
    {
        // which day this Log is for
        public DateTime Date { get; set; }

        // list of Tasks we added that day
        public List<TaskEntry> Tasks { get; set; } = new();

        // returns the filename we should use to store this Log
        public string GetFileName()
        {
            // format: LogYYYYMMDD.csv
            return $"Log{Date:yyyyMMdd}.csv";
        }
    }
}
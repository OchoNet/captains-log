using System;
using System.Collections.Generic;
using System.IO;
using CaptainsLog.Models;

namespace CaptainsLog.Utils
{
    // helper for reading/writing Log files
    public static class CsvHelper
    {
        // reads a Log file and returns a list of TaskEntry objects
        public static List<TaskEntry> ReadLogFile(string filePath)
        {
            var tasks = new List<TaskEntry>();

            // if no Log file, do nothing
            if (!File.Exists(filePath))
                return tasks;

            // read all lines from the Log file
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                // split the line into fields
                var parts = line.Split(',');
                if (parts.Length < 4) continue; // skip bad rows

                // create the Task from values in the line
                tasks.Add(new TaskEntry
                {
                    IsComplete = parts[0].Trim() == "1",  // 1 = done
                    Title = parts[1].Trim(),
                    Time = parts[2].Trim(),
                    Account = parts[3].Trim()
                });
            }

            return tasks;
        }

        // writes a list of Tasks to the Log file
        public static void WriteLogFile(string filePath, List<TaskEntry> tasks)
        {
            var lines = new List<string>();

            foreach (var task in tasks)
            {
                // convert each Task to a single line
                var complete = task.IsComplete ? "1" : "0";
                lines.Add($"{complete},{task.Title},{task.Time},{task.Account}");
            }

            // write the full list to the Log file
            File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));
        }
    }
}
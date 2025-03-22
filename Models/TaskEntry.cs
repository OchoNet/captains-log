namespace CaptainsLog.Models
{
    // one row on the Task list
    public class TaskEntry
    {
        // whether this Task is done 
        public bool IsComplete { get; set; }

        // the actual Task name
        public string Title { get; set; } = string.Empty;

        // how much time was spent 
        public string Time { get; set; } = string.Empty;

        // which account the Task was for 
        public string Account { get; set; } = string.Empty;

        // used to display the Tasks
        public override string ToString()
        {
            return $"{Account}: {Title}";
        }
    }
}
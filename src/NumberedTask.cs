namespace todotxtlib.net
{
    /// <summary>
    /// Represents a Task and its position in its source list. Numbering is 1-based.
    /// </summary>
    /// <param name="Number">The position of the Task in its source list.</param>
    /// <param name="Task">The Task</param>
    public record NumberedTask(int Number, Task Task)
    {
        public override string ToString()
        {
            return Task.ToString();
        }

        public string ToNumberedString(string format)
        {
            return $"{Number.ToString(format)} {Task}";
        }

        public char? Priority => Task.Priority;
        public bool IsPriority => Task.IsPriority;
        public bool Completed => Task.Completed;
    }
}
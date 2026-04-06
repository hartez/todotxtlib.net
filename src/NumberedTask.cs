using System;

namespace todotxtlib.net
{
    /// <summary>
    /// Represents a Task and its position in its source list. Numbering is 1-based.
    /// </summary>
    /// <param name="Number">The position of the Task in its source list.</param>
    /// <param name="Task">The Task</param>
    /// <param name="Format">The format method provided by the parent TaskList</param>
    public record NumberedTask(int Number, Task Task, Func<NumberedTask, string> Format)
    {
        public override string ToString()
        {
            return Format(this);
        }

        public char? Priority => Task.Priority;
        public bool IsPriority => Task.IsPriority;
        public bool Completed => Task.Completed;
    }
}
using System.Collections.Generic;
using System.Linq;

namespace todotxtlib.net
{
    public static class TaskListExtensions
    {
        extension(IEnumerable<NumberedTask> tasks)
        {
            public IEnumerable<string> ToNumberedOutput()
            {
                return tasks.Select(numberedTask => numberedTask.ToString());
            }

            public IEnumerable<string> ToOutput()
            {
                return tasks.Select(numberedTask => numberedTask.Task.ToString());
            }
        }
    }
}
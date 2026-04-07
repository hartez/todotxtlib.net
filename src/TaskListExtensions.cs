using System.Collections.Generic;
using System.IO;
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

            public void Save(string filePath)
            {
                try
                {
                    File.WriteAllLines(filePath, [.. tasks.Select(numberedTask => numberedTask.Task.ToString())]);
                }
                catch (IOException ex)
                {
                    throw new TaskException("There was a problem trying to save your file", ex);
                }
            }
        }
    }
}
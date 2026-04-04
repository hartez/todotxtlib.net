using System.Collections;
using System.Collections.Generic;

namespace todotxtlib.net
{
    public class TaskListView : IOutputTasks, IEnumerable<NumberedTask>
    {
        private readonly TaskList _taskList = [];

        internal TaskListView(IEnumerable<NumberedTask> todos)
        {
            foreach (var todo in todos)
            {
                _taskList.Add(todo);
            }
        }

        public IEnumerator<NumberedTask> GetEnumerator()
        {
            return ((IEnumerable<NumberedTask>)_taskList).GetEnumerator();
        }

        public IEnumerable<string> ToNumberedOutput()
        {
            return _taskList.ToNumberedOutput();
        }

        public IEnumerable<string> ToOutput()
        {
            return _taskList.ToOutput();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _taskList.GetEnumerator();
        }
    }
}
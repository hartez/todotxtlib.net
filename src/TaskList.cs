using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace todotxtlib.net
{
    public class TaskList : IEnumerable<NumberedTask>
    {
        private readonly List<NumberedTask> _tasks = [];

        public int Count => _tasks.Count;

        private string Format(NumberedTask numberedTask)
        {
            string numberFormat = new('0', Count.ToString().Length);
            return $"{numberedTask.Number.ToString(numberFormat)} {numberedTask.Task}";
        }

        public TaskList()
        {
        }

        public TaskList(string filePath)
        {
            LoadTasks(filePath);
        }

        public override string ToString()
        {
            return _tasks.Aggregate(string.Empty, (acc, numberedTask) =>
                acc + (acc.Length == 0 ? string.Empty : Environment.NewLine) + numberedTask.ToString());
        }

        public IEnumerable<string> ToNumberedOutput()
        {
            return _tasks.Select(numberedTask => numberedTask.ToString());
        }

        public IEnumerable<NumberedTask> ListCompleted()
        {
            return from numberedTask in _tasks
                   where numberedTask.Completed
                   select numberedTask;
        }

        public IEnumerable<NumberedTask> Search(string term)
        {
            bool include = true;

            if (term.StartsWith('-'))
            {
                include = false;
                term = term[1..];
            }

            return from numberedTask in _tasks
                   where !(include ^ numberedTask.Task.Body.Contains(term, StringComparison.OrdinalIgnoreCase))
                   select numberedTask;
        }

        public IEnumerable<NumberedTask> GetPriority(char? priority)
        {
            if (priority == null)
            {
                return from numberedTask in _tasks
                       where numberedTask.IsPriority
                       orderby numberedTask.Priority
                       select numberedTask;
            }

            return from numberedTask in _tasks
                   where numberedTask.Priority == char.ToUpper(priority.Value)
                   select numberedTask;
        }

        public void SetItemPriority(int itemNumber, char priority)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);
            _tasks[index] = new NumberedTask(itemNumber, task.WithPriority(priority), Format);
        }

        public void ClearItemPriority(int itemNumber)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);
            _tasks[index] = new NumberedTask(itemNumber, task.ClearPriority(), Format);
        }

        public void MarkCompleted(int itemNumber)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);
            if (task.Completed)
            {
                return;
            }

            _tasks[index] = new NumberedTask(itemNumber, task.WithCompleted(), Format);
        }

        public void MarkPending(int itemNumber)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);
            if (!task.Completed)
            {
                return;
            }

            _tasks[index] = new NumberedTask(itemNumber, task.WithPending(), Format);
        }

        public void ToggleCompleted(int itemNumber)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);

            _tasks[index] = new NumberedTask(itemNumber, task.Completed ? task.WithPending() : task.WithCompleted(), Format);
        }

        private bool ReplaceItemText(int itemNumber, string oldText, string newText)
        {
            var task = GetTask(itemNumber);

            var replacement = task.WithReplacementText(oldText, newText);

            if (task == replacement)
            {
                // Nothing changed, so no reason to update the list
                return false;
            }

            _tasks[itemNumber - 1] = new NumberedTask(itemNumber, replacement, Format);

            return true;
        }

        public Task GetTask(int itemNumber)
        {
            return _tasks[itemNumber - 1].Task;
        }

        public void AppendToTask(int itemNumber, string newText)
        {
            var task = GetTask(itemNumber);
            _tasks[itemNumber - 1] = new NumberedTask(itemNumber, task.WithBody(task.Body + newText), Format);
        }

        public void PrependToTask(int itemNumber, string newText)
        {
            var task = GetTask(itemNumber);
            _tasks[itemNumber - 1] = new NumberedTask(itemNumber, task.WithBody(newText + task.Body), Format);
        }

        public void ReplaceTask(int itemNumber, string newTask, bool ensureCreatedDate = false)
        {
            _tasks[itemNumber - 1] = Create(newTask, itemNumber, ensureCreatedDate); 
        }

        public bool RemoveFromTask(int item, string term)
        {
            return ReplaceItemText(item, term, string.Empty);
        }

        public IEnumerable<NumberedTask> RemoveCompletedTasks(bool preserveLineNumbers)
        {
            // TODO the Format method there are attached to may be incorrect if the total number
            // of tasks drops below the threshold to have the same digits (e.g., from 101 to 99)
            // We can fix this by creating a custom local format method and copying these to 
            // a new List using the custom format method
            IEnumerable<NumberedTask> completed = [.. ListCompleted()];

            for (int n = Count - 1; n >= 0; n--)
            {
                if (_tasks[n].Completed)
                {
                    if (preserveLineNumbers)
                    {
                        _tasks[n] = new NumberedTask(n, Task.Empty, Format);
                    }
                    else
                    {
                        _tasks.Remove(_tasks[n]);
                    }
                }
            }

            return completed;
        }

        public void RemoveTask(int itemNumber, bool preserveLineNumbers = false)
        {
            if (preserveLineNumbers)
            {
                _tasks[itemNumber - 1] = new NumberedTask(itemNumber, Task.Empty, Format);
            }
            else
            {
                _tasks.RemoveAt(itemNumber - 1);
                RenumberFrom(itemNumber - 1);
            }
        }

        private void RenumberFrom(int index)
        {
            for (int n = index; n < Count; n++)
            {
                var old = _tasks[index];
                _tasks[index] = new NumberedTask(old.Number - 1, old.Task, Format);
            }
        }

        public void LoadTasks(Stream fileStream)
        {
            try
            {
                _tasks.Clear();

                var lines = new List<string>();

                using (var sr = new StreamReader(fileStream))
                {
                    while (!sr.EndOfStream)
                    {
                        lines.Add(sr.ReadLine());
                    }
                }

                foreach (string line in lines)
                {
                    Add(line);
                }
            }
            catch (IOException ex)
            {
                throw new TaskException("There was a problem trying to read from your file", ex);
            }
        }

        public void LoadTasks(string filePath)
        {
            try
            {
                _tasks.Clear();

                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    Add(line);
                }
            }
            catch (IOException ex)
            {
                throw new TaskException("There was a problem trying to read from your file", ex);
            }
        }

        public void WriteTasks(Stream stream)
        {
            try
            {
                using var sw = new StreamWriter(stream);
                foreach (var item in _tasks)
                {
                    sw.WriteLine(item.ToString());
                }

                sw.Flush();

            }
            catch (IOException ex)
            {
                throw new TaskException("There was a problem trying to write your tasks to the stream", ex);
            }
        }

        public void Save(string filePath)
        {
            try
            {
                File.WriteAllLines(filePath, [.. _tasks.Select(numberedTask => numberedTask.Task.ToString())]);
            }
            catch (IOException ex)
            {
                throw new TaskException("There was a problem trying to save your file", ex);
            }
        }

        public NumberedTask Create(string task, bool ensureCreatedDate = false)
        {
            var newTask = Create(task, Count + 1, ensureCreatedDate);

            _tasks.Add(newTask);

            return newTask;
        }

        private NumberedTask Create(string task, int number, bool ensureCreatedDate = false)
        { 
            var toAdd = Task.Parse(task);

            if (ensureCreatedDate && toAdd.CreatedDate is null)
            {
                toAdd = toAdd.WithCreatedDate();
            }

            return new NumberedTask(number, toAdd, Format);
        }

        public void Add(string task)
        {
            _tasks.Add(new NumberedTask(Count + 1, Task.Parse(task), Format));
        }

        public IEnumerator<NumberedTask> GetEnumerator()
        {
            return ((IEnumerable<NumberedTask>)_tasks).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_tasks).GetEnumerator();
        }

        internal void Add(NumberedTask task)
        {
            _tasks.Add(task);
        }

        public bool ItemExists(int itemNumber)
        {
            return itemNumber <= Count;
        }
    }
}
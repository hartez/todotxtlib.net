using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace todotxtlib.net
{
    public class TaskList : List<NumberedTask>, IOutputTasks
    {
        public static TaskList Merge(TaskList original, TaskList new1, TaskList new2)
        {
            var diff = new DiffMatchPatch.diff_match_patch();
            var diffs = diff.diff_main(original.ToString(), new1.ToString());

            var patches = diff.patch_make(original.ToString(), diffs);

            var text = diff.patch_apply(patches, new2.ToString());

            var result = new TaskList();
            result.LoadTasksFromString((string)text[0]);

            return result;
        }

        private string NumberFormat => new('0', Count.ToString().Length);

        public TaskList()
        {
        }

        public TaskList(string filePath)
        {
            LoadTasks(filePath);
        }

        public override string ToString()
        {
            return this.Aggregate(string.Empty, (s, task) => s + (s.Length == 0 ? string.Empty : Environment.NewLine) + task.ToString());
        }

        public IEnumerable<string> ToOutput()
        {
            return this.Select(x => x.ToString());
        }

        public IEnumerable<string> ToNumberedOutput()
        {
            var numberFormat = NumberFormat;

            foreach (var task in this)
            {
                yield return task.ToNumberedString(numberFormat);
            }
        }

        public TaskListView ListCompleted()
        {
            return new TaskListView(from numberedTask in this
                                    where numberedTask.Completed
                                    select numberedTask);
        }

        public TaskListView Search(string term)
        {
            bool include = true;

            if (term.StartsWith('-'))
            {
                include = false;
                term = term[1..];
            }

            return new TaskListView(from numberedTask in this
                                    where !(include ^ numberedTask.Task.Body.Contains(term, StringComparison.OrdinalIgnoreCase))
                                    select numberedTask);
        }

        public TaskListView GetPriority(char? priority)
        {
            if (priority == null)
            {
                return new TaskListView(from numberedTask in this
                                        where numberedTask.IsPriority
                                        orderby numberedTask.Priority
                                        select numberedTask);
            }

            return new TaskListView(from numberedTask in this
                                    where numberedTask.Priority == priority
                                    select numberedTask);
        }

        public void SetItemPriority(int itemNumber, char priority)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);
            this[index] = new NumberedTask(itemNumber, task.WithPriority(priority));
        }

        public void MarkCompleted(int itemNumber)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);
            if (task.Completed)
            {
                return;
            }

            this[index] = new NumberedTask(itemNumber, task.WithCompleted());
        }

        public void MarkPending(int itemNumber)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);
            if (!task.Completed)
            {
                return;
            }

            this[index] = new NumberedTask(itemNumber, task.WithPending());
        }

        public void ToggleCompleted(int itemNumber)
        {
            var index = itemNumber - 1;
            var task = GetTask(itemNumber);

            this[index] = new NumberedTask(itemNumber, task.Completed ? task.WithPending() : task.WithCompleted());
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

            this[itemNumber - 1] = new NumberedTask(itemNumber, replacement);

            return true;
        }

        public Task GetTask(int itemNumber)
        {
            return this[itemNumber - 1].Task;
        }

        public void AppendToTask(int itemNumber, string newText)
        {
            var task = GetTask(itemNumber);
            this[itemNumber - 1] = new NumberedTask(itemNumber, task.WithBody(task.Body + newText));
        }

        public void PrependToTask(int itemNumber, string newText)
        {
            var task = GetTask(itemNumber);
            this[itemNumber - 1] = new NumberedTask(itemNumber, task.WithBody(newText + task.Body));
        }

        public bool RemoveFromTask(int item, string term)
        {
            return ReplaceItemText(item, term, string.Empty);
        }

        public TaskListView RemoveCompletedTasks(bool preserveLineNumbers)
        {
            TaskListView completed = ListCompleted();

            for (int n = Count - 1; n >= 0; n--)
            {
                if (this[n].Completed)
                {
                    if (preserveLineNumbers)
                    {
                        this[n] = new NumberedTask(n, Task.Empty);
                    }
                    else
                    {
                        Remove(this[n]);
                    }
                }
            }

            return completed;
        }

        public void RemoveTask(int itemNumber, bool preserveLineNumbers = false)
        {
            if (preserveLineNumbers)
            {
                this[itemNumber - 1] = new NumberedTask(itemNumber, Task.Empty);
            }
            else
            {
                RemoveAt(itemNumber - 1);
                RenumberFrom(itemNumber - 1);
            }
        }

        private void RenumberFrom(int index)
        {
            for (int n = index; n < Count; n++)
            {
                var old = this[index];
                this[index] = new NumberedTask(old.Number - 1, old.Task);
            }
        }

        public void LoadTasksFromString(string text)
        {
            using var sr = new StringReader(text);
            var line = sr.ReadLine();
            while (line != null)
            {
                Add(line);
                line = sr.ReadLine();
            }
        }

        public void LoadTasks(Stream fileStream)
        {
            try
            {
                Clear();

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
                Clear();

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
                foreach (var item in this)
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

        public void SaveTasks(FileStream fileStream)
        {
            try
            {
                using var sw = new StreamWriter(fileStream);
                foreach (var item in this)
                {
                    sw.WriteLine(item.ToString());
                }

                sw.Flush();

            }
            catch (IOException ex)
            {
                throw new TaskException("There was a problem trying to save your file", ex);
            }
        }

        public void SaveTasks(string filePath)
        {
            try
            {
                File.WriteAllLines(filePath, [.. this.Select(t => t.ToString())]);
            }
            catch (IOException ex)
            {
                throw new TaskException("There was a problem trying to save your file", ex);
            }
        }

        public void Add(string task)
        {
            Add(new NumberedTask(Count + 1, Task.Parse(task)));
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace todotxtlib.net
{
    public class TaskList : List<Task>
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

        private string _numberFormat;

        public TaskList()
        {
        }

        public TaskList(string filePath)
        {
            LoadTasks(filePath);
        }

        public TaskList(IEnumerable<Task> todos, int parentListItemCount)
        {
            _numberFormat = new string('0', parentListItemCount.ToString().Length);
            foreach (var todo in todos)
            {
                Add(todo);
            }
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
            if (string.IsNullOrEmpty(_numberFormat))
            {
                _numberFormat = new string('0', Count.ToString().Length);
            }

            for (int n = 0; n < Count; n++)
            {
                yield return $"{n.ToString(_numberFormat)} {this[n]}";
            }
        }

        public TaskList ListCompleted()
        {
            return new TaskList(from todo in this
                                where todo.Completed
                                select todo, Count);
        }

        public TaskList Search(string term)
        {
            bool include = true;

            if (term.StartsWith("-"))
            {
                include = false;
                term = term[1..];
            }

            return new TaskList(from task in this
                                where !(include ^ task.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                                select task, Count);
        }

        public TaskList GetPriority(char? priority)
        {
            if (priority == null)
            {
                return new TaskList(from todo in this
                                    where todo.IsPriority
                                    orderby todo.Priority
                                    select todo, Count);
            }

            return new TaskList(from todo in this
                                where todo.Priority == priority
                                select todo, Count);
        }

        public void SetItemPriority(int index, char priority)
        {
            this[index] = this[index].WithPriority(priority);
        }

        public void MarkCompleted(int index)
        {
            var task = GetTask(index);
            if (task == null || task.Completed)
            {
                return;
            }

            this[index] = task.WithCompleted();
        }

        public void MarkPending(int index)
        {
            var task = GetTask(index);
            if (task == null || task.Completed)
            {
                return;
            }

            this[index] = task.WithPending();
        }

        public void ToggleCompleted(int index)
        {
            var task = GetTask(index);
            if (task == null)
            {
                return;
            }

            this[index] = task.Completed ? task.WithPending() : task.WithCompleted();
        }

        private bool ReplaceItemText(int index, string oldText, string newText)
        {
            var target = GetTask(index);

            var replacement = target.WithReplacementText(oldText, newText);

            if (target == replacement)
            {
                return false;
            }

            this[index] = replacement;

            return true;
        }

        public Task GetTask(int index)
        {
            if (index >= Count) { return null; }

            return this[index];
        }

        public void AppendToTask(int index, string newText)
        {
            var current = GetTask(index);

            if (current == null) { return; }

            this[index] = current.WithBody(current.Body + newText);
        }

        public void PrependToTask(int index, string newText)
        {
            var current = GetTask(index);

            if (current == null) { return; }

            this[index] = current.WithBody(newText + current.Body);
        }

        public bool RemoveFromTask(int item, string term)
        {
            return ReplaceItemText(item, term, string.Empty);
        }

        public TaskList RemoveCompletedTasks(bool preserveLineNumbers)
        {
            TaskList completed = ListCompleted();

            for (int n = Count - 1; n >= 0; n--)
            {
                if (this[n].Completed)
                {
                    if (preserveLineNumbers)
                    {
                        this[n] = Task.Empty;
                    }
                    else
                    {
                        Remove(this[n]);
                    }
                }
            }

            return completed;
        }

        public void RemoveTask(int index, bool preserveLineNumbers)
        {
            Task target = GetTask(index);

            if (target != null)
            {
                if (preserveLineNumbers)
                {
                    this[index] = Task.Empty;
                }
                else
                {
                    Remove(target);
                }
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

        /// <summary>
        /// Deletes a task from this list
        /// </summary>
        /// <param name="task">The task to delete from the list</param>
        /// <returns>True if the task was in the list; false otherwise</returns>
        public bool Delete(Task task)
        {
            try
            {
                return (Remove(this.First(t => t == task)));
            }
            catch (Exception ex)
            {
                throw new TaskException("An error occurred while trying to remove your task from the task list file", ex);
            }
        }

        public void Add(string task)
        {
            this.Add(Task.Parse(task));
        }
    }
}
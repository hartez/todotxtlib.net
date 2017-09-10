using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace todotxtlib.net
{
	public class TaskList : ObservableCollection<Task>
	{
	    public static TaskList Merge(TaskList original, TaskList new1, TaskList new2)
	    {
            var diff = new DiffMatchPatch.diff_match_patch();
            var diffs = diff.diff_main(original.ToString(), new1.ToString());

            var patches = diff.patch_make(original.ToString(), diffs);

            var text = diff.patch_apply(patches, new2.ToString());

	        var result = new TaskList();
            result.LoadTasksFromString((String)text[0]);

            return result;
	    }

	    private String _numberFormat;
        
		public TaskList()
		{
		}

		public TaskList(string filePath)
		{
			LoadTasks(filePath);
		}

		public TaskList(IEnumerable<Task> todos, int parentListItemCount)
		{
			_numberFormat = new String('0', parentListItemCount.ToString().Length);
			foreach (var todo in todos)
			{
				Add(todo);
			}
		}

        public override string ToString()
        {
            return this.Aggregate(String.Empty, (s, task) => s + (s.Length == 0 ? String.Empty : Environment.NewLine) + task.ToString());
        }

		public IEnumerable<String> ToOutput()
		{
			return this.Select(x => x.ToString());
		}

		public IEnumerable<String> ToNumberedOutput()
		{
			if (String.IsNullOrEmpty(_numberFormat))
			{
				_numberFormat = new String('0', Count.ToString().Length);
			}

			return this.Select(x => x.ToString(_numberFormat));
		}

		public TaskList ListCompleted()
		{
			return new TaskList(from todo in this
			                    where todo.Completed
			                    select todo, Count);
		}

		public TaskList Search(String term)
		{
			bool include = true;

			if (term.StartsWith("-"))
			{
				include = false;
				term = term.Substring(1);
			}

			return new TaskList(from task in this
			                    where !(include ^ task.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
			                    select task, Count);
		}

		public TaskList GetPriority(String priority)
		{
			if (!String.IsNullOrEmpty(priority))
			{
				return new TaskList(from todo in this
				                    where todo.Priority == priority
				                    select todo, Count);
			}

			return new TaskList(from todo in this
			                    where todo.IsPriority
			                    orderby todo.Priority
			                    select todo, Count);
		}

		public void SetItemPriority(int item, string priority)
		{
			Task target = (from todo in this
			               where todo.ItemNumber == item
			               select todo).FirstOrDefault();

			if (target != null)
			{
				target.Priority = priority;
			}
		}

		private bool ReplaceItemText(int item, string oldText, string newText)
		{
			Task target = (from todo in this
			               where todo.ItemNumber == item
			               select todo).FirstOrDefault();

			if (target != null)
			{
				return target.ReplaceItemText(oldText, newText);
			}

			return false;
		}

		public void ReplaceInTask(int item, string newText)
		{
			Task target = (from todo in this
			               where todo.ItemNumber == item
			               select todo).FirstOrDefault();

			target?.Replace(newText);
		}

		public void AppendToTask(int item, string newText)
		{
			Task target = (from todo in this
			               where todo.ItemNumber == item
			               select todo).FirstOrDefault();

			target?.Append(newText);
		}

		public void PrependToTask(int item, string newText)
		{
			Task target = (from todo in this
			               where todo.ItemNumber == item
			               select todo).FirstOrDefault();

			target?.Prepend(newText);
		}

		public bool RemoveFromTask(int item, string term)
		{
			return ReplaceItemText(item, term, String.Empty);
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
						this[n].Empty();
					}
					else
					{
						Remove(this[n]);
					}
				}
			}

			return completed;
		}

		public void RemoveTask(int item, bool preserveLineNumbers)
		{
			Task target = (from todo in this
			               where todo.ItemNumber == item
			               select todo).FirstOrDefault();

			if (target != null)
			{
				if (preserveLineNumbers)
				{
					target.Empty();
				}
				else
				{
					Remove(target);

					int itemNumber = 1;
					foreach (Task todo in this)
					{
						todo.ItemNumber = itemNumber;
						itemNumber += 1;
					}
				}
			}
		}

	    public void LoadTasksFromString(String text)
	    {
	        using(var sr = new StringReader(text))
	        {
	            var line = sr.ReadLine();
	            while(line != null)
	            {
                    Add(new Task(line));
                    line = sr.ReadLine();
	            }
	        }
	    }

	    public void LoadTasks(Stream fileStream)
		{
			try
			{
				Clear();

				var lines = new List<string>();

				using(var sr = new StreamReader(fileStream))
				{
                    while (!sr.EndOfStream)
                    {
                        lines.Add(sr.ReadLine());
                    }
				}

				foreach (string line in lines)
				{
					Add(new Task(line));
				}
			}
			catch (IOException ex)
			{
				throw new TaskException("There was a problem trying to read from your file", ex);
			}
		}

		public void LoadTasks(String filePath)
		{
			try
			{
				Clear();

				string[] lines = ReadAllLines(filePath);

				foreach (string line in lines)
				{
				    Add(new Task(line));
				}
			}
			catch (IOException ex)
			{
				throw new TaskException("There was a problem trying to read from your file", ex);
			}
		}

		public void SaveTasks(FileStream fileStream)
		{
			try
			{
				using (var sw = new StreamWriter(fileStream))
				{
					foreach (var item in Items)
					{
						sw.WriteLine(item.ToString());
					}

					sw.Flush();
				}

			}
			catch (IOException ex)
			{
				throw new TaskException("There was a problem trying to save your file", ex);
			}
		}

		public void SaveTasks(String filePath)
		{
			try
			{
				WriteAllLines(filePath, this.Select(t => t.ToString()).ToArray());
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
				return (Remove(this.First(t => t.Raw == task.Raw)));
			}
			catch (Exception ex)
			{
				throw new TaskException("An error occurred while trying to remove your task from the task list file", ex);
			}
		}

		public void Update(Task currentTask, Task newTask)
		{
			try
			{
				int currentIndex = IndexOf(this.First(t => t.Raw == currentTask.Raw));

				this[currentIndex] = newTask;
			}
			catch (Exception ex)
			{
				throw new TaskException("An error occurred while trying to update your task in the task list file", ex);
			}
		}

        // WriteAllLines and ReadAllLines are included here to support Windows Phone
        // They're available by default in other versions of the .NET framework
		public static void WriteAllLines(string path, string[] lines)
		{
			using (var fs = File.Open(path, FileMode.Create, FileAccess.Write))
			{
				using (var sw = new StreamWriter(fs))
				{
					foreach (string line in lines)
					{
						sw.WriteLine(line);
					}

					sw.Flush();
				}
			}
		}

		public static string[] ReadAllLines(string path)
		{
			var lines = new List<string>();

			using (var fs = File.OpenRead(path))
			{
				using (var sr = new StreamReader(fs))
				{
                    while(!sr.EndOfStream)
                    {
                        lines.Add(sr.ReadLine());
                    }
				}
			}
			return lines.ToArray();
		}
	}
}
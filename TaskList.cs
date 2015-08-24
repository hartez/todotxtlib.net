using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

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
			result.LoadTasksFromString(text[0] as string);
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
			_numberFormat = new String('0', parentListItemCount.ToString().Length);
			foreach (var todo in todos)
				Add(todo);
		}

		public override string ToString()
		{
			return this.Aggregate(String.Empty, (s, task) => s + (s.Length == 0 ? String.Empty : Environment.NewLine) + task.ToString());
		}

		public IEnumerable<string> ToOutput()
		{
			return this.Select(x => x.ToString());
		}

		public IEnumerable<string> ToNumberedOutput()
		{
			if (String.IsNullOrEmpty(_numberFormat))
				_numberFormat = new String('0', Count.ToString().Length);

			return this.Select(x => x.ToString(_numberFormat));
		}

		public TaskList ListCompleted()
		{
			return new TaskList(this.Where(todo => todo.Completed), Count);
		}

		public TaskList Search(string term)
		{
			var include = true;

			if (!term.StartsWith("-"))
				return new TaskList(this.Where(task => !(include ^ task.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))), Count);
			include = false;
			term = term.Substring(1);

			return new TaskList(this.Where(task => !(include ^ task.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))), Count);
		}

		public TaskList GetPriority(string priority)
		{
			return new TaskList(!String.IsNullOrEmpty(priority)
				? this.Where(todo => todo.Priority == priority).OrderBy(todo => todo)
				: this.Where(todo => todo.Priority == priority).OrderBy(todo => todo.Priority), Count);
		}

		private readonly Func<TaskList, int, Task> _getTarget = (taskList, item) => taskList.FirstOrDefault(todo => todo.ItemNumber == item);

		public void SetItemPriority(int item, string priority)
		{
			var target = _getTarget(this, item);
			if (target != null) target.Priority = priority;
		}

		private bool ReplaceItemText(int item, string oldText, string newText)
		{
			var target = _getTarget(this, item);
			return target != null && target.ReplaceItemText(oldText, newText);
		}

		public void ReplaceInTask(int item, string newText)
		{
			var target = _getTarget(this, item);
			if (target != null) target.Replace(newText);
		}

		public void AppendToTask(int item, string newText)
		{
			var target = _getTarget(this, item);
			if (target != null) target.Append(newText);
		}

		public void PrependToTask(int item, string newText)
		{
			var target = _getTarget(this, item);
			if (target != null) target.Prepend(newText);
		}

		public bool RemoveFromTask(int item, string term)
		{
			return ReplaceItemText(item, term, String.Empty);
		}

		public TaskList RemoveCompletedTasks(bool preserveLineNumbers)
		{
			var completed = ListCompleted();
			for (var n = Count - 1; n >= 0; n--)
			{
				if (!this[n].Completed) continue;
				if (preserveLineNumbers)
					this[n].Empty();
				else
					Remove(this[n]);
			}
			return completed;
		}

		public void RemoveTask(int item, bool preserveLineNumbers)
		{
			var target = _getTarget(this, item);
			if (target == null) return;
			if (preserveLineNumbers) target.Empty();
			else
			{
				Remove(target);

				for (var i = 0; i < Count; i++)
					this[i].ItemNumber = i + 1;

				//var itemNumber = 1;
				//foreach (var todo in this)
				//{
				//	todo.ItemNumber = itemNumber;
				//	itemNumber++;
				//}
			}
		}

		public void LoadTasksFromString(string text)
		{
			using (var sr = new StringReader(text))
			{
				string line;
				while (!String.IsNullOrEmpty(line = sr.ReadLine()))
					Add(new Task(line));
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
						lines.Add(sr.ReadLine());
				}

				foreach (var line in lines)
					Add(new Task(line));
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

				foreach (var line in ReadAllLines(filePath))
					Add(new Task(line));
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
						sw.WriteLine(item.ToString());
					sw.Flush();
				}

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
				this[IndexOf(this.First(t => t.Raw == currentTask.Raw))] = newTask;
			}
			catch (Exception ex)
			{
				throw new TaskException("An error occurred while trying to update your task int the task list file", ex);
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
					foreach (var line in lines)
						sw.WriteLine(line);
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
					while (!sr.EndOfStream)
						lines.Add(sr.ReadLine());
				}
			}
			return lines.ToArray();
		}
	}
}
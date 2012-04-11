using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace todotxtlib.net
{
	public class Task : INotifyPropertyChanged
	{
		private readonly Dictionary<string, string> _metadata = new Dictionary<string, string>();
		private string _body;
		private bool _completed;

		private DateTime? _completedDate;
		private List<String> _contexts = new List<String>();

		private DateTime? _createdDate;

		private int? _itemNumber;

		private string _priority = String.Empty;
		private List<String> _projects = new List<String>();
		private string _raw;

		public Task(String raw, int? itemNumber)
		{
			ItemNumber = itemNumber;

			Raw = raw.Replace(Environment.NewLine, ""); //make sure it's just on one line

			ParseFields(raw);
		}

		public Task(string raw)
			: this(raw, null)
		{
		}

		public Task(string priority, List<string> projects, List<string> contexts, string body)
			: this(priority, projects, contexts, body, null, "", false, null)
		{
		}

		public Task(string priority, List<string> projects, List<string> contexts,
		            string body, DateTime? createdDate, string dueDate, bool completed, DateTime? completedDate)
		{
			Priority = priority.Replace("(", String.Empty).Replace(")", String.Empty).ToUpperInvariant();

			if (projects != null)
			{
				_projects = projects;
			}

			if (contexts != null)
			{
				_contexts = contexts;
			}

			CreatedDate = createdDate;
			DueDate = dueDate;

			Body = body + (Contexts.Count() > 0 ? " " : String.Empty)
			       + String.Join(" ", _contexts.ToArray())
			       + (Projects.Count() > 0 ? " " : String.Empty)
			       + String.Join(" ", Projects.ToArray())
			       + (String.IsNullOrEmpty(dueDate) ? String.Empty : " due:" + dueDate);

			Completed = completed;
			CompletedDate = completedDate;

			Raw = (_completed ? "x " : String.Empty)
			      + (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
			      + (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
			      + Body;
		}

		public String Body
		{
			get { return _body; }
			set
			{
				if (_body == value)
				{
					return;
				}

				_body = value;
				InvokePropertyChanged(new PropertyChangedEventArgs("Body"));
				ParseFields(Body);
			}
		}

		public DateTime? CompletedDate
		{
			get { return _completedDate; }
			private set
			{
				if (_completedDate == value)
				{
					return;
				}

				_completedDate = value;
				InvokePropertyChanged(new PropertyChangedEventArgs("CompletedDate"));
			}
		}

		public DateTime? CreatedDate
		{
			get { return _createdDate; }
			private set
			{
				if (_createdDate == value)
				{
					return;
				}

				_createdDate = value;
				InvokePropertyChanged(new PropertyChangedEventArgs("CreatedDate"));
			}
		}

		public int? ItemNumber
		{
			get { return _itemNumber; }
			set
			{
				_itemNumber = value;
				InvokePropertyChanged(new PropertyChangedEventArgs("ItemNumber"));
			}
		}

		public IDictionary<string, string> Metadata
		{
			get { return _metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
		}

		public IEnumerable<String> Projects
		{
			get { return _projects.Select(p => p); }
		}

		public IEnumerable<String> Contexts
		{
			get { return _contexts.Select(p => p); }
		}

		public String Priority
		{
			get { return _priority; }
			set
			{
				if (_priority == value
				    || (value == null && String.IsNullOrEmpty(_priority)))
				{
					return;
				}

				_priority = value != null ? value.ToUpperInvariant() : String.Empty;

				InvokePropertyChanged(new PropertyChangedEventArgs("Priority"));
			}
		}

		public string Raw
		{
			get { return _raw; }
			private set
			{
				_raw = value;
				InvokePropertyChanged(new PropertyChangedEventArgs("Raw"));
			}
		}

		public String DueDate
		{
			get
			{
				if (_metadata.ContainsKey("due"))
				{
					return _metadata["due"];
				}

				return String.Empty;
			}
			set
			{
				if (_metadata.ContainsKey("due"))
				{
					if (_metadata["due"] == value)
					{
						return;
					}

					_metadata["due"] = value;
				}
				else
				{
					_metadata.Add("due", value);
				}

				InvokePropertyChanged(new PropertyChangedEventArgs("DueDate"));
			}
		}

		public bool Completed
		{
			get { return _completed; }
			set
			{
				if (_completed == value)
				{
					return;
				}

				_completed = value;

				InvokePropertyChanged(new PropertyChangedEventArgs("Completed"));

				if (!_completed && IsPriority)
				{
					Priority = String.Empty;
				}

				if (_completed)
				{
					CompletedDate = DateTime.Now;
				}
			}
		}

		public bool IsPriority
		{
			get { return !String.IsNullOrEmpty(Priority); }
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		public void ToggleCompleted()
		{
			Completed = !Completed;
		}

		public void Empty()
		{
			Body = String.Empty;
			CreatedDate = null;
			Priority = String.Empty;
			_contexts = new List<String>();
			_projects = new List<String>();
		}

		private void ParseProjects(String todo)
		{
			_projects.Clear();

			MatchCollection projects = Regex.Matches(todo, @"\s(\+\w+)");

			foreach (Match match in projects)
			{
				String project = match.Groups[1].Captures[0].Value;
				_projects.Add(project);
			}

			InvokePropertyChanged(new PropertyChangedEventArgs("Projects"));
		}

		private void ParseContexts(string todo)
		{
			_contexts.Clear();

			MatchCollection contexts = Regex.Matches(todo, @"\s(@\w+)");

			foreach (Match match in contexts)
			{
				String context = match.Groups[1].Captures[0].Value;
				_contexts.Add(context);
			}

			InvokePropertyChanged(new PropertyChangedEventArgs("Contexts"));
		}

		private void ParseMetaData(string todo)
		{
			_metadata.Clear();

			MatchCollection metadata = Regex.Matches(todo, @"\s(?<meta>\w+:.\S*)");

			foreach (Match match in metadata)
			{
				String data = match.Groups[1].Captures[0].Value;
				string[] kvp = data.Split(':');

				AddToMetadata(kvp[0], kvp[1]);
			}

			RecognizePhoneNumbers(todo);

			InvokePropertyChanged(new PropertyChangedEventArgs("Metadata"));
		}

		private void AddToMetadata(string key, string value)
		{
			if (_metadata.Keys.Contains(key))
			{
				key = key + _metadata.Keys.Count(k => k == key).ToString(CultureInfo.InvariantCulture);
			}

			_metadata.Add(key, value);
		}

		private void RecognizePhoneNumbers(string todo)
		{
			var phoneRegex = new Regex(@"(?<!phone:)(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?");

			MatchCollection phoneNumbers = phoneRegex.Matches(todo);

			foreach (Match match in phoneNumbers)
			{
				AddToMetadata("phone", match.Value);
			}
		}

		private void ParseEverythingElse(String todo)
		{
			Match everythingElse = Regex.Match(todo,
			                                   @"(?:(?<done>[xX] (?:(?<completeddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )))?(?:\((?<priority>[A-Z])\) )?(?:(?<createddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$");

			if (everythingElse != Match.Empty)
			{
				if (everythingElse.Groups["createddate"].Success)
				{
					CreatedDate = DateTime.Parse(everythingElse.Groups["createddate"].Value);
				}

				if (everythingElse.Groups["completeddate"].Success)
				{
					CompletedDate = DateTime.Parse(everythingElse.Groups["completeddate"].Value);
				}

				if (everythingElse.Groups["priority"].Success)
				{
					Priority = everythingElse.Groups["priority"].Value;
				}

				if (everythingElse.Groups["todo"].Success)
				{
					_body = everythingElse.Groups["todo"].Value;
				}

				if (everythingElse.Groups["done"].Success)
				{
					_completed = true;
				}
			}
		}

		private void ParseFields(String todo)
		{
			ParseContexts(todo);
			ParseProjects(todo);
			ParseMetaData(todo);

			todo = todo.Trim();

			ParseEverythingElse(todo);
		}

		public void Replace(String newTodo)
		{
			ParseFields(newTodo);
		}

		public void Append(String toAppend)
		{
			ParseFields(Body + toAppend);
		}

		public void Prepend(String toPrepend)
		{
			ParseFields(toPrepend + Body);
		}

		public bool ReplaceItemText(string oldText, string newText)
		{
			if (Body.Contains(oldText))
			{
				Body = Body.Replace(oldText, newText);
				ParseFields(Body);
				return true;
			}

			return false;
		}

		public String ToString(String numberFormat)
		{
			if (ItemNumber.HasValue)
			{
				return ItemNumber.Value.ToString(numberFormat) + " " + ToString();
			}

			return ToString();
		}

		public override String ToString()
		{
			return
				(_completed ? "x " : String.Empty)
				+ (_completed && CompletedDate.HasValue ? (CompletedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
				+ (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
				+ (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
				+ Body;
		}

		public void InvokePropertyChanged(PropertyChangedEventArgs e)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}
	}
}
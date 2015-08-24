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
		private List<string> _contexts = new List<string>();
		private DateTime? _createdDate;
		private int? _itemNumber;
		private string _priority = String.Empty;
		private List<string> _projects = new List<string>();
		private string _raw;

		public Task(string raw, int? itemNumber)
		{
			ItemNumber = itemNumber;

			Raw = raw.Replace(Environment.NewLine, String.Empty); //make sure it's just on one line

			ParseFields(raw);
		}

		public Task(string raw) : this(raw, null) { }

		public Task(string priority, List<string> projects, List<string> contexts, string body)
			: this(priority, projects, contexts, body, null, String.Empty, false, null) { }

		public Task(string priority, List<string> projects, List<string> contexts,
			    string body, DateTime? createdDate, string dueDate, bool completed, DateTime? completedDate)
		{
			Priority = priority.Replace("(", String.Empty).Replace(")", String.Empty).ToUpperInvariant();

			_projects = projects ?? new List<string>();
			_contexts = contexts ?? new List<string>();

			CreatedDate = createdDate;
			DueDate = dueDate;

			Body = body + (Contexts.Any() ? " " : String.Empty)
			       + String.Join(" ", _contexts.ToArray())
			       + (Projects.Any() ? " " : String.Empty)
			       + String.Join(" ", Projects.ToArray())
			       + (String.IsNullOrEmpty(dueDate) ? String.Empty : " due:" + dueDate);

			Completed = completed;
			CompletedDate = completedDate;

			Raw = (_completed ? "x " : String.Empty)
			      + (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
			      + (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
			      + Body;
		}

		public string Body
		{
			get { return _body; }
			set
			{
				if (_body == value) return;
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
				if (_completedDate == value) return;
				_completedDate = value;
				InvokePropertyChanged(new PropertyChangedEventArgs("CompletedDate"));
			}
		}

		public DateTime? CreatedDate
		{
			get { return _createdDate; }
			private set
			{
				if (_createdDate == value) return;
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

		public IEnumerable<string> Projects
		{
			get { return _projects.Select(p => p); }
		}

		public IEnumerable<string> Contexts
		{
			get { return _contexts.Select(c => c); }
		}

		public string Priority
		{
			get { return _priority; }
			set
			{
				if (_priority == value || (value == null && String.IsNullOrEmpty(_priority))) return;
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

		public string DueDate
		{
			get
			{
				return _metadata.ContainsKey("due") ? _metadata["due"] : String.Empty;
			}
			set
			{
				if (_metadata.ContainsKey("due"))
				{
					if (_metadata["due"] == value) return;
					_metadata["due"] = value;
				}
				else
					_metadata.Add("due", value);
				InvokePropertyChanged(new PropertyChangedEventArgs("DueDate"));
			}
		}

		public bool Completed
		{
			get { return _completed; }
			set
			{
				if (_completed == value) return;
				_completed = value;
				InvokePropertyChanged(new PropertyChangedEventArgs("Completed"));
				if (!_completed && IsPriority) Priority = String.Empty;
				if (_completed) CompletedDate = DateTime.Now;
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
			_contexts = new List<string>();
			_projects = new List<string>();
		}

		private void ParseProjects(String todo)
		{
			_projects.Clear();

			var projects = Regex.Matches(todo, @"\s(\+\S*\w)");
			_projects.AddRange(projects.Cast<Match>().Select(match => match.Groups[1].Captures[0].Value));

			InvokePropertyChanged(new PropertyChangedEventArgs("Projects"));
		}

		private void ParseContexts(string todo)
		{
			_contexts.Clear();

			var contexts = Regex.Matches(todo, @"\s(@\S*\w)");
			_contexts.AddRange(contexts.Cast<Match>().Select(match => match.Groups[1].Captures[0].Value));

			InvokePropertyChanged(new PropertyChangedEventArgs("Contexts"));
		}

		private void ParseMetaData(string todo)
		{
			_metadata.Clear();

			var metadata = Regex.Matches(todo, @"(?:^|\s)(?<meta>\w+:[^\s]+\S*)");
			foreach (var keyValuePair in from Match match in metadata
						     select match.Groups[1].Captures[0].Value
							     into data
							     select data.Split(':'))
				AddToMetadata(keyValuePair[0], keyValuePair[1]);

			RecognizePhoneNumbers(todo);
			InvokePropertyChanged(new PropertyChangedEventArgs("Metadata"));
		}

		private void AddToMetadata(string key, string value)
		{
			if (_metadata.Keys.Contains(key))
				key += _metadata.Keys.Count(currentKey => Regex.IsMatch(currentKey, "^" + key + "[0-9]*$", RegexOptions.CultureInvariant));

			_metadata.Add(key, value);
		}

		private void RecognizePhoneNumbers(string todo)
		{
			var phoneRegex = new Regex(@"(?<!phone:)(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?");

			var phoneNumbers = phoneRegex.Matches(todo);
			foreach (Match match in phoneNumbers)
				AddToMetadata("phone", match.Value);
		}

		private void ParseEverythingElse(String todo)
		{
			var everythingElse = Regex.Match(todo,
							   @"(?:(?<done>[xX] (?:(?<completeddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )))?(?:\((?<priority>[A-Z])\) )?(?:(?<createddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$");

			if (everythingElse == Match.Empty) return;
			if (everythingElse.Groups["createddate"].Success)
				CreatedDate = DateTime.Parse(everythingElse.Groups["createddate"].Value);

			if (everythingElse.Groups["completeddate"].Success)
				CompletedDate = DateTime.Parse(everythingElse.Groups["completeddate"].Value);

			if (everythingElse.Groups["priority"].Success)
				Priority = everythingElse.Groups["priority"].Value;

			if (everythingElse.Groups["todo"].Success)
				_body = everythingElse.Groups["todo"].Value;

			if (everythingElse.Groups["done"].Success)
				_completed = true;
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
			if (!Body.Contains(oldText)) return false;
			Body = Body.Replace(oldText, newText);
			ParseFields(Body);
			return true;
		}

		public string ToString(string numberFormat)
		{
			if (ItemNumber.HasValue)
				return ItemNumber.Value.ToString(numberFormat) + " " + ToString();

			return ToString();
		}

		public override string ToString()
		{
			return (_completed ? "x " : String.Empty)
				+ (_completed && CompletedDate.HasValue ? (CompletedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
				+ (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
				+ (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
				+ Body;
		}

		public void InvokePropertyChanged(PropertyChangedEventArgs e)
		{
			var handler = PropertyChanged;
			if (handler != null) handler(this, e);
		}
	}
}
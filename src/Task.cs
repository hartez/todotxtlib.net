using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace todotxtlib.net
{
    public partial class Task : INotifyPropertyChanged
    {
        private readonly Dictionary<string, string> _metadata = [];
        private string _body;
        private bool _completed;

        private DateTime? _completedDate;
        private List<string> _contexts = [];

        private DateTime? _createdDate;

        private int? _itemNumber;

        private string _priority = string.Empty;
        private List<string> _projects = [];
        private string _raw;

        [GeneratedRegex(@"\s(\+\S*\w)")]
        private static partial Regex ProjectsRegex();

        [GeneratedRegex(@"\s(@\S*\w)")]
        private static partial Regex ContextsRegex();

        [GeneratedRegex(@"(?:(?<done>[xX] (?:(?<completeddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )))?(?:\((?<priority>[A-Z])\) )?(?:(?<createddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$")]
        private static partial Regex EverythingElseRegex();

        [GeneratedRegex(@"(?<!phone:)(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?")]
        private static partial Regex PhoneNumberRegex();

        [GeneratedRegex(@"(?:^|\s)(?<meta>\w+:[^\s]+\S*)")]
        private static partial Regex MetaDataRegex();

        public Task(string raw, int? itemNumber)
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
            Priority = priority.Replace("(", string.Empty).Replace(")", string.Empty).ToUpperInvariant();

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

            Body = body + (Contexts.Any() ? " " : string.Empty)
                   + string.Join(" ", Contexts)
                   + (Projects.Any() ? " " : string.Empty)
                   + string.Join(" ", Projects)
                   + (string.IsNullOrEmpty(dueDate) ? string.Empty : " due:" + dueDate);

            Completed = completed;
            CompletedDate = completedDate;

            Raw = (_completed ? "x " : string.Empty)
                  + (!string.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : string.Empty)
                  + (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : string.Empty)
                  + Body;
        }

        public string Body
        {
            get => _body;
            set
            {
                if (_body == value)
                {
                    return;
                }

                _body = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Body)));
                ParseFields(Body);
            }
        }

        public DateTime? CompletedDate
        {
            get => _completedDate;
            private set
            {
                if (_completedDate == value)
                {
                    return;
                }

                _completedDate = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CompletedDate)));
            }
        }

        public DateTime? CreatedDate
        {
            get => _createdDate;
            private set
            {
                if (_createdDate == value)
                {
                    return;
                }

                _createdDate = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(CreatedDate)));
            }
        }

        public int? ItemNumber
        {
            get => _itemNumber;
            set
            {
                _itemNumber = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(ItemNumber)));
            }
        }

        public IDictionary<string, string> Metadata
        {
            get { return _metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value); }
        }

        public IEnumerable<string> Projects
        {
            get { return _projects; }
        }

        public IEnumerable<string> Contexts
        {
            get { return _contexts; }
        }

        public string Priority
        {
            get => _priority;
            set
            {
                if (_priority == value
                    || (value == null && string.IsNullOrEmpty(_priority)))
                {
                    return;
                }

                _priority = value != null ? value.ToUpperInvariant() : string.Empty;

                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Priority)));
            }
        }

        public string Raw
        {
            get => _raw;
            private set
            {
                _raw = value;
                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Raw)));
            }
        }

        public string DueDate
        {
            get
            {
                if (_metadata.TryGetValue("due", out var value))
                {
                    return value;
                }

                return string.Empty;
            }
            set
            {
                if (!_metadata.TryAdd("due", value))
                {
                    if (_metadata["due"] == value)
                    {
                        return;
                    }

                    _metadata["due"] = value;
                }

                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(DueDate)));
            }
        }

        public bool Completed
        {
            get => _completed;
            set
            {
                if (_completed == value)
                {
                    return;
                }

                _completed = value;

                InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Completed)));

                if (!_completed && IsPriority)
                {
                    Priority = string.Empty;
                }

                if (_completed)
                {
                    CompletedDate = DateTime.Now;
                }
            }
        }

        public bool IsPriority => !string.IsNullOrEmpty(Priority);

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        public void ToggleCompleted()
        {
            Completed = !Completed;
        }

        public void Empty()
        {
            Body = string.Empty;
            CreatedDate = null;
            Priority = string.Empty;
            _contexts = [];
            _projects = [];
        }

        private void ParseProjects(string todo)
        {
            _projects.Clear();

            var projects = ProjectsRegex().Matches(todo);

            foreach (Match match in projects)
            {
                string project = match.Groups[1].Captures[0].Value;
                _projects.Add(project);
            }

            InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Projects)));
        }

        private void ParseContexts(string todo)
        {
            _contexts.Clear();

            MatchCollection contexts = ContextsRegex().Matches(todo);

            foreach (Match match in contexts)
            {
                string context = match.Groups[1].Captures[0].Value;
                _contexts.Add(context);
            }

            InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Contexts)));
        }

        private void ParseMetaData(string todo)
        {
            _metadata.Clear();

            MatchCollection metadata = MetaDataRegex().Matches(todo);

            foreach (Match match in metadata)
            {
                string data = match.Groups[1].Captures[0].Value;
                string[] kvp = data.Split(':');

                AddToMetadata(kvp[0], kvp[1]);
            }

            RecognizePhoneNumbers(todo);

            InvokePropertyChanged(new PropertyChangedEventArgs(nameof(Metadata)));
        }

        private void AddToMetadata(string key, string value)
        {
            if (_metadata.ContainsKey(key))
            {
                var previous = _metadata.Keys.Count(currentKey => Regex.IsMatch(currentKey, "^" + key + "[0-9]*$", RegexOptions.CultureInvariant));

                key += previous;
            }

            _metadata.Add(key, value);
        }

        private void RecognizePhoneNumbers(string todo)
        {
            var phoneRegex = PhoneNumberRegex();

            MatchCollection phoneNumbers = phoneRegex.Matches(todo);

            foreach (Match match in phoneNumbers)
            {
                AddToMetadata("phone", match.Value);
            }
        }

        private void ParseEverythingElse(string todo)
        {
            Match everythingElse = EverythingElseRegex().Match(todo);

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

        private void ParseFields(string todo)
        {
            ParseContexts(todo);
            ParseProjects(todo);
            ParseMetaData(todo);

            todo = todo.Trim();

            ParseEverythingElse(todo);
        }

        public void Replace(string newTodo)
        {
            ParseFields(newTodo);
        }

        public void Append(string toAppend)
        {
            ParseFields(Body + toAppend);
        }

        public void Prepend(string toPrepend)
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

        public string ToString(string numberFormat)
        {
            if (ItemNumber.HasValue)
            {
                return $"{ItemNumber.Value.ToString(numberFormat)} {ToString()}";
            }

            return ToString();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (_completed)
            {
                stringBuilder.Append("x ");
            }

            if (_completed && CompletedDate.HasValue)
            {
                stringBuilder.Append($"{CompletedDate.Value.ToString("yyyy-MM-dd")} ");
            }

            if (!string.IsNullOrEmpty(Priority))
            {
                stringBuilder.Append($"({Priority}) ");
            }

            if (CreatedDate.HasValue)
            {
                stringBuilder.Append($"{CreatedDate.Value.ToString("yyyy-MM-dd")} ");
            }

            stringBuilder.Append(Body);

            return stringBuilder.ToString();
        }

        public void InvokePropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}
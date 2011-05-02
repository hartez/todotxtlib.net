using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace todotxtlib.net
{
    public class Task
    {
        private bool _completed;
        private string _priority = String.Empty;

        public int? ItemNumber;

        public String Priority
        {
            get { return _priority; }
            set { _priority = value.ToUpperInvariant(); }
        }

        public DateTime? CreatedDate;
        public DateTime? CompletedDate;
        public String Body;
        public List<String> Contexts = new List<String>();
        public List<String> Projects = new List<String>();
        
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();

        public string Raw { get; set; }

        public String DueDate
        {
            get
            {
                if(Metadata.ContainsKey("due"))
                {
                    return Metadata["due"];
                }

                return String.Empty;
            }
            set {
                if (Metadata.ContainsKey("due"))
                {
                    Metadata["due"] = value;
                }
                else
                {
                    Metadata.Add("due", value);
                }
            }
        }

        public bool Completed
        {
            get { return _completed; }
            private set { _completed = value; }
        }

        public void ToggleCompleted()
        {
            _completed = !_completed;

            if (!_completed && IsPriority)
            {
                Priority = String.Empty;
            }

            if (_completed)
            {
                CompletedDate = DateTime.Now;
            }
        }

        public void Empty()
        {
            Body = String.Empty;
            CreatedDate = null;
            Priority = String.Empty;
            Contexts = new List<String>();
            Projects = new List<String>();
        }

        private void ParseProjects(String todo)
        {
            Projects.Clear();

            MatchCollection projects = Regex.Matches(todo, @"\s(\+\w+)");

            foreach (Match match in projects)
            {
                String project = match.Groups[1].Captures[0].Value;
                Projects.Add(project);
            }
        }

        private void ParseContexts(string todo)
        {
            Contexts.Clear();

            MatchCollection contexts = Regex.Matches(todo, @"\s(@\w+)");

            foreach (Match match in contexts)
            {
                String context = match.Groups[1].Captures[0].Value;
                Contexts.Add(context);
            }
        }

        private void ParseMetaData(string todo)
        {
            Metadata.Clear();

            MatchCollection metadata = Regex.Matches(todo, @"\s(?<meta>\w+:.\S*)");

            foreach (Match match in metadata)
            {
                String data = match.Groups[1].Captures[0].Value;
                var kvp = data.Split(':');
                Metadata.Add(kvp[0], kvp[1]);
            }
        }

        private void ParseEverythingElse(String todo)
        {
            Match everythingElse = Regex.Match(todo, @"(?:(?<done>[xX]) )?(?:\((?<priority>[A-Z])\) )?(?:(?<createddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?:(?<completeddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$");

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
                    Body = everythingElse.Groups["todo"].Value;
                }

                if (everythingElse.Groups["done"].Success)
                {
                    _completed = true;
                }
            }
        }

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

        public Task(string priority, List<string> projects, List<string> contexts,
                    string body, DateTime? createdDate = null, string dueDate = "", bool completed = false)
        {
            Priority = priority.Replace("(", String.Empty).Replace(")", String.Empty).ToUpperInvariant();
           
            if (projects != null)
            {
                Projects = projects;
            }
            
            if (contexts != null)
            {
                Contexts = contexts;
            }
            
            CreatedDate = createdDate;
            DueDate = dueDate;

            Body = body + (Contexts.Count > 0 ? " " : String.Empty)
                   + String.Join(" ", Contexts.ToArray())
                   + (Projects.Count > 0 ? " " : String.Empty)
                   + String.Join(" ", Projects.ToArray())
                   + (String.IsNullOrEmpty(dueDate) ? String.Empty : " due:" + dueDate);

            Completed = completed;

            Raw = (_completed ? "x " : String.Empty)
                  + (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
                  + (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
                  + Body;
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

        public bool IsPriority
        {
            get { return !String.IsNullOrEmpty(Priority); }
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
                + (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
                + (_completed && CompletedDate.HasValue ? (CompletedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
                + (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
                + Body;
        }
    }
}
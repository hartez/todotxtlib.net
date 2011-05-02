using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace todotxtlib.net
{
    public class Task
    {
        public int? ItemNumber;
        public String Priority = String.Empty;
        public DateTime? CreatedDate;
        public DateTime? CompletedDate;
        public String Body;
        public List<String> Contexts = new List<String>();
        public List<String> Projects = new List<String>();
        private bool _completed;

        public string Raw { get; set; }

        private string _dueDate = String.Empty;
        public String DueDate
        {
            get { return _dueDate; }
            set { _dueDate = value; }
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

        private String ParseProjects(String todo)
        {
            Projects.Clear();

            MatchCollection projects = Regex.Matches(todo, @"\s(\+\w+)");

            foreach (Match match in projects)
            {
                String project = match.Groups[1].Captures[0].Value;
                Projects.Add(project);
            }

            return todo;
        }

        private String ParseContexts(String todo)
        {
            Contexts.Clear();

            MatchCollection contexts = Regex.Matches(todo, @"\s(@\w+)");

            foreach (Match match in contexts)
            {
                String context = match.Groups[1].Captures[0].Value;
                Contexts.Add(context);
            }

            return todo;
        }

        private void ParseEverythingElse(String todo)
        {
            Match everythingElse = Regex.Match(todo, @"(?:(?<done>[xX]) )?(?:\((?<priority>[A-Z])\) )?(?:(?<date>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$");

            if (everythingElse != Match.Empty)
            {
                if (everythingElse.Groups["date"].Success)
                {
                    CreatedDate = DateTime.Parse(everythingElse.Groups["date"].Value);
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
            string body, string dueDate = "", bool completed = false)
        {
            Priority = priority.Replace("(", String.Empty).Replace(")", String.Empty);
            Projects = projects;
            Contexts = contexts;
            DueDate = dueDate;
            Body = body + (Contexts.Count > 0 ? " " : String.Empty)
                       + String.Join(" ", Contexts.ToArray())
                       + (Projects.Count > 0 ? " " : String.Empty)
                       + String.Join(" ", Projects.ToArray());
            
            Completed = completed;

            Raw = (_completed ? "x " : String.Empty)
                   + (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
                   + (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
                   + Body; 
        }

        private void ParseFields(String todo)
        {
            todo = ParseContexts(todo);
            todo = ParseProjects(todo);

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
                + (CreatedDate.HasValue ? (CreatedDate.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
                + Body;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace todotxtlib.net
{
    public class Task
    {
        public int ItemNumber;
        public String Priority = String.Empty;
        public DateTime? Date;
        public String Text;
        public List<String> Contexts = new List<String>();
        public List<String> Projects = new List<String>();
        private bool _completed;
        private String Raw = String.Empty;

        public bool Completed
        {
            get { return _completed; }
        }

        public void MarkCompleted()
        {
            _completed = true;

            if (IsPriority)
            {
                Priority = String.Empty;
            }

            Date = DateTime.Now;
        }

        public void Empty()
        {
            Text = String.Empty;
            Date = null;
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
                todo = todo.Replace(project, String.Empty);
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
                todo = todo.Replace(context, String.Empty);
            }

            return todo;
        }

        private void ParseEverythingElse(String todo)
        {
            Match everythingElse = Regex.Match(todo, @"(?:(?<done>[x]) )?(?:\((?<priority>[A-Z])\) )?(?:(?<date>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$");

            if (everythingElse != Match.Empty)
            {
                if (everythingElse.Groups["date"].Success)
                {
                    Date = DateTime.Parse(everythingElse.Groups["date"].Value);
                }

                if (everythingElse.Groups["priority"].Success)
                {
                    Priority = everythingElse.Groups["priority"].Value;
                }

                if (everythingElse.Groups["todo"].Success)
                {
                    Text = everythingElse.Groups["todo"].Value;
                }

                if (everythingElse.Groups["done"].Success)
                {
                    _completed = true;
                }
            }
        }

        public Task(String todo, int itemNumber)
        {
            ItemNumber = itemNumber;

            Raw = todo.Replace(Environment.NewLine, ""); //make sure it's just on one line

            ParseFields(todo);
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
            ParseFields(ToDoProjectContext + toAppend);
        }

        public void Prepend(String toPrepend)
        {
            ParseFields(toPrepend + ToDoProjectContext);
        }

        public bool ReplaceItemText(string oldText, string newText)
        {
            String replaceableText = ToDoProjectContext;

            if (replaceableText.Contains(oldText))
            {
                replaceableText = replaceableText.Replace(oldText, newText);
                ParseFields(replaceableText);
                return true;
            }

            return false;
        }

        public bool IsPriority
        {
            get { return !String.IsNullOrEmpty(Priority); }
        }

        private String ToDoProjectContext
        {
            get
            {
                return Text
                       + (Projects.Count > 0 ? " " : String.Empty)
                       + String.Join(" ", Projects.ToArray())
                       + (Contexts.Count > 0 ? " " : String.Empty)
                       + String.Join(" ", Contexts.ToArray());
            }
        }

        public String ToString(String numberFormat)
        {
            return ItemNumber.ToString(numberFormat) + " " + ToString();
        }

        public override String ToString()
        {
            return
                (_completed ? "x " : String.Empty)
                + (!String.IsNullOrEmpty(Priority) ? "(" + Priority + ") " : String.Empty)
                + (Date.HasValue ? (Date.Value.ToString("yyyy-MM-dd") + " ") : String.Empty)
                + ToDoProjectContext;
        }
    }
}
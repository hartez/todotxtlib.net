using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace todotxtlib.net
{
    public partial class Task : IEquatable<Task>
    {
        public string Body { get; } = "";
        public bool Completed { get; } = false;
        public DateTime? CompletedDate { get; } = null;
        public DateTime? CreatedDate { get; } = null;
        public IReadOnlyList<string> Contexts { get; init; } = [];
        public IReadOnlyList<string> Projects { get; init; } = [];
        public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

        // TODO Temp solving this with nullable char; move this to an enum once everything else is calmed down
        public char? Priority { get; } = null;

        public static Task Empty { get; } = new Task();

        private Task(string body, char? priority = null, DateTime? createdDate = null,
            List<string> projects = null, List<string> contexts = null, Dictionary<string, string> metadata = null) 
            : this(body, createdDate, projects, contexts, metadata)
        {
            Priority = priority;
        }

        private Task(string body, DateTime? completedDate, DateTime? createdDate = null,
            List<string> projects = null, List<string> contexts = null, Dictionary<string, string> metadata = null) 
            : this(body, createdDate, projects, contexts, metadata) 
        {
            Completed = true;
            CompletedDate = completedDate;
        }

        private Task() : this("", null, null, null, null)
        {
        }

        private Task(string body, DateTime? createdDate = null,
            List<string> projects = null,
            List<string> contexts = null,
            Dictionary<string, string> metadata = null)
        {
            Body = body;
            CreatedDate = createdDate;
            Projects = projects;
            Contexts = contexts;
            Metadata = metadata;
        }

        public string DueDate => Metadata.TryGetValue("due", out var value) ? value : string.Empty;
        public bool IsPriority => Priority != null;

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (Completed)
            {
                stringBuilder.Append("x ");
            }

            if (Completed && CompletedDate.HasValue)
            {
                stringBuilder.Append($"{CompletedDate.Value.ToString("yyyy-MM-dd")} ");
            }

            if (!Completed && Priority != null)
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

        internal Task WithReplacementText(string oldText, string newText)
        {
            var body = Body.Replace(oldText, newText);

            if (Completed)
            {
                return new Task(body, completedDate: CompletedDate, createdDate: CreatedDate, 
                    projects: FindProjects(body), contexts: FindContexts(body), metadata: FindMetadata(body));
            }

            return new Task(body, priority: Priority, createdDate: CreatedDate);
        }

        internal Task WithPriority(char priority)
        {
            return new Task(Body, createdDate: CreatedDate, priority: char.ToUpper(priority));
        }

        internal Task WithBody(string body)
        {
            if (Completed)
            {
                return new Task(body, completedDate: CompletedDate, createdDate: CreatedDate);
            }

            return new Task(body, priority: Priority, createdDate: CreatedDate);
        }

        internal Task WithCompleted()
        {
            if (Completed)
            {
                return this;
            }

            return new Task(Body, completedDate: CompletedDate ?? DateTime.Now, createdDate: CreatedDate);
        }

        internal Task WithCreatedDate()
        {
            if (CreatedDate is not null)
            {
                return this;
            }

            return new Task(Body, priority: Priority, createdDate: DateTime.Now);
        }

        internal Task WithPending()
        {
            if (!Completed)
            {
                return this;
            }

            return new Task(Body, priority: Priority, createdDate: CreatedDate);
        }

        [GeneratedRegex(@"\s(\+\S*\w)")]
        private static partial Regex ProjectsRegex();

        [GeneratedRegex(@"\s(@\S*\w)")]
        private static partial Regex ContextsRegex();

        [GeneratedRegex(@"(?:(?<done>[x] (?:(?<completeddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )))?(?:\((?<priority>[A-Z])\) )?(?:(?<createddate>[0-9]{4}-[0-9]{2}-[0-9]{2}) )?(?<todo>.+)$")]
        private static partial Regex EverythingElseRegex();

        [GeneratedRegex(@"(?:^|\s)(?<meta>\w+:[^\s]+\S*)")]
        private static partial Regex MetaDataRegex();

        public static List<string> FindProjects(string body)
        {
            return [.. ProjectsRegex().Matches(body).Select(match => match.Groups[1].Captures[0].Value)];
        }

        public static List<string> FindContexts(string body)
        {
            return [.. ContextsRegex().Matches(body).Select(match => match.Groups[1].Captures[0].Value)];
        }

        public static Dictionary<string, string> FindMetadata(string body)
        {
            MatchCollection metadata = MetaDataRegex().Matches(body);

            var result = new Dictionary<string, string>();

            foreach (Match match in metadata)
            {
                string data = match.Groups[1].Captures[0].Value;
                string[] kvp = data.Split(':');

                result.Add(kvp[0], kvp[1]);
            }

            return result;
        }

        static DateTime? ParseDateTime(Match match, string groupName)
        {
            var group = match.Groups[groupName];

            if (!group.Success)
            {
                return null;
            }

            if (DateTime.TryParse(group.Value, out DateTime result))
            {
                return result;
            }

            return null;
        }

        static char? ParsePriority(Match match)
        {
            var group = match.Groups["priority"];

            if (group.Success)
            {
                return group.Value.First();
            }

            return null;
        }

        public static Task Parse(string task)
        {
            task = task.Trim();

            Match match = EverythingElseRegex().Match(task);

            DateTime? completedDate = ParseDateTime(match, "completeddate");
            DateTime? createdDate = ParseDateTime(match, "createddate");
            char? priority = ParsePriority(match);
            bool completed = match.Groups["done"].Success;
            string body = match.Groups["todo"].Success ? match.Groups["todo"].Value : "";

            if (completed)
            {
                return new Task(body: body, completedDate: completedDate, createdDate: createdDate,
                    projects: FindProjects(body), contexts: FindContexts(body), metadata: FindMetadata(body));
            }

            return new Task(body: body, priority: priority, createdDate: createdDate,
                    projects: FindProjects(body), contexts: FindContexts(body), metadata: FindMetadata(body));
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(Task other)
        {
            return Completed == other.Completed
                && Priority == other.Priority
                && CompletedDate == other.CompletedDate
                && CreatedDate == other.CreatedDate
                && Body == other.Body;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Body, Completed, CompletedDate, CreatedDate, Priority);
        }

        public static bool operator ==(Task obj1, Task obj2)
        {
            if (ReferenceEquals(obj1, obj2))
                return true;
            if (obj1 is null)
                return false;
            if (obj2 is null)
                return false;
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Task obj1, Task obj2) => !(obj1 == obj2);
    }
}
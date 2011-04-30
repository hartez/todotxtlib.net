using System;
using System.Collections.Generic;
using System.Linq;

namespace todotxtlib.net
{
    public class TaskList : List<Task>
    {
        private String _numberFormat;

        public TaskList()
        { }

        public TaskList(IEnumerable<Task> todos, int parentListItemCount)
            : base(todos)
        {
            _numberFormat = new String('0', parentListItemCount.ToString().Length);
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

            return new TaskList(from todo in this
                                where !(include ^ todo.ToString().Contains(term))
                                select todo, Count);
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

        public void ReplaceToDo(int item, string newText)
        {
            Task target = (from todo in this
                           where todo.ItemNumber == item
                           select todo).FirstOrDefault();

            if (target != null)
            {
                target.Replace(newText);
            }
        }

        public void AppendToDo(int item, string newText)
        {
            Task target = (from todo in this
                           where todo.ItemNumber == item
                           select todo).FirstOrDefault();

            if (target != null)
            {
                target.Append(newText);
            }
        }

        public void PrependToDo(int item, string newText)
        {
            Task target = (from todo in this
                           where todo.ItemNumber == item
                           select todo).FirstOrDefault();

            if (target != null)
            {
                target.Prepend(newText);
            }
        }

        public bool RemoveFromItem(int item, string term)
        {
            return ReplaceItemText(item, term, String.Empty);
        }

        public TaskList RemoveCompletedItems(bool preserveLineNumbers)
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

        public void RemoveItem(int item, bool preserveLineNumbers)
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
    }
}
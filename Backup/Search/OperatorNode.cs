using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IronCow.Search
{
    public class OperatorNode : Node
    {
        public string Name { get; set; }
        public string Argument { get; set; }

        static OperatorNode()
        {
            RegisterOperatorDelegates();
        }

        public OperatorNode(string name)
        {
            Name = name;
        }

        public OperatorNode(string name, string argument)
        {
            Name = name;
            Argument = argument;
        }

        public override bool ShouldInclude(SearchContext context)
        {
            Func<SearchContext, string, bool> operatorDelegate;
            if (!sOperatorDelegates.TryGetValue(Name, out operatorDelegate))
                throw new NotSupportedException();
            return operatorDelegate(context, Argument);
        }

        public override bool NeedsArchivedLists()
        {
            return (Name == "includeArchived");
        }

        public override IEnumerator<Node> GetEnumerator()
        {
            return new EmptyEnumerator<Node>();
        }

        #region Implementation Delegates
        private static Dictionary<string, Func<SearchContext, string, bool>> sOperatorDelegates;

        private static void RegisterOperatorDelegates()
        {
            sOperatorDelegates = new Dictionary<string, Func<SearchContext, string, bool>>();
            sOperatorDelegates.Add("list", OperatorList);
            sOperatorDelegates.Add("priority", OperatorPriority);
            sOperatorDelegates.Add("status", OperatorStatus);
            sOperatorDelegates.Add("tag", OperatorTag);
            sOperatorDelegates.Add("tagContains", OperatorTagContains);
            sOperatorDelegates.Add("isTagged", OperatorIsTagged);
            sOperatorDelegates.Add("location", OperatorLocation);
            sOperatorDelegates.Add("locatedWithin", OperatorLocatedWithin);
            sOperatorDelegates.Add("isLocated", OperatorIsLocated);
            sOperatorDelegates.Add("isRepeating", OperatorIsRepeating);
            sOperatorDelegates.Add("name", OperatorName);
            sOperatorDelegates.Add("noteContains", OperatorNoteContains);
            sOperatorDelegates.Add("hasNotes", OperatorHasNotes);
            sOperatorDelegates.Add("due", OperatorDue);
            sOperatorDelegates.Add("dueBefore", OperatorDueBefore);
            sOperatorDelegates.Add("dueAfter", OperatorDueAfter);
            sOperatorDelegates.Add("dueWithin", OperatorDueWithin);
            sOperatorDelegates.Add("completed", OperatorCompleted);
            sOperatorDelegates.Add("completedBefore", OperatorCompletedBefore);
            sOperatorDelegates.Add("completedAfter", OperatorCompletedAfter);
            sOperatorDelegates.Add("completedWithin", OperatorCompletedWithin);
            sOperatorDelegates.Add("added", OperatorAdded);
            sOperatorDelegates.Add("addedBefore", OperatorAddedBefore);
            sOperatorDelegates.Add("addedAfter", OperatorAddedAfter);
            sOperatorDelegates.Add("addedWithin", OperatorAddedWithin);
            sOperatorDelegates.Add("timeEstimate", OperatorTimeEstimate);
            sOperatorDelegates.Add("postponed", OperatorPostponed);
            sOperatorDelegates.Add("isShared", OperatorIsShared);
            sOperatorDelegates.Add("sharedWith", OperatorSharedWith);
            sOperatorDelegates.Add("isReceived", OperatorIsReceived);
            sOperatorDelegates.Add("to", OperatorTo);
            sOperatorDelegates.Add("from", OperatorFrom);
            sOperatorDelegates.Add("includeArchived", OperatorIncludeArchived);
            
        }

        private static bool OperatorList(SearchContext context, string argument)
        {
            if (context.Task.Parent == null)
                throw new InvalidOperationException(string.Format("The given task '{0}' doesn't belong to any task list.", context.Task.Name));
            return (string.Compare(context.Task.Parent.Name, argument, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private static bool OperatorPriority(SearchContext context, string argument)
        {
            if (string.Compare(argument, "none", StringComparison.Ordinal) == 0)
                return context.Task.Priority == TaskPriority.None;
            if (argument == "1")
                return context.Task.Priority == TaskPriority.One;
            if (argument == "2")
                return context.Task.Priority == TaskPriority.Two;
            if (argument == "3")
                return context.Task.Priority == TaskPriority.Three;
            throw new ArgumentException(string.Format("The given priority '{0}' is not valid. Should be 'none', '1', '2' or '3'.", argument));
        }

        private static bool OperatorStatus(SearchContext context, string argument)
        {
            if (string.Compare(argument, "completed", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.IsCompleted;
            if (string.Compare(argument, "incomplete", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.IsIncomplete;
            throw new ArgumentException(string.Format("The given status '{0}' is not valid. Should be 'completed' or 'incomplete'.", argument));
        }

        private static bool OperatorTag(SearchContext context, string argument)
        {
            return context.Task.Tags.Contains(argument, StringComparer.CurrentCultureIgnoreCase);
        }

        private static bool OperatorTagContains(SearchContext context, string argument)
        {
            argument = argument.ToLower();
            foreach (var tag in context.Task.Tags)
            {
                if (tag.ToLower().Contains(argument))
                    return true;
            }
            return false;
        }

        private static bool OperatorIsTagged(SearchContext context, string argument)
        {
            if (string.Compare(argument, "true", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.Tags.Count > 0;
            if (string.Compare(argument, "false", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.Tags.Count == 0;
            throw new ArgumentException(string.Format("The given tagged status '{0}' is invalid. Should be 'true' or 'false'.", argument));
        }

        private static bool OperatorLocation(SearchContext context, string argument)
        {
            return (context.Task.Location != null) && (string.Compare(context.Task.Location.Name, argument, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private static bool OperatorIsLocated(SearchContext context, string argument)
        {
            if (string.Compare(argument, "true", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.Location != null;
            if (string.Compare(argument, "false", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.Location == null;
            throw new ArgumentException(string.Format("The given located status '{0}' is invalid. Should be 'true' or 'false'.", argument));
        }

        private static bool OperatorIsRepeating(SearchContext context, string argument)
        {
            if (string.Compare(argument, "true", StringComparison.OrdinalIgnoreCase) == 0)
                return !string.IsNullOrEmpty(context.Task.Recurrence);
            if (string.Compare(argument, "false", StringComparison.OrdinalIgnoreCase) == 0)
                return string.IsNullOrEmpty(context.Task.Recurrence);
            throw new ArgumentException(string.Format("The given repeating status '{0}' is invalid. Should be 'true' or 'false'.", argument));
        }

        private static bool OperatorName(SearchContext context, string argument)
        {
            return context.Task.Name.ToLower().Contains(argument.ToLower());
        }

        private static bool OperatorNoteContains(SearchContext context, string argument)
        {
            argument = argument.ToLower();
            foreach (var note in context.Task.Notes)
            {
                if (note.Title.ToLower().Contains(argument))
                    return true;
                if (note.Body.ToLower().Contains(argument))
                    return true;
            }
            return false;
        }

        private static bool OperatorHasNotes(SearchContext context, string argument)
        {
            if (string.Compare(argument, "true", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.Notes.Count > 0;
            if (string.Compare(argument, "false", StringComparison.OrdinalIgnoreCase) == 0)
                return context.Task.Notes.Count == 0;
            throw new ArgumentException(string.Format("The given notes status '{0}' is invalid. Should be 'true' or 'false'.", argument));
        }

        private static bool OperatorDue(SearchContext context, string argument)
        {
            if (Regex.IsMatch(argument, @"\s*never\s*$", RegexOptions.IgnoreCase))
                return !context.Task.DueDateTime.HasValue;
            if (!context.Task.DueDateTime.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            return (context.Task.FuzzyDueDateTime == dateTime);
        }

        private static bool OperatorDueBefore(SearchContext context, string argument)
        {
            if (!context.Task.DueDateTime.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            return (context.Task.FuzzyDueDateTime <= dateTime);
        }

        private static bool OperatorDueAfter(SearchContext context, string argument)
        {
            if (!context.Task.DueDateTime.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            return (context.Task.FuzzyDueDateTime >= dateTime);
        }

        private static bool OperatorDueWithin(SearchContext context, string argument)
        {
            if (!context.Task.DueDateTime.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            return (context.Task.FuzzyDueDateTime >= FuzzyDateTime.Today && context.Task.FuzzyDueDateTime <= dateTime);
        }

        private static bool OperatorCompleted(SearchContext context, string argument)
        {
            if (!context.Task.Completed.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            FuzzyDateTime completed = new FuzzyDateTime(context.Task.Completed.Value, true);
            return completed == dateTime;
        }

        private static bool OperatorCompletedBefore(SearchContext context, string argument)
        {
            if (!context.Task.Completed.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            FuzzyDateTime completed = new FuzzyDateTime(context.Task.Completed.Value, true);
            return completed <= dateTime;
        }

        private static bool OperatorCompletedAfter(SearchContext context, string argument)
        {
            if (!context.Task.Completed.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            FuzzyDateTime completed = new FuzzyDateTime(context.Task.Completed.Value, true);
            return completed >= dateTime;
        }

        private static bool OperatorCompletedWithin(SearchContext context, string argument)
        {
            if (!context.Task.Completed.HasValue)
                return false;
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            DateTime completed = context.Task.Completed.Value;
            if (dateTime.HasTime)
            {
                // Make the date be backwards ("1 week of today" is not "within 1 week" but
                // "within the past week" in this context).
                TimeSpan timeSpan = dateTime.DateTime - DateTime.Now;
                DateTime after = DateTime.Now.Subtract(timeSpan);
                return completed >= after && completed <= DateTime.Now;
            }
            else
            {
                // Make the date be backwards ("1 week of today" is not "within 1 week" but
                // "within the past week" in this context).
                TimeSpan timeSpan = dateTime.DateTime - DateTime.Today;
                DateTime after = DateTime.Today.Subtract(timeSpan);
                return completed >= after && completed <= DateTime.Today;
            }
        }

        private static bool OperatorAdded(SearchContext context, string argument)
        {
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            FuzzyDateTime added = new FuzzyDateTime(context.Task.Added, true);
            return added == dateTime;
        }

        private static bool OperatorAddedBefore(SearchContext context, string argument)
        {
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            FuzzyDateTime added = new FuzzyDateTime(context.Task.Added, true);
            return added <= dateTime;
        }

        private static bool OperatorAddedAfter(SearchContext context, string argument)
        {
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            FuzzyDateTime added = new FuzzyDateTime(context.Task.Added, true);
            return added >= dateTime;
        }

        private static bool OperatorAddedWithin(SearchContext context, string argument)
        {
            FuzzyDateTime dateTime = DateConverter.ParseDateTime(argument, context.DateFormat);
            DateTime added = context.Task.Added;
            if (dateTime.HasTime)
            {
                // Make the date be backwards ("1 week of today" is not "within 1 week" but
                // "within the past week" in this context).
                TimeSpan timeSpan = dateTime.DateTime - DateTime.Now;
                DateTime after = DateTime.Now.Subtract(timeSpan);
                return added >= after && added <= DateTime.Now;
            }
            else
            {
                // Make the date be backwards ("1 week of today" is not "within 1 week" but
                // "within the past week" in this context).
                TimeSpan timeSpan = dateTime.DateTime - DateTime.Today;
                DateTime after = DateTime.Today.Subtract(timeSpan);
                return added >= after && added <= DateTime.Today;
            }
        }

        private static bool OperatorTimeEstimate(SearchContext context, string argument)
        {
            if (string.IsNullOrEmpty(context.Task.Estimate))
                return false;

            Match match = Regex.Match(argument, @"^\s*(?<comparison>\<|\>)(?<time>.*)$");
            if (!match.Success)
                throw new ArgumentException();
            TimeSpan timeSpan = DateConverter.GetTimeSpan(match.Groups["time"].Value);
            TimeSpan estimate = DateConverter.GetTimeSpan(context.Task.Estimate);
            if (match.Groups["comparison"].Success)
            {
                if (match.Groups["comparison"].Value == "<")
                    return estimate <= timeSpan;
                else
                    return estimate >= timeSpan;
            }
            else
            {
                return timeSpan == estimate;
            }
        }

        private static bool OperatorPostponed(SearchContext context, string argument)
        {
            Match match = Regex.Match(argument, @"^\s*(?<comparison>\<|\>)(?<num>\d+)$");
            if (!match.Success)
                throw new ArgumentException(string.Format("The given number of times postponed '{0}' is invalid, or has an invalid comparison operator ('>' or '<').", argument));
            int num = int.Parse(match.Groups["num"].Value);
            int timesPostponed = context.Task.Postponed;
            if (match.Groups["comparison"].Success)
            {
                if (match.Groups["comparison"].Value == "<")
                    return timesPostponed <= num;
                else
                    return timesPostponed >= num;
            }
            else
            {
                return timesPostponed == num;
            }
        }

        private static bool OperatorIsShared(SearchContext context, string argument)
        {
            throw new NotSupportedException("Operator 'shared' is not supported on the client.");
        }

        private static bool OperatorSharedWith(SearchContext context, string argument)
        {
            throw new NotSupportedException("Operator 'sharedWith' is not supported on the client.");
        }

        private static bool OperatorIsReceived(SearchContext context, string argument)
        {
            throw new NotSupportedException("Operator 'isReceived' is not supported on the client.");
        }

        private static bool OperatorTo(SearchContext context, string argument)
        {
            throw new NotSupportedException("Operator 'to' is not supported on the client.");
        }

        private static bool OperatorFrom(SearchContext context, string argument)
        {
            throw new NotSupportedException("Operator 'from' is not supported on the client.");
        }

        private static bool OperatorLocatedWithin(SearchContext context, string argument)
        {
            // TODO: implement location-based searches
            return false;
        }

        private static bool OperatorIncludeArchived(SearchContext context, string argument)
        {
            // Do nothing...
            return true;
        }
        #endregion
    }
}

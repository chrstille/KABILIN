using KABILIN.Models;

namespace KABILIN.Helpers
{
    public record ProgressStep(string Label, string State);

    /// <summary>Builds the five-step progress tracker shown to families and administrators.</summary>
    public static class RequestProgress
    {
        private static readonly string[] Labels = { "Submitted", "Under review", "Processing", "Approved", "Completed" };

        private static int IndexOf(int statusId) => statusId switch
        {
            StatusIds.Submitted => 0,
            StatusIds.UnderReview => 1,
            StatusIds.Incomplete => 1,
            StatusIds.Processing => 2,
            StatusIds.Approved => 3,
            StatusIds.Completed => 4,
            _ => 0
        };

        public static List<ProgressStep> Build(int statusId, int? previousStatusId)
        {
            var steps = new List<ProgressStep>();
            if (statusId == StatusIds.Completed)
            {
                foreach (var l in Labels) steps.Add(new ProgressStep(l, "done"));
                return steps;
            }

            if (statusId == StatusIds.Rejected)
            {
                var reached = IndexOf(previousStatusId ?? StatusIds.Submitted);
                for (var i = 0; i < Labels.Length; i++)
                    steps.Add(new ProgressStep(i == reached + 1 ? "Rejected" : Labels[i],
                        i <= reached ? "done" : i == reached + 1 ? "rejected" : "todo"));
                return steps;
            }

            var current = IndexOf(statusId);
            for (var i = 0; i < Labels.Length; i++)
            {
                var state = i < current ? "done"
                    : i == current ? (statusId == StatusIds.Incomplete ? "attention" : "current")
                    : "todo";
                var label = i == current && statusId == StatusIds.Incomplete ? "Needs documents" : Labels[i];
                steps.Add(new ProgressStep(label, state));
            }
            return steps;
        }
    }
}

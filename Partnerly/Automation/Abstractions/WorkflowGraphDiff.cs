namespace Partnerly.Automation.Abstractions
{
    public static class WorkflowGraphDiff
    {
        // True when the draft differs from the published graph in what the agent does.
        // Moving cards on the canvas is not a change that needs a new publish.
        public static bool HasLogicChanges(string publishedJson, string draftJson) =>
            Logic(publishedJson) != Logic(draftJson);

        private static string Logic(string graphJson)
        {
            var graph = WorkflowGraph.Parse(graphJson);
            graph.Notes = null; // notes are only a picture, not what the agent does
            if (graph.Settings != null) { graph.Settings.NotifyOnFailure = null; }
            foreach (var node in graph.Nodes)
            {
                node.X = 0;
                node.Y = 0;
            }

            return graph.ToJson();
        }
    }
}

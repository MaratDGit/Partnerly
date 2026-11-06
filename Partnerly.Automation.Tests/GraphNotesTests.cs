using Partnerly.Automation.Abstractions;

namespace Partnerly.Automation.Tests
{
    // Sticky notes on the editor canvas are only a picture: they must survive Save, and must never
    // count as a change of what the agent does.
    public class GraphNotesTests
    {
        private const string WithNotes =
            "{\"schemaVersion\":1,\"nodes\":[{\"id\":\"t1\",\"kind\":\"trigger\",\"type\":\"manual\",\"name\":\"start\",\"x\":0,\"y\":0,\"config\":{}}]," +
            "\"edges\":[],\"notes\":[{\"id\":\"n1\",\"text\":\"Check this\",\"x\":10,\"y\":20,\"w\":220,\"h\":130,\"color\":\"blue\"}]}";

        [Fact]
        public void Notes_survive_parse_and_save()
        {
            var graph = WorkflowGraph.Parse(WithNotes);
            Assert.NotNull(graph.Notes);
            Assert.Single(graph.Notes!);
            var again = WorkflowGraph.Parse(graph.ToJson());
            Assert.Equal("Check this", again.Notes![0].Text);
            Assert.Equal("blue", again.Notes[0].Color);
            Assert.Equal(220, again.Notes[0].W);
        }

        [Fact]
        public void Graph_without_notes_writes_no_notes_field()
        {
            var graph = WorkflowGraph.Parse("{\"nodes\":[],\"edges\":[]}");
            Assert.Null(graph.Notes);
            Assert.DoesNotContain("notes", graph.ToJson());
        }

        [Fact]
        public void Editing_notes_is_not_a_logic_change()
        {
            var plain = "{\"schemaVersion\":1,\"nodes\":[{\"id\":\"t1\",\"kind\":\"trigger\",\"type\":\"manual\",\"name\":\"start\",\"x\":0,\"y\":0,\"config\":{}}],\"edges\":[]}";
            Assert.False(WorkflowGraphDiff.HasLogicChanges(plain, WithNotes));
            Assert.False(WorkflowGraphDiff.HasLogicChanges(WithNotes, plain));
        }
    }
}

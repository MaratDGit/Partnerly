using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Tests
{
    public class GraphValidatorTests
    {
        private readonly GraphValidator _validator = new();

        [Fact]
        public void Accepts_linear_manual_graph()
        {
            var graph = WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""setFlag"", ""config"": {} }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }");

            Assert.Empty(_validator.Validate(graph));
        }

        [Fact]
        public void Rejects_unknown_action_and_unreachable_node()
        {
            var graph = WorkflowGraph.Parse(@"{
              ""schemaVersion"": 1,
              ""nodes"": [
                { ""id"": ""t1"", ""kind"": ""trigger"", ""type"": ""manual"", ""name"": ""start"", ""config"": {} },
                { ""id"": ""a1"", ""kind"": ""action"", ""type"": ""not_a_real_action"", ""name"": ""bad"", ""config"": {} },
                { ""id"": ""a2"", ""kind"": ""action"", ""type"": ""set_variable"", ""name"": ""orphan"", ""config"": {} }
              ],
              ""edges"": [ { ""id"": ""e1"", ""from"": ""t1"", ""to"": ""a1"" } ]
            }");

            var errors = _validator.Validate(graph);
            Assert.Contains(errors, item => item.Contains("Unknown action"));
            Assert.Contains(errors, item => item.Contains("not reachable"));
        }
    }
}

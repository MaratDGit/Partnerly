using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Automation.Conditions;
using Partnerly.Automation.Engine;

namespace Partnerly.Automation.Tests
{
    public class ConditionEvaluatorTests
    {
        private readonly ConditionEvaluator _evaluator = new();
        private readonly VariableBinder _binder = new();

        [Fact]
        public void And_requires_all_children()
        {
            var node = new ConditionNode
            {
                Op = "and",
                Children =
                {
                    new ConditionNode { Op = "equals", Left = "vars.a", Right = "1" },
                    new ConditionNode { Op = "equals", Left = "vars.b", Right = "2" }
                }
            };
            var vars = Vars(("a", "1"), ("b", "2"));
            Assert.True(_evaluator.Evaluate(node, vars, _binder));
            vars["vars"]!["b"] = "9";
            Assert.False(_evaluator.Evaluate(node, vars, _binder));
        }

        [Fact]
        public void Or_and_not_work()
        {
            var inner = new ConditionNode { Op = "equals", Left = "vars.a", Right = "yes" };
            var or = new ConditionNode { Op = "or", Children = { inner, new ConditionNode { Op = "equals", Left = "vars.b", Right = "x" } } };
            var not = new ConditionNode { Op = "not", Children = { inner } };
            var vars = Vars(("a", "no"), ("b", "x"));
            Assert.True(_evaluator.Evaluate(or, vars, _binder));
            Assert.True(_evaluator.Evaluate(not, vars, _binder));
        }

        [Fact]
        public void Empty_and_contains()
        {
            var empty = new ConditionNode { Op = "is_empty", Left = "vars.missing" };
            var contains = new ConditionNode { Op = "contains", Left = "vars.a", Right = "ell" };
            var vars = Vars(("a", "Hello"));
            Assert.True(_evaluator.Evaluate(empty, vars, _binder));
            Assert.True(_evaluator.Evaluate(contains, vars, _binder));
        }

        [Fact]
        public void Changed_compares_before_and_after()
        {
            var node = new ConditionNode { Op = "changed", Left = "status" };
            var variables = new JsonObject
            {
                ["trigger"] = new JsonObject
                {
                    ["before"] = new JsonObject { ["status"] = "N" },
                    ["after"] = new JsonObject { ["status"] = "P" }
                }
            };
            Assert.True(_evaluator.Evaluate(node, variables, _binder));
            ((JsonObject)variables["trigger"]!["after"]!)["status"] = "N";
            Assert.False(_evaluator.Evaluate(node, variables, _binder));
        }

        private static JsonObject Vars(params (string key, string value)[] pairs)
        {
            var bag = new JsonObject();
            foreach (var pair in pairs)
            {
                bag[pair.key] = pair.value;
            }

            return new JsonObject { ["vars"] = bag };
        }
    }
}

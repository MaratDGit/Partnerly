using System.Text.Json.Nodes;

namespace Partnerly.Automation.Abstractions
{
    public interface IConditionEvaluator
    {
        bool Evaluate(ConditionNode? node, JsonObject variables, IVariableBinder binder);
    }
}

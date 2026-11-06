namespace Partnerly.Agent.Security
{
    // A refusal or failure that is safe to show in the run history of the agent on the site.
    public sealed class AgentException : Exception
    {
        public AgentException(string message) : base(message) { }
    }
}

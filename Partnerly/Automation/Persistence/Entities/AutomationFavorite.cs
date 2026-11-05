namespace Partnerly.Automation.Persistence.Entities
{
    // A palette item (trigger / action) pinned by a user. Stored per user so the
    // favorites follow the user into every automation editor and browser.
    public class AutomationFavorite
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        // "<kind>:<type>", e.g. "action:gmail.send".
        public string ItemKey { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}

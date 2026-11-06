using Partnerly.Events;
using Partnerly.Models;
using Xunit;

namespace Partnerly.Automation.Tests
{
    public class AppEventTests
    {
        private static User NewUser() => new()
        {
            Id = Guid.NewGuid(),
            Email = "a@example.com",
            FirstName = "Ann",
            LastName = "Lee",
            AccessFailedCount = 3,
            LockoutEnd = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc)
        };

        [Fact]
        public void User_events_carry_the_user_fields_and_no_password()
        {
            var user = NewUser();
            user.PasswordHash = "secret-hash";

            var evt = AutomationAppEvent.ForUser(AutomationAppEvent.UserLoggedIn, user);

            Assert.Equal("UserLoggedIn", evt.Name);
            Assert.Equal(user.Id.ToString(), evt.Payload["id"]!.GetValue<string>());
            Assert.Equal("a@example.com", evt.Payload["email"]!.GetValue<string>());
            Assert.DoesNotContain("secret-hash", evt.Payload.ToJsonString());
        }

        [Fact]
        public void Login_failed_reports_attempts_and_lockout()
        {
            var evt = AutomationAppEvent.LoginFailed(NewUser(), lockedOut: true);

            Assert.Equal("UserLoginFailed", evt.Name);
            Assert.Equal(3, evt.Payload["attempts"]!.GetValue<int>());
            Assert.True(evt.Payload["lockedOut"]!.GetValue<bool>());
        }

        [Fact]
        public void Locked_out_reports_the_end_of_the_lock()
        {
            var evt = AutomationAppEvent.LockedOut(NewUser());

            Assert.Equal("UserLockedOut", evt.Name);
            Assert.StartsWith("2026-11-06T12:00:00", evt.Payload["lockedUntil"]!.GetValue<string>());
        }

        [Fact]
        public void Ticket_assigned_names_both_assignees()
        {
            var previous = Guid.NewGuid();
            var ticket = new SupportTicket { Id = Guid.NewGuid(), TicketID = "T-1", Subject = "Help", Status = "New", AssignedTo = Guid.NewGuid() };

            var evt = AutomationAppEvent.TicketAssigned(ticket, previous);

            Assert.Equal("SupportTicketAssigned", evt.Name);
            Assert.Equal(ticket.AssignedTo.ToString(), evt.Payload["assignedTo"]!.GetValue<string>());
            Assert.Equal(previous.ToString(), evt.Payload["previousAssignedTo"]!.GetValue<string>());
        }
    }
}

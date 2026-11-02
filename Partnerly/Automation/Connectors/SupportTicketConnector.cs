using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;
using Partnerly.Models.Authorization;

namespace Partnerly.Automation.Connectors
{
    public class SupportTicketConnector : ConnectorBase
    {
        private readonly ISupportTicketService _tickets;
        private readonly IPermissionService _permissions;

        public SupportTicketConnector(
            AppDbContext db,
            ISupportTicketService tickets,
            IPermissionService permissions) : base(db)
        {
            _tickets = tickets;
            _permissions = permissions;
        }

        public override string Key => "support_ticket";
        public override string DisplayName => "Support ticket";

        public override IReadOnlyList<EntityFieldDescriptor> Fields { get; } = new[]
        {
            new EntityFieldDescriptor { Name = "id", DataType = "guid" },
            new EntityFieldDescriptor { Name = "ticketId", DataType = "string" },
            new EntityFieldDescriptor { Name = "subject", DataType = "string", Updatable = true },
            new EntityFieldDescriptor { Name = "message", DataType = "string" },
            new EntityFieldDescriptor { Name = "status", DataType = "string", Updatable = true },
            new EntityFieldDescriptor { Name = "userId", DataType = "guid" },
            new EntityFieldDescriptor { Name = "assignedTo", DataType = "guid", Updatable = true },
            new EntityFieldDescriptor { Name = "note", DataType = "string", Updatable = true }
        };

        public override bool Can(string operation) =>
            operation is "find" or "create" or "update" or "delete";

        public override async Task<JsonObject?> FindAsync(JsonObject filters, Guid ownerUserId, CancellationToken cancellationToken)
        {
            SupportTicket? ticket = null;
            var id = ReadId(filters, "id");
            if (id != null)
            {
                ticket = await _tickets.GetSupportTicketByIDAsync(id);
            }
            else
            {
                var ticketId = ReadString(filters, "ticketId") ?? ReadString(filters, "ticketID");
                if (!string.IsNullOrWhiteSpace(ticketId))
                {
                    ticket = (await _tickets.GetAllSupportTicketAsync())
                        .FirstOrDefault(item => item != null && item.TicketID == ticketId);
                }
            }

            if (ticket == null || !await CanSeeTicketAsync(ticket, ownerUserId, cancellationToken))
            {
                return null;
            }

            var bag = ToBag(ticket);
            return Matches(bag, filters) ? bag : null;
        }

        public override async Task<JsonObject?> CreateAsync(JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken)
        {
            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                UserId = ReadId(fields, "userId") ?? ownerUserId,
                Subject = ReadString(fields, "subject") ?? "Automation",
                Message = ReadString(fields, "message") ?? string.Empty,
                Status = ReadString(fields, "status") ?? "New",
                AssignedTo = ReadId(fields, "assignedTo"),
                Note = ReadString(fields, "note")
            };
            var result = await _tickets.CreateSupportTicketAsync(ticket);
            return result.Success && result.Data != null ? ToBag(result.Data) : null;
        }

        public override async Task<JsonObject?> UpdateAsync(JsonObject key, JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken)
        {
            var id = ReadId(key, "id") ?? ReadId(fields, "id");
            var ticket = id == null ? null : await _tickets.GetSupportTicketByIDAsync(id);
            if (ticket == null || !await CanSeeTicketAsync(ticket, ownerUserId, cancellationToken))
            {
                return null;
            }

            var oldAssigned = ticket.AssignedTo;
            var oldStatus = ticket.Status ?? string.Empty;
            if (fields.TryGetPropertyValue("status", out var status) && status != null)
            {
                ticket.Status = status.GetValue<string>();
            }

            if (fields.TryGetPropertyValue("assignedTo", out var assigned))
            {
                ticket.AssignedTo = Guid.TryParse(assigned?.ToString()?.Trim('"'), out var assignedId) ? assignedId : null;
            }

            if (fields.TryGetPropertyValue("note", out var note))
            {
                ticket.Note = note?.GetValue<string>() ?? note?.ToString()?.Trim('"');
            }

            var result = await _tickets.UpdateSupportTicketAsync(ticket, oldAssigned, oldStatus);
            return result.Success && result.Data != null ? ToBag(result.Data) : null;
        }

        public override async Task<bool> DeleteAsync(JsonObject key, Guid ownerUserId, CancellationToken cancellationToken)
        {
            if (!await _permissions.HasPermissionAsync(ownerUserId, PermissionCodes.SupportTicketsDelete))
            {
                return false;
            }

            var id = ReadId(key, "id");
            if (id == null)
            {
                return false;
            }

            var ticket = await _tickets.GetSupportTicketByIDAsync(id);
            if (ticket == null || !await CanSeeTicketAsync(ticket, ownerUserId, cancellationToken))
            {
                return false;
            }

            var result = await _tickets.DeleteSupportTicketAsync(id);
            return result.Success;
        }

        public override JsonObject ToBag(object entity)
        {
            if (entity is not SupportTicket ticket)
            {
                return new JsonObject();
            }

            return new JsonObject
            {
                ["id"] = ticket.Id.ToString(),
                ["ticketId"] = ticket.TicketID,
                ["subject"] = ticket.Subject,
                ["message"] = ticket.Message,
                ["status"] = ticket.Status,
                ["userId"] = GuidValue(ticket.UserId),
                ["assignedTo"] = GuidValue(ticket.AssignedTo),
                ["note"] = ticket.Note,
                ["isDeleted"] = ticket.IsDeleted
            };
        }

        private async Task<bool> CanSeeTicketAsync(SupportTicket ticket, Guid ownerUserId, CancellationToken cancellationToken)
        {
            if (ticket.UserId == ownerUserId || ticket.AssignedTo == ownerUserId)
            {
                return true;
            }

            if (ticket.UserId == null)
            {
                return false;
            }

            var creator = await Db.Users!.FindAsync(new object[] { ticket.UserId.Value }, cancellationToken);
            return creator != null && await OwnerCanViewUserAsync(ownerUserId, creator, cancellationToken);
        }
    }
}

using System.Text.Json.Nodes;
using Partnerly.Automation.Abstractions;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models;

namespace Partnerly.Automation.Connectors
{
    public class EmailTemplateConnector : ConnectorBase
    {
        private readonly IEmailTemplateService _templates;

        public EmailTemplateConnector(AppDbContext db, IEmailTemplateService templates) : base(db)
        {
            _templates = templates;
        }

        public override string Key => "email_template";
        public override string DisplayName => "Email template";

        public override IReadOnlyList<EntityFieldDescriptor> Fields { get; } = new[]
        {
            new EntityFieldDescriptor { Name = "id", DataType = "guid" },
            new EntityFieldDescriptor { Name = "name", DataType = "string" },
            new EntityFieldDescriptor { Name = "subject", DataType = "string" }
        };

        public override bool Can(string operation) => operation == "find";

        public override async Task<JsonObject?> FindAsync(JsonObject filters, Guid ownerUserId, CancellationToken cancellationToken)
        {
            EmailTemplate? template = null;
            var id = ReadId(filters, "id");
            if (id != null)
            {
                template = await _templates.GetTemplateByIDAsync(id);
            }
            else
            {
                var name = ReadString(filters, "name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    template = await _templates.GetTemplateByNameAsync(name);
                }
            }

            if (template == null || template.IsDeleted)
            {
                return null;
            }

            var bag = ToBag(template);
            return Matches(bag, filters) ? bag : null;
        }

        public override Task<JsonObject?> CreateAsync(JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<JsonObject?>(null);

        public override Task<JsonObject?> UpdateAsync(JsonObject key, JsonObject fields, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<JsonObject?>(null);

        public override Task<bool> DeleteAsync(JsonObject key, Guid ownerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public override JsonObject ToBag(object entity)
        {
            if (entity is not EmailTemplate template)
            {
                return new JsonObject();
            }

            return new JsonObject
            {
                ["id"] = template.Id.ToString(),
                ["name"] = template.Name,
                ["subject"] = template.Subject
            };
        }
    }
}

using System;
using System.Collections.Generic;

namespace Partnerly.Models.ViewModels
{
    public class SendToGroupMembersRequest
    {
        public Guid GroupId { get; set; }
        public List<Guid> UserIds { get; set; } = new();
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }
}

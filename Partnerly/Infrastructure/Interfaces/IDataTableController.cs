using Microsoft.AspNetCore.Mvc;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IDataTableController
    {
        Task<JsonResult> GetData();
    }
}

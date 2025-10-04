using Microsoft.AspNetCore.Mvc;
using Partnerly.Infrastructure.Interfaces;

namespace Partnerly.Controllers
{
    public class LogRecordsController : _BaseController
    {
        public LogRecordsController(IUserService userService, ICurrentUserService currentUser)
        : base(userService, currentUser)
        {
        }

        public class UserViewModel 
        { 
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Code { get; set; }
           
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetUsers(
        int page = 1,
        int pageSize = 10,
        string search = "",
        string sortColumn = "Id",
        string sortOrder = "asc")
        {
            var allUsers = new List<UserViewModel>();
            for (int i = 1; i <= 100; i++)
            {
                allUsers.Add(new UserViewModel
                {
                    Id = i,
                    Name = $"User{i}",
                    Email = $"user{i}@mail.com"
                });
            }

            var data = allUsers;

            return Json(new
            {
                data
            });
        }
    }
}

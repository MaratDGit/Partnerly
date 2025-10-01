using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Partnerly.Descriptors.Messages;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Models.ViewModels;
using System.Diagnostics;

namespace Partnerly.Controllers
{
    public class DashboardController : _BaseController
    {
        public DashboardController(IUserService userService, ICurrentUserService currentUser)
        : base(userService, currentUser)
        {
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> MyProfile()
        {
            return View();
        }

        public async Task<IActionResult> Settings()
        {
            return View();
        }

        public async Task<IActionResult> Payments()
        {
            return View();
        }
    }
}

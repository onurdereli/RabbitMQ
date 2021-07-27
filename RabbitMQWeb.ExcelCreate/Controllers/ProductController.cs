using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using RabbitMQWeb.ExcelCreate.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RabbitMQWeb.ExcelCreate.Services;
using RabbitMQWeb.ExcelCreate.Utils;
using Shared;

namespace RabbitMQWeb.ExcelCreate.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {

        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RabbitMqPublisher _rabbitMqPublisher;

        public ProductController(AppDbContext context, UserManager<IdentityUser> userManager, RabbitMqPublisher rabbitMqPublisher)
        {
            _context = context;
            _userManager = userManager;
            _rabbitMqPublisher = rabbitMqPublisher;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> CreateProductExcel()
        {
            var user = await _userManager.FindByNameAsync(User.Identity?.Name);

            var fileName = $"product-exce{Guid.NewGuid().ToString().Substring(1, 10)}";

            UserFile userFile = new()
            {
                FileName = fileName,
                UserId = user.Id,
                FileStatus = FileStatus.Creating
            };

            await _context.UserFiles.AddAsync(userFile);
            await _context.SaveChangesAsync();
            //rabbitmq'ya mesaj gönder
            _rabbitMqPublisher.Publish(new CreateExcelMessage{FileId = userFile.Id});
            TempData["StartCreatingExcel"] = true;
            
            return RedirectToAction(nameof(Files));
        }

        public async Task<IActionResult> Files()
        {
            var user = await _userManager.FindByNameAsync(User.Identity?.Name);

            var userList = await _context.UserFiles
                .Where(x => x.UserId == user.Id)
                .OrderByDescending(x=> x.Id)
                .ToListAsync();

            return View(userList);
        }
    }
}

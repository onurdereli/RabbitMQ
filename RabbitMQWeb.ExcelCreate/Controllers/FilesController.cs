using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RabbitMQWeb.ExcelCreate.Hubs;
using RabbitMQWeb.ExcelCreate.Models;
using RabbitMQWeb.ExcelCreate.Utils;

namespace RabbitMQWeb.ExcelCreate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<MyHub> _hubContext;

        public FilesController(AppDbContext context, IHubContext<MyHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, int fileId)
        {
            if(file.Length <= 0)
            {
                return BadRequest();
            }

            var userFile = await _context.UserFiles.FirstAsync(x => x.Id == fileId);

            var filePath = file.FileName + Path.GetExtension(file.FileName);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/files", filePath);

            await using FileStream stream = new(path, FileMode.Create);

            await file.CopyToAsync(stream);

            userFile.FilePath = path;
            userFile.FileStatus = FileStatus.Completed;
            userFile.CreatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            //SignalR notification oluşturulacak
            await _hubContext.Clients.User(userFile.UserId).SendAsync("CompletedFile");

            return Ok();
        }
    }
}

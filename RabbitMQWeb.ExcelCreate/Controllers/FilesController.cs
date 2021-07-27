using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RabbitMQWeb.ExcelCreate.Models;
using RabbitMQWeb.ExcelCreate.Utils;

namespace RabbitMQWeb.ExcelCreate.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FilesController(AppDbContext context)
        {
            _context = context;
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

            return Ok();
        }
    }
}

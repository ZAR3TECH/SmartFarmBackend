using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_Farm.DTOS;
using Smart_Farm.Models;

namespace Smart_Farm.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class plantController : ControllerBase
    {
        farContext db;
        private readonly Cloudinary _cloudinary;

        public plantController(farContext db, IConfiguration configuration)
        {
            this.db = db;
            _cloudinary = new Cloudinary(new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            ));
        }

        // ─── GET api/plant ────────────────────────────────────────────────────
        [HttpGet]
        public ActionResult GetAll()
        {
            var plants = db.PLANTs
                .AsNoTracking()
                .Select(p => new PlantResponseDto
                {
                    Pid = p.Pid,
                    PhotoUrl = p.PhotoUrl,
                    Name = p.Name,
                    Description = p.Description,
                    Seed_type = p.Seed_type,
                    Fertilizer_need = p.Fertilizer_need,
                    Days_to_harvest = p.Days_to_harvest,
                    Season = p.Season,
                    Humidity_range = p.Humidity_range,
                    Water_need = p.Water_need,
                    Soil_type = p.Soil_type,
                    Temperature_range = p.Temperature_range
                })
                .ToList();

            return Ok(plants);
        }

        // ─── GET api/plant/{id} ───────────────────────────────────────────────
        [HttpGet("{id}")]
        public ActionResult GetById(int id)
        {
            PLANT? b = db.PLANTs.Find(id);
            if (b == null) return NotFound();

            return Ok(new PlantResponseDto
            {
                Pid = b.Pid,
                PhotoUrl = b.PhotoUrl,
                Name = b.Name,
                Description = b.Description,
                Seed_type = b.Seed_type,
                Fertilizer_need = b.Fertilizer_need,
                Days_to_harvest = b.Days_to_harvest,
                Season = b.Season,
                Humidity_range = b.Humidity_range,
                Water_need = b.Water_need,
                Soil_type = b.Soil_type,
                Temperature_range = b.Temperature_range
            });
        }

        

        // ─── GET api/plant/compatibility/{pid} ───────────────────────────────
        [HttpGet("compatibility/{pid}")]
        public IActionResult GetCompatibilityByPlant(int pid)
        {
            var data = db.COMPATIBILITies
                .Where(c => c.Pid == pid)
                .Include(c => c.Fr)
                .Include(c => c.PidNavigation)
                .Select(c => new DTOS.COMPATIBILITY
                {
                    FertilizerName = c.Fr.Fertilizer_name,
                    PlantName = c.PidNavigation.Name,
                    Rate = c.Rate
                })
                .ToList();

            return Ok(data);
        }

        // ─── GET api/plant/growin ─────────────────────────────────────────────
        [HttpGet("growin")]
        public IActionResult GetGrowsIn()
        {
            var data = db.GROWS_INs
                .Include(x => x.PidNavigation)
                .Include(x => x.SidNavigation)
                .Select(x => new Grows_in
                {
                    PlantName = x.PidNavigation.Name,
                    SeasonName = x.SidNavigation.Name,
                    Description = x.Plant_in_season_description,
                    Rate = x.Rate
                })
                .ToList();

            return Ok(data);
        }

        // ─── Helper ───────────────────────────────────────────────────────────
        private static string? ExtractPublicId(string url)
        {
            try
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/');
                var uploadIdx = Array.IndexOf(segments, "upload");
                if (uploadIdx < 0) return null;
                var start = uploadIdx + 1;
                if (start < segments.Length && segments[start].StartsWith('v') &&
                    long.TryParse(segments[start][1..], out _))
                    start++;
                var publicIdWithExt = string.Join("/", segments[start..]);
                var dot = publicIdWithExt.LastIndexOf('.');
                return dot >= 0 ? publicIdWithExt[..dot] : publicIdWithExt;
            }
            catch { return null; }
        }
    }
}
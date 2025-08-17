using FarmerTest.Helpers;
using FarmerTest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;

namespace FarmerTest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _ctx;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _ctx = context;
        }

        public IActionResult Index(string? q = null)
        {
            ViewBag.Q = q;
            return View();
        }

        public async Task<IActionResult> Table(string? q = null, int page = 1, int pageSize = 10)
        {
            var data = _ctx.Farmers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var keyword = q.Trim();
                var kwLower = keyword.ToLower();

                // Nếu người dùng nhập được định dạng ngày, cho phép lọc theo ngày tạo
                DateTime? ca = null;
                if (DateTime.TryParse(keyword, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    ca = d;

                data = data.Where(f =>
                    f.FarmerCode.ToLower().Contains(kwLower) ||
                    f.FarmerName.ToLower().Contains(kwLower) ||
                    (f.Phone1 != null && f.Phone1.ToLower().Contains(kwLower)) ||
                    (f.Phone2 != null && f.Phone2.ToLower().Contains(kwLower)) ||
                    (f.Address != null && f.Address.ToLower().Contains(kwLower)) ||
                    (ca != null && f.CreatedAt == ca)
                );
            }

            data = data.OrderByDescending(f => f.CreatedAt);

            var model = await PaginatedList<Farmer>.CreateAsync(data, page, pageSize);
            ViewBag.Q = q;
            return PartialView("_Table", model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateOrEdit(int? id)
        {
            if (id is null)
                return PartialView("_Upsert", new Farmer());

            var entity = await _ctx.Farmers.FindAsync(id.Value);
            if (entity is null) return NotFound();
            return PartialView("_Upsert", entity);
        }

        [HttpPost("CreateOrEdit/{id:int?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrEdit(int id, Farmer input)
        {
            if (!ModelState.IsValid)
                return StatusCode(400, await this.RenderPartialViewAsync("_Upsert", input));

            input.FarmerCode = (input.FarmerCode ?? "").Trim();
            input.FarmerName = (input.FarmerName ?? "").Trim();

            // 1) Tiền kiểm trùng mã (case-insensitive)
            var code = input.FarmerCode;
            var dup = await _ctx.Farmers
                .AsNoTracking()
                .AnyAsync(x => x.FarmerID != id && x.FarmerCode.ToUpper() == code.ToUpper());
            if (dup)
            {
                ModelState.AddModelError(nameof(Farmer.FarmerCode), "Mã nông dân đã tồn tại. Vui lòng nhập mã khác.");
                return StatusCode(400, await this.RenderPartialViewAsync("_Upsert", input));
            }

            if (id == 0)
            {
                _ctx.Farmers.Add(input);
            }
            else
            {
                var entity = await _ctx.Farmers.FindAsync(id);
                if (entity is null) return NotFound();

                entity.FarmerCode = input.FarmerCode;
                entity.FarmerName = input.FarmerName;
                entity.Phone1 = input.Phone1;
                entity.Phone2 = input.Phone2;
                entity.Address = input.Address;
                entity.UpdatedAt = DateTime.Now;
            }

            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", $"Lỗi khi lưu dữ liệu: {ex.GetBaseException().Message}");
                return StatusCode(400, await this.RenderPartialViewAsync("_Upsert", input));
            }

            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _ctx.Farmers.FindAsync(id);
            if (entity is null) return NotFound();
            _ctx.Farmers.Remove(entity);
            await _ctx.SaveChangesAsync();
            return Ok();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMany([FromForm] List<int> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("Danh sách rỗng.");

            try
            {
                await _ctx.Farmers.Where(f => ids.Contains(f.FarmerID)).ExecuteDeleteAsync();
                return Ok();
            }
            catch (Exception)
            {
                return StatusCode(500, "Không xoá được (có thể do ràng buộc dữ liệu).");
            }
        }
    }
}
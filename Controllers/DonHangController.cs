using CoffeeShopManager.Data;
using CoffeeShopManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoffeeShopManager.Controllers
{
    [Authorize]
    public class DonHangController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? LayMaNguoiDungHienTai()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int maNguoiDung))
            {
                return maNguoiDung;
            }

            return null;
        }

        private List<string> DanhSachTrangThai()
        {
            return new List<string>
            {
                "Chờ xử lý",
                "Đang chuẩn bị",
                "Hoàn thành",
                "Đã hủy"
            };
        }

        // ADMIN: Xem toàn bộ đơn hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var donHangs = await _context.DonHangs
                .Include(d => d.NguoiDung)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            ViewBag.DanhSachTrangThai = DanhSachTrangThai();

            return View(donHangs);
        }

        // ADMIN: Xem chi tiết đơn hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int? madonhang)
        {
            if (madonhang == null)
            {
                return NotFound();
            }

            var donhang = await _context.DonHangs
                .Include(d => d.NguoiDung)
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(m => m.MaDonHang == madonhang);

            if (donhang == null)
            {
                return NotFound();
            }

            ViewBag.DanhSachTrangThai = DanhSachTrangThai();

            return View(donhang);
        }

        // ADMIN: Cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CapNhatTrangThai(int madonhang, string trangThai)
        {
            var danhSachTrangThai = DanhSachTrangThai();

            if (!danhSachTrangThai.Contains(trangThai))
            {
                TempData["Error"] = "Trạng thái đơn hàng không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var donHang = await _context.DonHangs.FindAsync(madonhang);

            if (donHang == null)
            {
                return NotFound();
            }

            donHang.TrangThai = trangThai;

            _context.Update(donHang);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã cập nhật đơn hàng #{donHang.MaDonHang} sang trạng thái: {trangThai}.";

            return RedirectToAction(nameof(Index));
        }

        // USER: Xem đơn hàng của chính mình
        [Authorize(Roles = "User")]
        public async Task<IActionResult> CuaToi()
        {
            var maNguoiDung = LayMaNguoiDungHienTai();

            if (maNguoiDung == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var donHangs = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                .Where(d => d.MaNguoiDung == maNguoiDung)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(donHangs);
        }

        // USER: Xem chi tiết đơn hàng của chính mình
        [Authorize(Roles = "User")]
        public async Task<IActionResult> ChiTietCuaToi(int id)
        {
            var maNguoiDung = LayMaNguoiDungHienTai();

            if (maNguoiDung == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var donHang = await _context.DonHangs
                .Include(d => d.ChiTietDonHangs)
                    .ThenInclude(ct => ct.SanPham)
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaNguoiDung == maNguoiDung);

            if (donHang == null)
            {
                return NotFound();
            }

            return View(donHang);
        }

        // USER: Hủy đơn hàng của chính mình
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> HuyDon(int id)
        {
            var maNguoiDung = LayMaNguoiDungHienTai();

            if (maNguoiDung == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var donHang = await _context.DonHangs
                .FirstOrDefaultAsync(d => d.MaDonHang == id && d.MaNguoiDung == maNguoiDung);

            if (donHang == null)
            {
                return NotFound();
            }

            if (donHang.TrangThai != "Chờ xử lý")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn hàng đang ở trạng thái Chờ xử lý.";
                return RedirectToAction(nameof(CuaToi));
            }

            donHang.TrangThai = "Đã hủy";

            _context.Update(donHang);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bạn đã hủy đơn hàng thành công.";

            return RedirectToAction(nameof(CuaToi));
        }

        // ADMIN: Form tạo đơn hàng thủ công
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // ADMIN: Form sửa đơn hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? madonhang)
        {
            if (madonhang == null)
            {
                return NotFound();
            }

            var donhang = await _context.DonHangs.FindAsync(madonhang);

            if (donhang == null)
            {
                return NotFound();
            }

            return View(donhang);
        }

        // ADMIN: Xử lý sửa đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(
            int? madonhang,
            [Bind("MaDonHang,TenKhachHang,SoDienThoai,NgayDat,TongTien,TrangThai,GhiChu,MaSP,MaNguoiDung")] DonHang donhang)
        {
            if (madonhang != donhang.MaDonHang)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(donhang);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonHangExists(donhang.MaDonHang))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(donhang);
        }

        // ADMIN: Form xóa đơn hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? madonhang)
        {
            if (madonhang == null)
            {
                return NotFound();
            }

            var donhang = await _context.DonHangs
                .FirstOrDefaultAsync(m => m.MaDonHang == madonhang);

            if (donhang == null)
            {
                return NotFound();
            }

            return View(donhang);
        }

        // ADMIN: Xử lý xóa đơn hàng
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int? madonhang)
        {
            var donhang = await _context.DonHangs.FindAsync(madonhang);

            if (donhang != null)
            {
                _context.DonHangs.Remove(donhang);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DonHangExists(int? madonhang)
        {
            return _context.DonHangs.Any(e => e.MaDonHang == madonhang);
        }
    }
}
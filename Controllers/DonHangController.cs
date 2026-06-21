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

        // ADMIN: Xem toàn bộ đơn hàng
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var donHangs = await _context.DonHangs
                .Include(d => d.NguoiDung)
                .OrderByDescending(d => d.NgayDat)
                .ToListAsync();

            return View(donHangs);
        }

        // ADMIN: Xem chi tiết một đơn hàng
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

            return View(donhang);
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

        // ADMIN: Tạo đơn hàng thủ công
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // ADMIN: Tạo đơn hàng thủ công từ sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? sanPhamId)
        {
            var donHangMoi = new DonHang();

            if (sanPhamId != null)
            {
                var sanPham = await _context.SanPhams.FindAsync(sanPhamId);

                if (sanPham != null)
                {
                    donHangMoi.MaSP = sanPham.MaSP;
                    donHangMoi.TongTien = (double)sanPham.Gia;
                    donHangMoi.GhiChu = "Món đặt: " + sanPham.TenSP;
                }
            }

            return View(donHangMoi);
        }

        // ADMIN: Sửa đơn hàng
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

        // ADMIN: Xóa đơn hàng
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int? madonhang)
        {
            var donhang = await _context.DonHangs.FindAsync(madonhang);

            if (donhang != null)
            {
                _context.DonHangs.Remove(donhang);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool DonHangExists(int? madonhang)
        {
            return _context.DonHangs.Any(e => e.MaDonHang == madonhang);
        }
    }
}
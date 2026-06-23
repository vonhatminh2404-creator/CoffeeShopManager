using CoffeeShopManager.Data;
using CoffeeShopManager.Models;
using CoffeeShopManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoffeeShopManager.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DASHBOARD ADMIN
        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TongSanPham = await _context.SanPhams.CountAsync(),

                SanPhamDangHien = await _context.SanPhams.CountAsync(s => !s.IsAn),

                SanPhamDangAn = await _context.SanPhams.CountAsync(s => s.IsAn),

                TongDonHang = await _context.DonHangs.CountAsync(),

                DonChoXuLy = await _context.DonHangs.CountAsync(d => d.TrangThai == "Chờ xử lý"),

                DonDaHuy = await _context.DonHangs.CountAsync(d => d.TrangThai == "Đã hủy"),

                DonHoanThanh = await _context.DonHangs.CountAsync(d => d.TrangThai == "Hoàn thành"),

                DoanhThuTamTinh = await _context.DonHangs
                    .Where(d => d.TrangThai != "Đã hủy")
                    .SumAsync(d => (double?)d.TongTien) ?? 0,

                TongNguoiDung = await _context.NguoiDungs.CountAsync(),

                TongTaiKhoanUser = await _context.NguoiDungs.CountAsync(n => n.VaiTro == "User"),

                TongTaiKhoanAdmin = await _context.NguoiDungs.CountAsync(n => n.VaiTro == "Admin"),

                DonHangGanDay = await _context.DonHangs
                    .Include(d => d.NguoiDung)
                    .OrderByDescending(d => d.NgayDat)
                    .Take(8)
                    .ToListAsync()
            };

            return View(model);
        }

        // QUẢN LÝ SẢN PHẨM
        public async Task<IActionResult> Index()
        {
            var danhSachSanPham = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .OrderBy(s => s.IsAn)
                .ThenBy(s => s.TenSP)
                .ToListAsync();

            return View(danhSachSanPham);
        }

        // CHI TIẾT SẢN PHẨM
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .FirstOrDefaultAsync(m => m.MaSP == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }

        // FORM THÊM SẢN PHẨM
        public IActionResult Create()
        {
            ViewBag.DanhSachMenu = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc");
            return View();
        }

        // XỬ LÝ THÊM SẢN PHẨM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SanPham sanPham)
        {
            ModelState.Remove("DanhMuc");

            if (ModelState.IsValid)
            {
                var monMoi = new SanPham
                {
                    TenSP = sanPham.TenSP,
                    Gia = sanPham.Gia,
                    HinhAnh = sanPham.HinhAnh,
                    MaDanhMuc = sanPham.MaDanhMuc,
                    IsAn = false
                };

                _context.Add(monMoi);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.DanhSachMenu = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
            return View(sanPham);
        }

        // FORM SỬA SẢN PHẨM
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams.FindAsync(id);

            if (sanPham == null)
            {
                return NotFound();
            }

            ViewBag.DanhSachMenu = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
            return View(sanPham);
        }

        // XỬ LÝ SỬA SẢN PHẨM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SanPham sanPham)
        {
            if (id != sanPham.MaSP)
            {
                return NotFound();
            }

            ModelState.Remove("DanhMuc");

            if (ModelState.IsValid)
            {
                try
                {
                    var sanPhamCu = await _context.SanPhams
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.MaSP == id);

                    if (sanPhamCu != null)
                    {
                        sanPham.IsAn = sanPhamCu.IsAn;
                    }

                    _context.Update(sanPham);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanPhamExists(sanPham.MaSP))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.DanhSachMenu = new SelectList(_context.DanhMucs, "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
            return View(sanPham);
        }

        // FORM XÓA SẢN PHẨM
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sanPham = await _context.SanPhams
                .Include(s => s.DanhMuc)
                .FirstOrDefaultAsync(m => m.MaSP == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            return View(sanPham);
        }

        // XỬ LÝ XÓA SẢN PHẨM
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);

            if (sanPham != null)
            {
                _context.SanPhams.Remove(sanPham);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ẨN / HIỆN SẢN PHẨM
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnHien(int id)
        {
            var sanPham = await _context.SanPhams.FindAsync(id);

            if (sanPham == null)
            {
                return NotFound();
            }

            sanPham.IsAn = !sanPham.IsAn;

            _context.Update(sanPham);
            await _context.SaveChangesAsync();

            TempData["Success"] = sanPham.IsAn
                ? "Sản phẩm đã được ẩn khỏi trang người dùng."
                : "Sản phẩm đã được hiển thị lại trên trang người dùng.";

            return RedirectToAction(nameof(Details), new { id = id });
        }

        private bool SanPhamExists(int id)
        {
            return _context.SanPhams.Any(e => e.MaSP == id);
        }
    }
}
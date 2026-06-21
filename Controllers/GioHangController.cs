using CoffeeShopManager.Data;
using CoffeeShopManager.Models;
using CoffeeShopManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CoffeeShopManager.Controllers
{
    [Authorize]
    public class GioHangController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string GIO_HANG_KEY = "GioHang";

        public GioHangController(ApplicationDbContext context)
        {
            _context = context;
        }

        private List<int> LayDanhSachIdTrongGio()
        {
            var gioHang = HttpContext.Session.GetString(GIO_HANG_KEY) ?? "";

            if (string.IsNullOrWhiteSpace(gioHang))
            {
                return new List<int>();
            }

            return gioHang
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
        }

        private void LuuDanhSachIdVaoGio(List<int> danhSachId)
        {
            HttpContext.Session.SetString(GIO_HANG_KEY, string.Join(",", danhSachId));
        }

        private async Task<List<GioHangItemViewModel>> TaoDanhSachGioHang()
        {
            var danhSachId = LayDanhSachIdTrongGio();

            if (!danhSachId.Any())
            {
                return new List<GioHangItemViewModel>();
            }

            var nhomSoLuong = danhSachId
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            var ids = nhomSoLuong.Keys.ToList();

            var sanPhams = await _context.SanPhams
                .Where(s => ids.Contains(s.MaSP) && !s.IsAn)
                .ToListAsync();

            var gioHang = sanPhams.Select(sp => new GioHangItemViewModel
            {
                MaSP = sp.MaSP,
                TenSP = sp.TenSP ?? "",
                Gia = sp.Gia,
                HinhAnh = sp.HinhAnh,
                SoLuong = nhomSoLuong[sp.MaSP]
            }).ToList();

            return gioHang;
        }

        public async Task<IActionResult> Index()
        {
            var gioHang = await TaoDanhSachGioHang();

            ViewBag.TongTien = gioHang.Sum(x => x.ThanhTien);
            ViewBag.TongSoLuong = gioHang.Sum(x => x.SoLuong);

            return View(gioHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemVaoGio(int id)
        {
            var sanPham = await _context.SanPhams
                .FirstOrDefaultAsync(s => s.MaSP == id && !s.IsAn);

            if (sanPham == null)
            {
                return NotFound();
            }

            var danhSachId = LayDanhSachIdTrongGio();
            danhSachId.Add(id);

            LuuDanhSachIdVaoGio(danhSachId);

            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CapNhat(int id, int soLuong)
        {
            var danhSachId = LayDanhSachIdTrongGio();

            danhSachId.RemoveAll(x => x == id);

            if (soLuong > 0)
            {
                for (int i = 0; i < soLuong; i++)
                {
                    danhSachId.Add(id);
                }
            }

            LuuDanhSachIdVaoGio(danhSachId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Xoa(int id)
        {
            var danhSachId = LayDanhSachIdTrongGio();

            danhSachId.RemoveAll(x => x == id);

            LuuDanhSachIdVaoGio(danhSachId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XoaTatCa()
        {
            HttpContext.Session.Remove(GIO_HANG_KEY);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> DatHang()
        {
            var gioHang = await TaoDanhSachGioHang();

            if (!gioHang.Any())
            {
                TempData["Error"] = "Giỏ hàng đang trống, vui lòng chọn sản phẩm trước.";
                return RedirectToAction("Index");
            }

            ViewBag.GioHang = gioHang;
            ViewBag.TongTien = gioHang.Sum(x => x.ThanhTien);

            var donHang = new DonHang
            {
                TenKhachHang = User.Identity?.Name,
                TrangThai = "Chờ xử lý"
            };

            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DatHang(string tenKhachHang, string soDienThoai, string? ghiChu)
        {
            var gioHang = await TaoDanhSachGioHang();

            if (!gioHang.Any())
            {
                TempData["Error"] = "Giỏ hàng đang trống, vui lòng chọn sản phẩm trước.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(tenKhachHang))
            {
                ModelState.AddModelError("TenKhachHang", "Vui lòng nhập tên khách hàng");
            }

            if (string.IsNullOrWhiteSpace(soDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Vui lòng nhập số điện thoại");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.GioHang = gioHang;
                ViewBag.TongTien = gioHang.Sum(x => x.ThanhTien);

                var model = new DonHang
                {
                    TenKhachHang = tenKhachHang,
                    SoDienThoai = soDienThoai,
                    GhiChu = ghiChu,
                    TrangThai = "Chờ xử lý"
                };

                return View(model);
            }

            int? maNguoiDung = null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out int idNguoiDung))
            {
                maNguoiDung = idNguoiDung;
            }

            var tongTien = gioHang.Sum(x => x.ThanhTien);

            var donHang = new DonHang
            {
                TenKhachHang = tenKhachHang,
                SoDienThoai = soDienThoai,
                NgayDat = DateTime.Now,
                TongTien = (double)tongTien,
                TrangThai = "Chờ xử lý",
                GhiChu = ghiChu ?? "",
                MaNguoiDung = maNguoiDung
            };

            _context.DonHangs.Add(donHang);
            await _context.SaveChangesAsync();

            foreach (var item in gioHang)
            {
                var chiTiet = new ChiTietDonHang
                {
                    MaDon = donHang.MaDonHang,
                    MaSP = item.MaSP,
                    SoLuong = item.SoLuong,
                    GiaBan = item.Gia
                };

                _context.ChiTietDonHangs.Add(chiTiet);
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(GIO_HANG_KEY);

            TempData["Success"] = "Đặt hàng thành công.";

            return RedirectToAction("CuaToi", "DonHang");
        }
    }
}
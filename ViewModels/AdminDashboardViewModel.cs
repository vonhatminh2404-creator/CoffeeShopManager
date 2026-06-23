using CoffeeShopManager.Models;

namespace CoffeeShopManager.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TongSanPham { get; set; }

        public int SanPhamDangHien { get; set; }

        public int SanPhamDangAn { get; set; }

        public int TongDonHang { get; set; }

        public int DonChoXuLy { get; set; }

        public int DonDaHuy { get; set; }

        public int DonHoanThanh { get; set; }

        public double DoanhThuTamTinh { get; set; }

        public int TongNguoiDung { get; set; }

        public int TongTaiKhoanUser { get; set; }

        public int TongTaiKhoanAdmin { get; set; }

        public List<DonHang> DonHangGanDay { get; set; } = new List<DonHang>();
    }
}
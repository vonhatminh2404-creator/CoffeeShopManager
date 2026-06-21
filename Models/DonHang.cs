using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeeShopManager.Models
{
    public class DonHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDonHang { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách")]
        public string? TenKhachHang { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string? SoDienThoai { get; set; }

        public DateTime NgayDat { get; set; } = DateTime.Now;

        public double TongTien { get; set; }

        public string TrangThai { get; set; } = "Chờ xử lý";

        public string? GhiChu { get; set; } = "";

        public int? MaSP { get; set; }

        // Liên kết đơn hàng với tài khoản người dùng
        public int? MaNguoiDung { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }

        public virtual ICollection<ChiTietDonHang> ChiTietDonHangs { get; set; } = new List<ChiTietDonHang>();
    }
}
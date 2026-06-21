namespace CoffeeShopManager.ViewModels
{
    public class GioHangItemViewModel
    {
        public int MaSP { get; set; }

        public string TenSP { get; set; } = "";

        public decimal Gia { get; set; }

        public string? HinhAnh { get; set; }

        public int SoLuong { get; set; }

        public decimal ThanhTien
        {
            get
            {
                return Gia * SoLuong;
            }
        }
    }
}
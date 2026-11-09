namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderName { get; set; } = "SPORTS GROUND";
        public string SenderEmail { get; set; } = "";
        public string Password { get; set; } = "";
    }
}

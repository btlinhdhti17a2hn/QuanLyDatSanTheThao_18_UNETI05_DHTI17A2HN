namespace QuanLyDatSanTheThao_18_UNETI05_DHTI17A2HN.Services
{
    public interface IEmailService
    {
        Task GuiEmailAsync(string denEmail, string tieuDe, string noiDungHtml);
    }
}

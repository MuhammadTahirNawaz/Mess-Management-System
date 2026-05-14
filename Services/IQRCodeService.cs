namespace Semester_Project.Services
{
    public interface IQRCodeService
    {
        string GenerateQRCodeBase64(string data);
    }
}

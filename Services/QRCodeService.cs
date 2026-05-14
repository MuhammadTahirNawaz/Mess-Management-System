using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace Semester_Project.Services
{
    public class QRCodeService : IQRCodeService
    {
        public string GenerateQRCodeBase64(string data)
        {
            try
            {
                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                {
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
                    using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                    {
                        byte[] qrCodeBytes = qrCode.GetGraphic(20);
                        return Convert.ToBase64String(qrCodeBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating QR code: {ex.Message}", ex);
            }
        }
    }
}

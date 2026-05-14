using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Microsoft.EntityFrameworkCore;
using Semester_Project.Data;
using System.Drawing;
using System.Text.Json;

namespace Semester_Project.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FaceRecognitionService> _logger;
        private readonly CascadeClassifier _faceCascade;

        public FaceRecognitionService(ApplicationDbContext context, ILogger<FaceRecognitionService> logger)
        {
            _context = context;
            _logger = logger;
            
            // Initialize face cascade classifier for face detection
            var cascadePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");
            
            // If cascade file doesn't exist, download it
            if (!File.Exists(cascadePath))
            {
                DownloadHaarCascade(cascadePath);
            }
            
            _faceCascade = new CascadeClassifier(cascadePath);
        }

        private void DownloadHaarCascade(string path)
        {
            try
            {
                var url = "https://raw.githubusercontent.com/opencv/opencv/master/data/haarcascades/haarcascade_frontalface_default.xml";
                using var client = new HttpClient();
                var data = client.GetByteArrayAsync(url).Result;
                File.WriteAllBytes(path, data);
                _logger.LogInformation("Downloaded Haar Cascade file successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download Haar Cascade file");
            }
        }

        public async Task<string?> EncodeFaceFromBase64(string base64Image)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Remove data URL prefix if present
                    if (base64Image.Contains(","))
                    {
                        base64Image = base64Image.Split(',')[1];
                    }

                    byte[] imageBytes = Convert.FromBase64String(base64Image);
                    
                    using var ms = new MemoryStream(imageBytes);
                    using var bitmap = new Bitmap(ms);
                    
                    // Convert Bitmap to Emgu Image
                    var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    var bitmapData = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);
                    var image = new Image<Bgr, byte>(bitmap.Width, bitmap.Height, bitmapData.Stride, bitmapData.Scan0);
                    bitmap.UnlockBits(bitmapData);
                    
                    using var grayImage = image.Convert<Gray, byte>();

                    // Detect faces
                    var faces = _faceCascade.DetectMultiScale(grayImage, 1.1, 3, Size.Empty);

                    if (faces.Length == 0)
                    {
                        _logger.LogWarning("No face detected in image");
                        return null;
                    }

                    // Get the largest face
                    var face = faces.OrderByDescending(f => f.Width * f.Height).First();
                    
                    _logger.LogInformation($"Face detected - Size: {face.Width}x{face.Height}");
                    
                    // Crop face region
                    grayImage.ROI = face;
                    var croppedFace = grayImage.Copy();
                    grayImage.ROI = Rectangle.Empty;
                    
                    // Resize to fixed size
                    var resizedFace = croppedFace.Resize(100, 100, Inter.Cubic);
                    
                    // Apply histogram equalization for lighting normalization
                    CvInvoke.EqualizeHist(resizedFace, resizedFace);

                    // Convert image data to base64
                    var imageData = resizedFace.ToJpegData();
                    var faceImageBase64 = Convert.ToBase64String(imageData);

                    _logger.LogInformation($"Face encoding generated successfully (size: {faceImageBase64.Length} chars)");

                    return faceImageBase64;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error encoding face from image");
                    return null;
                }
            });
        }

        public async Task<bool> CompareFaces(string encoding1, string encoding2, double threshold = 0.85)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Convert base64 back to images
                    var img1Bytes = Convert.FromBase64String(encoding1);
                    var img2Bytes = Convert.FromBase64String(encoding2);

                    using var ms1 = new MemoryStream(img1Bytes);
                    using var ms2 = new MemoryStream(img2Bytes);
                    using var bitmap1 = new Bitmap(ms1);
                    using var bitmap2 = new Bitmap(ms2);

                    // Convert to grayscale Emgu images
                    var rect1 = new Rectangle(0, 0, bitmap1.Width, bitmap1.Height);
                    var rect2 = new Rectangle(0, 0, bitmap2.Width, bitmap2.Height);
                    
                    var bitmapData1 = bitmap1.LockBits(rect1, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap1.PixelFormat);
                    var bitmapData2 = bitmap2.LockBits(rect2, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap2.PixelFormat);
                    
                    var image1 = new Image<Bgr, byte>(bitmap1.Width, bitmap1.Height, bitmapData1.Stride, bitmapData1.Scan0);
                    var image2 = new Image<Bgr, byte>(bitmap2.Width, bitmap2.Height, bitmapData2.Stride, bitmapData2.Scan0);
                    
                    bitmap1.UnlockBits(bitmapData1);
                    bitmap2.UnlockBits(bitmapData2);

                    var gray1 = image1.Convert<Gray, byte>();
                    var gray2 = image2.Convert<Gray, byte>();

                    // Calculate similarity using Mean Squared Error
                    double totalDiff = 0;
                    int pixelCount = gray1.Width * gray1.Height;

                    for (int y = 0; y < gray1.Height; y++)
                    {
                        for (int x = 0; x < gray1.Width; x++)
                        {
                            double diff = gray1.Data[y, x, 0] - gray2.Data[y, x, 0];
                            totalDiff += diff * diff;
                        }
                    }

                    double mse = totalDiff / pixelCount;
                    double similarity = 1.0 - (Math.Sqrt(mse) / 255.0); // Normalize to 0-1

                    _logger.LogInformation($"Face similarity: {similarity:P2} (threshold: {threshold:P2})");

                    return similarity >= threshold;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error comparing faces");
                    return false;
                }
            });
        }

        public async Task<int?> RecognizeStudentFromImage(string base64Image)
        {
            try
            {
                var encoding = await EncodeFaceFromBase64(base64Image);
                if (encoding == null)
                {
                    _logger.LogWarning("Failed to encode face from image");
                    return null;
                }

                // Get all students with registered faces
                var students = await _context.Students
                    .Where(s => s.IsFaceRegistered && s.FaceEncoding != null)
                    .ToListAsync();

                _logger.LogInformation($"Comparing against {students.Count} registered face(s)");

                // Find best match
                int? bestMatchId = null;
                double bestSimilarity = 0;

                foreach (var student in students)
                {
                    try
                    {
                        // Convert base64 strings to images and compare
                        var img1Bytes = Convert.FromBase64String(encoding);
                        var img2Bytes = Convert.FromBase64String(student.FaceEncoding!);

                        using var ms1 = new MemoryStream(img1Bytes);
                        using var ms2 = new MemoryStream(img2Bytes);
                        using var bitmap1 = new Bitmap(ms1);
                        using var bitmap2 = new Bitmap(ms2);

                        var rect1 = new Rectangle(0, 0, bitmap1.Width, bitmap1.Height);
                        var rect2 = new Rectangle(0, 0, bitmap2.Width, bitmap2.Height);
                        
                        var bitmapData1 = bitmap1.LockBits(rect1, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap1.PixelFormat);
                        var bitmapData2 = bitmap2.LockBits(rect2, System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap2.PixelFormat);
                        
                        var image1 = new Image<Bgr, byte>(bitmap1.Width, bitmap1.Height, bitmapData1.Stride, bitmapData1.Scan0);
                        var image2 = new Image<Bgr, byte>(bitmap2.Width, bitmap2.Height, bitmapData2.Stride, bitmapData2.Scan0);
                        
                        bitmap1.UnlockBits(bitmapData1);
                        bitmap2.UnlockBits(bitmapData2);

                        var gray1 = image1.Convert<Gray, byte>();
                        var gray2 = image2.Convert<Gray, byte>();

                        // Calculate similarity
                        double totalDiff = 0;
                        int pixelCount = gray1.Width * gray1.Height;

                        for (int y = 0; y < gray1.Height; y++)
                        {
                            for (int x = 0; x < gray1.Width; x++)
                            {
                                double diff = gray1.Data[y, x, 0] - gray2.Data[y, x, 0];
                                totalDiff += diff * diff;
                            }
                        }

                        double mse = totalDiff / pixelCount;
                        double similarity = 1.0 - (Math.Sqrt(mse) / 255.0);

                        _logger.LogInformation($"  [{student.Name} ({student.PermanentID})] Similarity: {similarity:P2}");

                        if (similarity > bestSimilarity && similarity >= 0.75) // 75% threshold
                        {
                            bestSimilarity = similarity;
                            bestMatchId = student.Id;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error comparing with student {student.Name}");
                    }
                }

                if (bestMatchId.HasValue)
                {
                    var matchedStudent = students.First(s => s.Id == bestMatchId);
                    _logger.LogInformation($"✓ MATCH FOUND: {matchedStudent.Name} ({matchedStudent.PermanentID}) - Similarity: {bestSimilarity:P2}");
                }
                else
                {
                    _logger.LogWarning($"✗ NO MATCH - Best similarity: {bestSimilarity:P2} (need 75%)");
                }

                return bestMatchId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recognizing student from image");
                return null;
            }
        }
    }
}

namespace Semester_Project.Services
{
    public interface IFaceRecognitionService
    {
        Task<string?> EncodeFaceFromBase64(string base64Image);
        Task<bool> CompareFaces(string encoding1, string encoding2, double threshold = 0.6);
        Task<int?> RecognizeStudentFromImage(string base64Image);
    }
}

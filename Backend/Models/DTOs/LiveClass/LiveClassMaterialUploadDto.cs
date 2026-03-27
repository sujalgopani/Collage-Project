namespace ExamNest.Models.DTOs.LiveClass
{
    public class LiveClassMaterialUploadDto
    {
        public string? MaterialTitle { get; set; }
        public string? MaterialDescription { get; set; }
        public string? MaterialLink { get; set; }
        public IFormFile? MaterialFile { get; set; }
    }
}

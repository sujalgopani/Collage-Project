using Microsoft.AspNetCore.Http;

namespace ExamNest.Models.DTOs
{
    public class UpdateProfileImageDto
    {
        public IFormFile? ProfileImage { get; set; }
    }
}

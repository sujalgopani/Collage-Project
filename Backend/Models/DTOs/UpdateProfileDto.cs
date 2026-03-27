namespace ExamNest.Models.DTOs
{
    public class UpdateProfileDto
    {
		public string FirstName { get; set; } = string.Empty;
		public string? MiddleName { get; set; }
		public string LastName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string? Phone { get; set; }
	}
}

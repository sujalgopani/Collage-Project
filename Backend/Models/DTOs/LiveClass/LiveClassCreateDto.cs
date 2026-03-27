namespace ExamNest.Models.DTOs.LiveClass
{
	public class LiveClassCreateDto
	{
		public int CourseId { get; set; }
		public string Title { get; set; } = "";
		public string? Agenda { get; set; }
		public string MeetingLink { get; set; } = "";

		// ? Keep DateTimeOffset (BEST)
		public DateTimeOffset StartAt { get; set; }
		public DateTimeOffset EndAt { get; set; }
	}
}

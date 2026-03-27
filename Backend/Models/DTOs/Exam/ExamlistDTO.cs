namespace ExamNest.Models.DTOs.Exam
{
    public class ExamlistDTO
    {
        public int ExamId { get; set; }
        public int CourseId { get; set; }
        public string ?TeacherName { get; set; }
        public string? tittle { get; set; }
        public string? Description { get; set; }
        public int DurationMinit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Enddate { get; set; }
		public DateTime Startdate { get; set; }
        public bool ?Isflagged { get; set; }

    }
}

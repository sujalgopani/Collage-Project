using System.Text.Json.Serialization;

namespace ExamNest.Models
{
    public class CourseMedia
    {
        public int CourseMediaId { get; set; }

        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileType { get; set; } = ""; // video / image

        public int CourseId { get; set; }

		[JsonIgnore]
		public Course? Course { get; set; }

    }
}

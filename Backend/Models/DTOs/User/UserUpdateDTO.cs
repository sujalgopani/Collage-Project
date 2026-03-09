namespace ExamNest.Models.DTOs.User
{
    public class UserUpdateDTO
    {
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Newpassword { get; set; } = "";

        public string Phone { get; set; } = "";

        public bool IsActive { get; set; }

    }
}

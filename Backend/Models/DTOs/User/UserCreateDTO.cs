namespace ExamNest.Models.DTOs.User
{
    public class UserCreateDTO
    {
        public string FirstName { get; set; } = "";
        public string MiddleName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        //public string Username { get; set; } = ""; // auto geenerte
        public string Password { get; set; } = "";
        public string Phone { get; set; } = "";
    }
}

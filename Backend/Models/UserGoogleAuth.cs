using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamNest.Models
{
    [Table("user_google_auth")]
    public class UserGoogleAuth
    {
        [Key]
        [Column("google_auth_id")]
        public int GoogleAuthId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [StringLength(100)]
        [Column("google_sub")]
        public string GoogleSub { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        [Column("google_email")]
        public string GoogleEmail { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}

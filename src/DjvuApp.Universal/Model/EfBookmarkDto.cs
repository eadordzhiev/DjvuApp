using System.ComponentModel.DataAnnotations;

namespace DjvuApp.Model
{
    public sealed class EfBookmarkDto
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        public uint PageNumber { get; set; }

        [Required]
        public EfBookDto EfBookDto { get; set; }
    }
}
using System.ComponentModel.DataAnnotations.Schema; 

namespace SixteenSounds.Models
{
    public class Sample
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty; // Ścieżka do pliku na serwerze
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

    
        [NotMapped] 
        public string FileUrl { get; set; } = string.Empty;
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();

        public string CreatedBy { get; set; } = "User1";
        public bool IsPublic { get; set; } = true;
    }
}


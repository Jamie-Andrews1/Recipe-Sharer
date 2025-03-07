using System.ComponentModel.DataAnnotations;

namespace Blogs.Models;

public class Blog

{
    public int Id { get; set; }

    public string? ImagePath { get; set; }
    [StringLength(60, MinimumLength = 3)]
    [Required]
    public string? Title { get; set; }
    [StringLength(400, MinimumLength = 3)]
    [Required]
    public string? Description { get; set; }
}
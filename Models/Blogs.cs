using System.ComponentModel.DataAnnotations;
using Markdig;
using Microsoft.AspNetCore.Html;
using Users.Models;

namespace Blogs.Models;

public class Blog

{
    public int Id { get; set; }

    public string? ImagePath { get; set; }
    [StringLength(60, MinimumLength = 3)]
    [Required]
    public string? Title { get; set; }
    [StringLength(5000, MinimumLength = 3)]
    [Required]
    public string? Description { get; set; }

    public DateTime DateCreated { get; set; }

    public required string UserId
    { get; set; }

    public required User User { get; set; }


    public static string ParseMarkdown(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        return Markdown.ToHtml(markdown, pipeline);
    }
};

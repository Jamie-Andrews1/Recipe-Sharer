using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Blogs.Models;

public class Blog
{
    public int Id {get; set;}
    public string? ImagePath {get; set;}
    public string? Title {get; set;}
    public string? Description {get; set;}
}
using System.ComponentModel.DataAnnotations;

namespace MySolution.MyBlazorWASM.Models;

public class Contact
{
    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Birth Date")]
    [DataType(DataType.Date)]
    public DateTime? BirthDate { get; set; }

    [Display(Name = "Message")]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;

    [Display(Name = "Allow Contact")]
    public bool AllowContact { get; set; } = true;
}

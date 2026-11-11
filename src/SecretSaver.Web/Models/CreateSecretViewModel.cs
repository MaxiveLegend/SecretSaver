using System.ComponentModel.DataAnnotations;

namespace SecretSaver.Web.Models;

public class CreateSecretViewModel
{
    [Required(ErrorMessage = "Enter the password or secret you want to share.")]
    [StringLength(10_000, ErrorMessage = "The secret can be at most {1} characters long.")]
    [Display(Name = "Secret")]
    public string? Secret { get; set; }

    [Range(1, 168, ErrorMessage = "Choose a lifetime between 1 hour and 7 days.")]
    [Display(Name = "Expires after")]
    public int ExpiresInHours { get; set; } = 24;

    [Range(1, 10, ErrorMessage = "The number of views must be between {1} and {2}.")]
    [Display(Name = "Number of views")]
    public int MaxViews { get; set; } = 1;
}

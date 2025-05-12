using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Model;

public class NewLoan
{
    [Required(ErrorMessage = "User name is required")]
    public string UserName { get; set; }
    
    [Required(ErrorMessage = "ISBN is required")]
    public string Isbn { get; set; }
}
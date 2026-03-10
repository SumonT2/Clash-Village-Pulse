using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ClashVillagePulse.Web.ViewModels;

public sealed class VillageUploadViewModel
{
    [Display(Name = "Paste JSON")]
    public string? JsonText { get; set; }

    [Display(Name = "Upload JSON file")]
    public IFormFile? JsonFile { get; set; }
}
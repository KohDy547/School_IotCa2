using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CA2_Web.Pages
{
    public class IndexModel : PageModel
    {
        [TempData]
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public int LoaderFade = 800;
        public int ContentFade = 500;

        public void OnGet()
        {

        }
    }
}

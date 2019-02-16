using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Claims;

namespace CA2_Web.Pages.Location
{
    public class IndexModel : PageModel
    {
        [TempData]
        public string SearchedQuery { get; set; }
        public bool Searching => !string.IsNullOrEmpty(SearchedQuery);

        [TempData]
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public int LoaderFade = 800;
        public int ContentFade = 500;
        public bool IsAdmin = false;
        public string ImgBaseUrl { get; set; }
        public Models.Location[] Locations = new Models.Location[0];

        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        public IndexModel(
            ApplicationDbContext ApplicationDbContext,
            IOptions<AwsS3Configurations> AwsS3Configurations,
            IHttpContextAccessor IHttpContextAccessor)
        {
            _ApplicationDbContext = ApplicationDbContext;
            _AwsS3Configurations = AwsS3Configurations.Value;
            _IHttpContextAccessor = IHttpContextAccessor;

            ImgBaseUrl =_AwsS3Configurations.Locations_ImgBaseUrl;
        }

        public void OnGet()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    string currentUserId = 
                        _IHttpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                    int currentUserAl = 
                        _ApplicationDbContext.UserProperties
                        .Where(x => x.Id == currentUserId)
                        .Select(x => x.AccessLevel)
                        .First();
                    IsAdmin = currentUserAl >= 5;
                }

                if (Searching)
                {
                    string query = SearchedQuery;
                    string queryLower = query.ToLower();
                    try
                    {
                        Locations = _ApplicationDbContext.Locations.Where(x =>
                        x.Name.ToLower().Contains(queryLower) ||
                        x.Description.ToLower().Contains(queryLower))
                        .ToArray();
                        if (Locations.Length == 0) Message = "alert alert-warning|'" + query + "' returned no result.";
                        else Message = "alert alert-success|'" + query + "' returned " + Locations.Length + " result" +
                            ((Locations.Length > 1) ? "s." : ".");
                    }
                    catch (Exception)
                    {
                        Message = "alert alert-danger|Search failed.";
                    }
                }
                else
                {
                    Random rnd = new Random();
                    Locations = _ApplicationDbContext.Locations.OrderBy(x => rnd.Next()).ToArray();
                }
            }
            catch
            {
                Message = "alert alert-danger|Failed to load locations.";
            }
        }
        public IActionResult OnPost()
        {
            string inputQuery = Request.Form["txt_searchLocation"];
            SearchedQuery = inputQuery;
            return RedirectToPage("Index");
        }
    }
}
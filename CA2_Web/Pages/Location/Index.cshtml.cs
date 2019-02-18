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

        public string CurrentUserId = "";
        public string CurrentUserName = "";
        public int CurrentUserAl = 0;

        public string ImgBaseUrl { get; set; }
        public Models.Location[] Locations = new Models.Location[0];

        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly IoTConfigurations _IoTConfigurations;
        public IndexModel(
            IHttpContextAccessor IHttpContextAccessor,
            ApplicationDbContext ApplicationDbContext,
            IOptions<IoTConfigurations> IoTConfigurations)
        {
            _IHttpContextAccessor = IHttpContextAccessor;
            _ApplicationDbContext = ApplicationDbContext;
            _IoTConfigurations = IoTConfigurations.Value;

            ImgBaseUrl = _IoTConfigurations.AwsS3_LocationsImgBaseUrl;
        }

        public void OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                string[] currentUserInfo =
                    _ApplicationDbContext.UserProperties
                    .Where(
                        x => x.Id == _IHttpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value)
                    .Select(x => new string[] {
                        x.Id,
                        x.Email,
                        x.AccessLevel.ToString()
                    })
                    .First();

                CurrentUserId = currentUserInfo[0];
                CurrentUserName = currentUserInfo[1];
                CurrentUserAl = Int32.Parse(currentUserInfo[2]);
            }
            try
            {
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
                    catch
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
            string inputQuery = Request.Form["txt_inputSearchLocation"];
            SearchedQuery = inputQuery;
            return RedirectToPage("Index");
        }
    }
}
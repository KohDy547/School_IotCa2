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
    public class DetailsModel : PageModel
    {
        [TempData]
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public int LoaderFade = 800;
        public int ContentFade = 500;
        public bool IsAdmin = false;
        public string ImgBaseUrl { get; set; }
        public Models.Location Location = new Models.Location();
        public Models.Room[] Rooms = new Models.Room[0];

        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        public DetailsModel(
            ApplicationDbContext ApplicationDbContext,
            IOptions<AwsS3Configurations> AwsS3Configurations,
            IHttpContextAccessor IHttpContextAccessor)
        {
            _ApplicationDbContext = ApplicationDbContext;
            _AwsS3Configurations = AwsS3Configurations.Value;
            _IHttpContextAccessor = IHttpContextAccessor;

            ImgBaseUrl = _AwsS3Configurations.Locations_ImgBaseUrl;
        }

        public void OnGet(string targetId)
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

                Location = _ApplicationDbContext.Locations
                    .Where(x => x.Id == targetId)
                    .First();

                Rooms = _ApplicationDbContext.Rooms
                    .Where(x => x.LocationId == Location.Id)
                    .OrderBy(x => x.Name)
                    .ToArray();
            }
            catch
            {
                Message = "alert alert-danger|Failed to load location's details.";
            }
        }
        public IActionResult OnPost()
        {
            string postType = Request.Form["postType"];
            if (postType == "deleteLocation")
            {
                try
                {
                    string targetLocationId = Request.Form["targetLocationId"];
                    Models.Location location = _ApplicationDbContext.Locations.Where(x => x.Id == targetLocationId).First();
                    Models.Room[] rooms = _ApplicationDbContext.Rooms.Where(x => x.LocationId == targetLocationId).ToArray();
                    foreach (Models.Room room in rooms)
                    {
                        _ApplicationDbContext.Rooms.Remove(room);
                    }

                    _ApplicationDbContext.Locations.Remove(location);
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Location successfully deleted.";
                    return RedirectToPage("Index");
                }
                catch
                {
                    Message = "alert alert-danger|Failed to delete location.";
                    return RedirectToPage("Details/" + Location.Id);
                }
            }
            else
            {

                try
                {
                    string targetRoomId = Request.Form["targetRoomId"];
                    Models.Room room = _ApplicationDbContext.Rooms.Where(x => x.Id == targetRoomId).First();

                    _ApplicationDbContext.Rooms.Remove(room);
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Room successfully deleted.";
                    return RedirectToPage("Details/" + Location.Id);
                }
                catch (Exception e)
                {
                    Message = "alert alert-danger|Failed to delete room.";
                    return RedirectToPage("Details/" + Location.Id);
                }
            }
        }
    }
}
using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace CA2_Web.Pages.Room
{
    public class IndexModel : PageModel
    {
        [TempData]
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public int LoaderFade = 800;
        public int ContentFade = 500;
        public bool IsAdmin = false;
        public string ImgBaseUrl { get; set; }
        public Models.Room Room = new Models.Room();
        public Models.Booking[] Bookings = new Models.Booking[0];

        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AppConfigurations _AppConfigurations;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        public IndexModel(
            ApplicationDbContext ApplicationDbContext,
            IOptions<AppConfigurations> AppConfigurations,
            IOptions<AwsS3Configurations> AwsS3Configurations,
            IHttpContextAccessor IHttpContextAccessor)
        {
            _ApplicationDbContext = ApplicationDbContext;
            _AppConfigurations = AppConfigurations.Value;
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

                Room = _ApplicationDbContext.Rooms
                    .Where(x => x.Id == targetId)
                    .First();
                
                Bookings = _ApplicationDbContext.Bookings
                    .Where(x => x.RoomId == Room.Id)
                    .OrderByDescending(x => DateTime.ParseExact(
                        x.BookDate, _AppConfigurations.AppDateTimeFormat, CultureInfo.InvariantCulture))
                    .ThenBy(x => x.TimeSlotId)
                    .ToArray();
            }
            catch(Exception e)
            {
                Message = "alert alert-danger|Failed to load location's details.";
            }
        }
        public IActionResult OnPost()
        {
            string postType = Request.Form["postType"];
            if(postType == "deleteRoom")
            {
                try
                {
                    string targetRoomId = Request.Form["targetRoomId"];
                    Models.Room room = _ApplicationDbContext.Rooms.Where(x => x.Id == targetRoomId).First();

                    _ApplicationDbContext.Rooms.Remove(room);
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Room successfully deleted.";
                    return Redirect("/Location/Index");
                }
                catch
                {
                    Message = "alert alert-danger|Failed to delete room.";
                    return RedirectToPage("Index/" + Room.Id);
                }
            }
            else
            {
                try
                {
                    string targetBookingId = Request.Form["targetBookingId"];
                    Models.Booking booking = _ApplicationDbContext.Bookings.Where(x => x.Id == targetBookingId).First();

                    _ApplicationDbContext.Bookings.Remove(booking);
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Booking successfully deleted.";
                    return Redirect("/Location/Index");
                }
                catch
                {
                    Message = "alert alert-danger|Failed to delete booking.";
                    return RedirectToPage("Index/" + Room.Id);
                }
            }
        }
    }
}
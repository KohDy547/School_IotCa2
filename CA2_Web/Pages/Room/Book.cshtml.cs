using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Claims;

namespace CA2_Web.Pages.Room
{
    public class BookModel : PageModel
    {
        [TempData]
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public int LoaderFade = 800;
        public int ContentFade = 500;
        public int IsStudent = 0;
        public string RoomId = "";
        public string RoomName = "";
        public string UserName { get; set; }
        public string[] TimeSlots = new string[]
        {
            "0800 - 1000",
            "1000 - 1200",
            "1300 - 1500",
            "1500 - 1700",
            "1700 - 1900"
        };
        public string[][] Authorities = new string[0][];

        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AppConfigurations _AppConfigurations;
        public BookModel(
            IHttpContextAccessor IHttpContextAccessor,
            ApplicationDbContext ApplicationDbContext,
            IOptions<AppConfigurations> AppConfigurations)
        {
            _IHttpContextAccessor = IHttpContextAccessor;
            _ApplicationDbContext = ApplicationDbContext;
            _AppConfigurations = AppConfigurations.Value;
        }

        public void OnGet(string targetId)
        {
            RoomId = targetId;

            string currentUserId =
                _IHttpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            int currentUserAl =
                _ApplicationDbContext.UserProperties
                .Where(x => x.Id == currentUserId)
                .Select(x => x.AccessLevel)
                .First();
            IsStudent = (currentUserAl == 1)? 1: 0;

            UserName =
                _ApplicationDbContext.UserProperties
                .Where(x => x.Id == currentUserId)
                .Select(x => x.Email)
                .First();

            RoomName = _ApplicationDbContext.Rooms.Where(x => x.Id == targetId).Select(x => x.Name).First();

            if (IsStudent == 1)
            {
                Authorities = _ApplicationDbContext.UserProperties
                    .Where(x => x.AccessLevel == 2)
                    .Select(x => new string[] { x.Id, x.Email })
                    .ToArray();
            }
        }
        public IActionResult OnPost()
        {
            string student = Request.Form["txt_isStudent"];
            string roomId = Request.Form["txt_roomId"];
            string userName = Request.Form["txt_userName"];

            string currentUserId =
                _IHttpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            try
            {
                string authority = "";
                if (student == "1")
                {
                    int tsId = Int32.Parse(Request.Form["cmb_timeSlot"]);
                    string rawDt = Request.Form["dt_date"];
                    string datetime = DateTime.ParseExact(rawDt,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                        .ToString(_AppConfigurations.AppDateTimeFormat);

                    authority = Request.Form["cmb_Authority"];
                    string authorityName = _ApplicationDbContext
                        .UserProperties
                        .Where(x => x.Id == authority)
                        .Select(x => x.Email)
                        .First();

                    _ApplicationDbContext.Bookings.Add(new Models.Booking
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoomId = roomId,
                        BookedByName = userName,
                        BookedById = currentUserId,
                        Status = 0,
                        SupervisedByName = authorityName,
                        SupervisedById = authority,
                        TimeSlot = TimeSlots[tsId],
                        TimeSlotId = tsId,
                        BookDate = datetime
                    });
                }
                else
                {
                    int tsId = Int32.Parse(Request.Form["cmb_timeSlot"]);
                    string rawDt = Request.Form["dt_date"];
                    string datetime = DateTime.ParseExact(rawDt,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                        .ToString(_AppConfigurations.AppDateTimeFormat);

                    _ApplicationDbContext.Bookings.Add(new Models.Booking
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoomId = roomId,
                        BookedByName = userName,
                        BookedById = currentUserId,
                        Status = 1,
                        SupervisedByName = null,
                        SupervisedById = null,
                        TimeSlot = TimeSlots[tsId],
                        TimeSlotId = tsId,
                        BookDate = datetime
                    });
                }

                _ApplicationDbContext.SaveChangesAsync();

                Message = "alert alert-success|Booking successfully created.";
                return Redirect("/Location/Index");
            }
            catch (Exception e)
            {
                Message = "alert alert-danger|Failed to creating booking.";
                return RedirectToPage("Book", roomId);
            }
        }
    }
}
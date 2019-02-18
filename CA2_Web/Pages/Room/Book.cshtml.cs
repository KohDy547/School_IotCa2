using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = "AccessLevel01")]
    public class BookModel : PageModel
    {
        [TempData]
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public int LoaderFade = 800;
        public int ContentFade = 500;

        public string CurrentUserId = "";
        public string CurrentUserName = "";
        public int CurrentUserAl = 0;

        public string CurrentRoomId = "";
        public string CurrentRoomName = "";

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

            CurrentRoomId = targetId;
            CurrentRoomName = _ApplicationDbContext.Rooms.Where(x => x.Id == targetId).Select(x => x.Name).First();

            if (CurrentUserAl == 1)
            {
                Authorities = _ApplicationDbContext.UserProperties
                    .Where(x => x.AccessLevel >= 2)
                    .Select(x => new string[] { x.Id, x.Email })
                    .ToArray();
            }
        }
        public IActionResult OnPost()
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
            CurrentRoomId = Request.Form["_CurrentRoomId"];

            try
            {
                if (CurrentUserAl == 1)
                {
                    int timeslotId = Int32.Parse(Request.Form["cmb_inputTimeSlot"]);
                    string inputDatetime = Request.Form["dt_inputDate"];
                    string formattedDatetime = DateTime.ParseExact(inputDatetime,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                        .ToString(_AppConfigurations.AppDateTimeFormat);

                    string authority = Request.Form["cmb_inputAuthority"];
                    string authorityName = _ApplicationDbContext
                        .UserProperties
                        .Where(x => x.Id == authority)
                        .Select(x => x.Email)
                        .First();

                    _ApplicationDbContext.Bookings.Add(new Models.Booking
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoomId = CurrentRoomId,
                        BookedByName = CurrentUserName,
                        BookedById = CurrentUserId,
                        Status = 0,
                        SupervisedByName = authorityName,
                        SupervisedById = authority,
                        TimeSlot = TimeSlots[timeslotId],
                        TimeSlotId = timeslotId,
                        BookDate = formattedDatetime
                    });
                }
                else
                {
                    int timeslotId = Int32.Parse(Request.Form["cmb_inputTimeSlot"]);
                    string inputDatetime = Request.Form["dt_inputDate"];
                    string formattedDatetime = DateTime.ParseExact(inputDatetime,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture)
                        .ToString(_AppConfigurations.AppDateTimeFormat);

                    _ApplicationDbContext.Bookings.Add(new Models.Booking
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoomId = CurrentRoomId,
                        BookedByName = CurrentUserName,
                        BookedById = CurrentUserId,
                        Status = 1,
                        SupervisedByName = null,
                        SupervisedById = null,
                        TimeSlot = TimeSlots[timeslotId],
                        TimeSlotId = timeslotId,
                        BookDate = formattedDatetime
                    });
                }

                _ApplicationDbContext.SaveChangesAsync();

                Message = "alert alert-success|Booking successfully created.";
                return Redirect("~/Room/Index/" + CurrentRoomId);
            }
            catch
            {
                Message = "alert alert-danger|Failed to creating booking.";
                return RedirectToPage("Book", CurrentRoomId);
            }
        }
    }
}
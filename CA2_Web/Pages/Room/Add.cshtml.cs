using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace CA2_Web.Pages.Room
{
    [Authorize(Policy = "AccessLevel02")]
    public class AddModel : PageModel
    {
        [TempData]
        public string Message { get; set; }
        public bool ShowMessage => !string.IsNullOrEmpty(Message);

        public int LoaderFade = 800;
        public int ContentFade = 500;

        public string CurrentUserId = "";
        public string CurrentUserName = "";
        public int CurrentUserAl = 0;

        public string TargetLocationId = "";
        public string TargetLocationName = "";

        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AppConfigurations _AppConfigurations;
        public AddModel(
            IHttpContextAccessor IHttpContextAccessor,
            ApplicationDbContext ApplicationDbContext,
            IOptions<AppConfigurations> AppConfigurations)
        {
            _IHttpContextAccessor = IHttpContextAccessor;
            _ApplicationDbContext = ApplicationDbContext;
            _AppConfigurations = AppConfigurations.Value;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public class InputModel
        {
            [Required(ErrorMessage = "Name"), Display(Name = "Name")]
            public string Name { get; set; }

            [Display(Name = "Device Id")]
            public string DeviceId { get; set; }
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
            
            TargetLocationId = targetId;
            TargetLocationName = _ApplicationDbContext.Locations
                .Where(x => x.Id == TargetLocationId)
                .Select(x => x.Name)
                .First();
        }
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                string returnMessage = "alert alert-danger|Invalid input for ";
                foreach (ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (ModelError modelError in modelState.Errors)
                    {
                        returnMessage += modelError.ErrorMessage + ", ";
                    }
                }
                Message = returnMessage.Remove(returnMessage.Length - 2) + ".";
                return RedirectToPage("Add");
            }

            TargetLocationId = Request.Form["txt_inputLocationId"];
            TargetLocationName = Request.Form["txt_inputLocationName"];

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
                string generatedId = Guid.NewGuid().ToString();
                Models.Room newRoom = new Models.Room
                {
                    Id = generatedId,
                    Name = Input.Name,
                    DeviceId = Input.DeviceId,
                    LocationId = TargetLocationId,
                    LocationName = TargetLocationName
                };
                _ApplicationDbContext.Rooms.Add(newRoom);
                _ApplicationDbContext.SaveChangesAsync();

                Message ="alert alert-success|Room added successfully.";
                return Redirect("~/Location/Details/" + TargetLocationId);
            }
            catch
            {
                Message = "alert alert-danger|Failed to add room.";
                return RedirectToPage("Add");
            }
        }
    }
}
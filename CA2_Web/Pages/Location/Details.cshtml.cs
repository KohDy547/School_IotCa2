using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using CA2_Assignment.Models;
using CA2_Assignment.Services;
using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Hosting;
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

        private readonly IHostingEnvironment _IHostingEnvironment;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AwsConfigurations _AwsConfigurations;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly IAwsService _IAwsService;
        private readonly AmazonS3Client _AmazonS3Client;
        public DetailsModel(
            IHostingEnvironment IHostingEnvironment,
            IHttpContextAccessor IHttpContextAccessor,
            ApplicationDbContext ApplicationDbContext,
            IOptions<AwsConfigurations> AwsConfigurations,
            IOptions<AwsS3Configurations> AwsS3Configurations,
            IAwsService IAwsService)
        {
            _IHostingEnvironment = IHostingEnvironment;
            _IHttpContextAccessor = IHttpContextAccessor;
            _ApplicationDbContext = ApplicationDbContext;
            _AwsConfigurations = AwsConfigurations.Value;
            _AwsS3Configurations = AwsS3Configurations.Value;
            _IAwsService = IAwsService;

            ImgBaseUrl = _AwsS3Configurations.Locations_ImgBaseUrl;

            string awsCredentialsPath = _IHostingEnvironment.ContentRootPath + _AwsConfigurations.CredentialsPath;
            _AmazonS3Client = new AmazonS3Client(
                new StoredProfileAWSCredentials(
                    _AwsS3Configurations.CredentialsProfile,
                    awsCredentialsPath),
                RegionEndpoint.GetBySystemName(_AwsS3Configurations.BucketRegion));
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
            try
            {
                string targetLocationId = Request.Form["targetLocationId"];

                Response returnedResponse = _IAwsService.awsS3_DeleteFileAsync(
                    _AmazonS3Client,
                    _AwsS3Configurations.BucketName,
                    "Ca2Iot/" + targetLocationId + ".jpg").Result;
                if (returnedResponse.HasError)
                {
                    Message = "alert alert-danger|Failed to delete location.";
                    return RedirectToPage("Details/" + Location.Id);
                }

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
    }
}
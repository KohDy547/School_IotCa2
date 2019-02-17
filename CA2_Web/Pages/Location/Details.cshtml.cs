using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
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
using System.Collections.Generic;
using System.Globalization;
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
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AppConfigurations _AppConfigurations;
        private readonly AwsConfigurations _AwsConfigurations;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly AwsDynamoConfigurations _AwsDynamoConfigurations;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly IAwsService _IAwsService;
        private readonly AmazonS3Client _AmazonS3Client;
        private readonly AmazonDynamoDBClient _AmazonDynamoDBClient;
        public DetailsModel(
            IHostingEnvironment IHostingEnvironment,
            ApplicationDbContext ApplicationDbContext,
            IOptions<AwsConfigurations> AwsConfigurations,
            IOptions<AwsDynamoConfigurations> AwsDynamoConfigurations,
            IOptions<AppConfigurations> AppConfigurations,
            IOptions<AwsS3Configurations> AwsS3Configurations,
            IHttpContextAccessor IHttpContextAccessor,
            IAwsService IAwsService)
        {
            _IHostingEnvironment = IHostingEnvironment;
            _ApplicationDbContext = ApplicationDbContext;
            _AwsConfigurations = AwsConfigurations.Value;
            _AwsDynamoConfigurations = AwsDynamoConfigurations.Value;
            _AppConfigurations = AppConfigurations.Value;
            _AwsS3Configurations = AwsS3Configurations.Value;
            _IHttpContextAccessor = IHttpContextAccessor;
            _IAwsService = IAwsService;

            ImgBaseUrl = _AwsS3Configurations.Locations_ImgBaseUrl;
            
            _AmazonS3Client = new AmazonS3Client(
                new StoredProfileAWSCredentials(
                    _AwsS3Configurations.CredentialsProfile,
                    _IHostingEnvironment.ContentRootPath + _AwsConfigurations.CredentialsPath),
                RegionEndpoint.GetBySystemName(_AwsS3Configurations.BucketRegion));

            _AmazonDynamoDBClient = new AmazonDynamoDBClient(
                new StoredProfileAWSCredentials(
                    _AwsDynamoConfigurations.CredentialsProfile,
                    _IHostingEnvironment.ContentRootPath + _AwsConfigurations.CredentialsPath),
                RegionEndpoint.GetBySystemName(_AwsDynamoConfigurations.Region));
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
            if(postType == "triggerLockdown")
            {
                try
                {
                    Response response;
                    string deviceId = Request.Form["txt_deviceId"];
                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        string roomId = Request.Form["txt_roomId"];

                        QueryRequest queryRequest = new QueryRequest
                        {
                            TableName = "kopiokosong_controller",
                            KeyConditionExpression = "deviceid = :deviceId",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                        {":deviceId", new AttributeValue { S = deviceId }}}
                        };
                        response = _IAwsService.awsDDB_GetObjectAsync(
                            _AmazonDynamoDBClient, queryRequest).Result;
                        if (response.HasError)
                        {
                            Message = "alert alert-danger|Failed to trigger lockdown.";
                            return RedirectToPage("Details/" + Location.Id);
                        }

                        QueryResponse queryResponse = (QueryResponse)response.Payload;
                        string[] latest = queryResponse.Items
                           .Select(
                               x => new string[] { x["datetimeid"].S, x["led"].S, x["line1"].S, x["line2"].S }
                           )
                           .OrderByDescending(
                               y => DateTime.ParseExact(y[0],
                               _AppConfigurations.AppDateTimeFormat,
                               CultureInfo.InvariantCulture)
                           )
                           .First();

                        string led = (latest[1] == "on") ? "off" : "on";
                        Document inputItem = new Document();
                        inputItem["deviceid"] = deviceId;
                        inputItem["datetimeid"] = DateTime.Now.ToString(_AppConfigurations.AppDateTimeFormat);
                        inputItem["led"] = led;
                        inputItem["line1"] = latest[2];
                        inputItem["line2"] = latest[3];

                        response = _IAwsService.awsDDB_PutObjectAsync(
                            _AmazonDynamoDBClient, inputItem, "kopiokosong_controller").Result;
                    }
                    Message = "alert alert-success|Lockdown triggered.";
                    return RedirectToPage("Index");
                }
                catch
                {
                    Message = "alert alert-danger|Failed to trigger lockdown.";
                    return RedirectToPage("Details/" + Location.Id);
                }
            }
            else
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
}
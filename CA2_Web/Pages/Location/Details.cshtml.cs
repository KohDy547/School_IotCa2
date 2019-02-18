using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.S3;
using CA2_Web.Configurations;
using CA2_Web.Data;
using CA2_Web.Models;
using CA2_Web.Services;
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

        public string CurrentUserId = "";
        public string CurrentUserName = "";
        public int CurrentUserAl = 0;

        public string CurrentLocationId = "";
        public string CurrentLocationName = "";
        public string CurrentLocationDescription = "";

        public string[] RoomsOnLockdown = new string[0];

        public string ImgBaseUrl { get; set; }
        public Models.Room[] Rooms = new Models.Room[0];

        private readonly IHostingEnvironment _IHostingEnvironment;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AppConfigurations _AppConfigurations;
        private readonly AwsConfigurations _AwsConfigurations;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly AwsDynamoConfigurations _AwsDynamoConfigurations;
        private readonly IoTConfigurations _IoTConfigurations;
        private readonly IAwsService _IAwsService;
        private readonly AmazonS3Client _AmazonS3Client;
        private readonly AmazonDynamoDBClient _AmazonDynamoDBClient;
        public DetailsModel(
            IHostingEnvironment IHostingEnvironment,
            IHttpContextAccessor IHttpContextAccessor,
            ApplicationDbContext ApplicationDbContext,
            IOptions<AppConfigurations> AppConfigurations,
            IOptions<AwsConfigurations> AwsConfigurations,
            IOptions<AwsS3Configurations> AwsS3Configurations,
            IOptions<AwsDynamoConfigurations> AwsDynamoConfigurations,
            IOptions<IoTConfigurations> IoTConfigurations,
            IAwsService IAwsService)
        {
            _IHostingEnvironment = IHostingEnvironment;
            _IHttpContextAccessor = IHttpContextAccessor;
            _ApplicationDbContext = ApplicationDbContext;
            _AppConfigurations = AppConfigurations.Value;
            _AwsConfigurations = AwsConfigurations.Value;
            _AwsS3Configurations = AwsS3Configurations.Value;
            _AwsDynamoConfigurations = AwsDynamoConfigurations.Value;
            _IoTConfigurations = IoTConfigurations.Value;
            _IAwsService = IAwsService;

            ImgBaseUrl = _IoTConfigurations.AwsS3_LocationsImgBaseUrl;

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
                Models.Location currentLocation = _ApplicationDbContext.Locations
                    .Where(x => x.Id == targetId)
                    .First();
                CurrentLocationId = currentLocation.Id;
                CurrentLocationName = currentLocation.Name;
                CurrentLocationDescription = currentLocation.Description;

                Rooms = _ApplicationDbContext.Rooms
                    .Where(x => x.LocationId == CurrentLocationId)
                    .OrderBy(x => x.Name)
                    .ToArray();

                if (CurrentUserAl >= 5)
                {
                    Response response;
                    List<string> roomsOnLockdownList = new List<string>();
                    foreach (Models.Room room in Rooms.Where(x => !string.IsNullOrEmpty(x.DeviceId)))
                    {
                        QueryRequest queryRequest = new QueryRequest
                        {
                            TableName = _IoTConfigurations.AwsDynamo_WebAppMessageTable,
                            KeyConditionExpression = "deviceid = :deviceId",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                {":deviceId", new AttributeValue { S = room.DeviceId }}
                            }
                        };
                        response = _IAwsService.awsDDB_GetObjectAsync(
                            _AmazonDynamoDBClient, queryRequest).Result;
                        if (response.HasError)
                        {
                            Message = "alert alert-danger|Failed to load location's details.";
                            return;
                        }

                        QueryResponse queryResponse = (QueryResponse)response.Payload;
                        string[] latest = queryResponse.Items
                           .Select(
                               x => new string[] { x["datetimeid"].S, x["led"].S }
                           )
                           .OrderByDescending(
                               y => DateTime.ParseExact(y[0],
                               _AppConfigurations.AppDateTimeFormat,
                               CultureInfo.InvariantCulture)
                           )
                           .First();

                        if (latest[1] == "on") roomsOnLockdownList.Add(room.Id);
                    }
                    RoomsOnLockdown = roomsOnLockdownList.ToArray();
                }
            }
            catch
            {
                Message = "alert alert-danger|Failed to load location's details.";
            }
        }
        public IActionResult OnPost()
        {
            string PostType = Request.Form["_PostType"];
            CurrentLocationId = Request.Form["_CurrentLocationId"];
            if (PostType == "TriggerLockdown")
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
                            TableName = _IoTConfigurations.AwsDynamo_WebAppMessageTable,
                            KeyConditionExpression = "deviceid = :deviceId",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                        {":deviceId", new AttributeValue { S = deviceId }}}
                        };
                        response = _IAwsService.awsDDB_GetObjectAsync(
                            _AmazonDynamoDBClient, queryRequest).Result;
                        if (response.HasError)
                        {
                            Message = "alert alert-danger|Failed to trigger lockdown.";
                            return RedirectToPage("Details", CurrentLocationId);
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
                        string lockdownStatus = (latest[1] == "on") ? "disengaged" : "engaged";
                        Document inputItem = new Document();
                        inputItem["deviceid"] = deviceId;
                        inputItem["datetimeid"] = DateTime.Now.ToString(_AppConfigurations.AppDateTimeFormat);
                        inputItem["led"] = led;
                        inputItem["line1"] = latest[2];
                        inputItem["line2"] = latest[3];

                        response = _IAwsService.awsDDB_PutObjectAsync(
                            _AmazonDynamoDBClient, inputItem, _IoTConfigurations.AwsDynamo_WebAppMessageTable).Result;

                        Message = "alert alert-success|Lockdown " + lockdownStatus + ".";
                        return RedirectToPage("Details", CurrentLocationId);
                    }

                    Message = "alert alert-danger|No device Id binded for room.";
                    return RedirectToPage("Details", CurrentLocationId);
                }
                catch
                {
                    Message = "alert alert-danger|Failed to trigger lockdown.";
                    return RedirectToPage("Details", CurrentLocationId);

                }
            }
            else if (PostType == "DeleteLocation")
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
                        return RedirectToPage("Details", CurrentLocationId);
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
                    return Redirect("~/Index");
                }
                catch
                {
                    Message = "alert alert-danger|Failed to delete location.";
                    return RedirectToPage("Details", CurrentLocationId);
                }
            }

            Message = "alert alert-danger|Failed to delete location.";
            return RedirectToPage("Details", CurrentLocationId);
        }
    }
}
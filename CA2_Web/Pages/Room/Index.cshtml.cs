using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using CA2_Web.Models;
using CA2_Web.Services;
using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Authorization;
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

namespace CA2_Web.Pages.Room
{
    [Authorize(Policy = "AccessLevel01")]
    public class IndexModel : PageModel
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
        public string CurrentRoomDeviceId = "";
        public string CurrentRoomLocationId = "";
        public string CurrentRoomLocationName = "";

        public string ImgBaseUrl { get; set; }
        public Models.Booking[] Bookings = new Models.Booking[0];
        public string[][] RfIdAudits = new string[0][];

        private readonly IHostingEnvironment _IHostingEnvironment;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AppConfigurations _AppConfigurations;
        private readonly AwsConfigurations _AwsConfigurations;
        private readonly AwsDynamoConfigurations _AwsDynamoConfigurations;
        private readonly IoTConfigurations _IoTConfigurations;
        private readonly AmazonDynamoDBClient _AmazonDynamoDBClient;
        private readonly IAwsService _IAwsService;
        public IndexModel(
            IHostingEnvironment IHostingEnvironment,
            IHttpContextAccessor IHttpContextAccessor,
            ApplicationDbContext ApplicationDbContext,
            IOptions<AppConfigurations> AppConfigurations,
            IOptions<AwsConfigurations> AwsConfigurations,
            IOptions<AwsDynamoConfigurations> AwsDynamoConfigurations,
            IOptions<IoTConfigurations> IoTConfigurations,
            IAwsService IAwsService)
        {
            _IHostingEnvironment = IHostingEnvironment;
            _IHttpContextAccessor = IHttpContextAccessor;
            _ApplicationDbContext = ApplicationDbContext;
            _AwsConfigurations = AwsConfigurations.Value;
            _AwsDynamoConfigurations = AwsDynamoConfigurations.Value;
            _AppConfigurations = AppConfigurations.Value;
            _IoTConfigurations = IoTConfigurations.Value;
            _IAwsService = IAwsService;

            _AmazonDynamoDBClient = new AmazonDynamoDBClient(
                new StoredProfileAWSCredentials(
                    _AwsDynamoConfigurations.CredentialsProfile,
                    _IHostingEnvironment.ContentRootPath + _AwsConfigurations.CredentialsPath),
                RegionEndpoint.GetBySystemName(_AwsDynamoConfigurations.Region));

            ImgBaseUrl = _IoTConfigurations.AwsS3_SurveillanceImgBaseUrl;
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
                Models.Room currentRoom = _ApplicationDbContext.Rooms
                    .Where(x => x.Id == targetId)
                    .First();
                CurrentRoomId = currentRoom.Id;
                CurrentRoomName = currentRoom.Name;
                CurrentRoomDeviceId = currentRoom.DeviceId;
                CurrentRoomLocationId = currentRoom.LocationId;
                CurrentRoomLocationName = currentRoom.LocationName;

                Bookings = _ApplicationDbContext.Bookings
                    .Where(x => x.RoomId == currentRoom.Id)
                    .OrderByDescending(x => DateTime.ParseExact(
                        x.BookDate, _AppConfigurations.AppDateTimeFormat, CultureInfo.InvariantCulture))
                    .ThenBy(x => x.TimeSlotId)
                    .ToArray();

                if (!string.IsNullOrEmpty(CurrentRoomDeviceId))
                {
                    try
                    {
                        Response response;
                        QueryRequest queryRequest = new QueryRequest
                        {
                            TableName = _IoTConfigurations.AwsDynamo_DeviceMessageTable,
                            KeyConditionExpression = "deviceid = :deviceId",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                {":deviceId", new AttributeValue { S = CurrentRoomDeviceId }}}
                        };
                        response = _IAwsService.awsDDB_GetObjectAsync(
                            _AmazonDynamoDBClient, queryRequest).Result;
                        if (response.HasError)
                        {
                            Message = "alert alert-danger|Failed to load room's details.";
                            return;
                        }

                        QueryResponse filteredRfIds = (QueryResponse)response.Payload;
                        string[][] rfIds = filteredRfIds.Items
                        .Where(x => x["RFID"].S != "-")
                        .Select(
                            x => new string[] {
                                x["datetimeid"].S, x["RFID"].S
                            }
                        )
                        .OrderByDescending(
                            y => DateTime.ParseExact(y[0],
                            _AppConfigurations.AppDateTimeFormat,
                            CultureInfo.InvariantCulture)
                        )
                        .Select(x => new string[] { x[1], x[0] })
                        .ToArray();
                        List<string[]> rfidList = new List<string[]>();
                        foreach (string[] rfid in rfIds)
                        {
                            Models.UserProperty user = _ApplicationDbContext.UserProperties
                                .Where(x => x.RfId.Trim() == rfid[0].Trim().Replace("\r\n", string.Empty))
                                .FirstOrDefault();
                            string username = (user == null) ? "Unregistered RfId" : user.Email;

                            rfidList.Add(new string[] { rfid[0].Trim().Replace("\r\n", string.Empty), username, rfid[1] });
                        }
                        RfIdAudits = rfidList.ToArray();
                    }
                    catch
                    {
                        Message = "alert alert-danger|Failed to load room's details.";
                    }
                }
            }
            catch
            {
                Message = "alert alert-danger|Failed to load room's details.";
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
            string PostType = Request.Form["_PostType"];
            
            if (PostType == "UpdateDeviceId")
            {
                try
                {
                    string inputDeviceId = Request.Form["txt_inputDeviceId"];

                    Models.Room tempRoom = _ApplicationDbContext.Rooms.Where(x => x.Id == CurrentRoomId).First();
                    tempRoom.DeviceId = inputDeviceId;
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Device Id successfully updated.";
                    return RedirectToPage("Index", CurrentRoomId);
                }
                catch
                {
                    Message = "alert alert-danger|Failed to update device Id.";
                    return RedirectToPage("Index", CurrentRoomId);
                }
            }
            else if (PostType == "UpdateDeviceMsg")
            {
                CurrentRoomDeviceId = Request.Form["_CurrentRoomDeviceId"];
                try
                {
                    Response response;
                    string message = Request.Form["txt_postMessage"];

                    QueryRequest queryRequest = new QueryRequest
                    {
                        TableName = "kopiokosong_controller",
                        KeyConditionExpression = "deviceid = :deviceId",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                        {":deviceId", new AttributeValue { S = CurrentRoomDeviceId }}}
                    };
                    response = _IAwsService.awsDDB_GetObjectAsync(
                        _AmazonDynamoDBClient, queryRequest).Result;
                    if (response.HasError)
                    {
                        Message = "alert alert-danger|Failed to post lcd message.";
                        return RedirectToPage("Index", CurrentRoomId);
                    }

                    QueryResponse queryResponse = (QueryResponse)response.Payload;
                    string latestLed = queryResponse.Items
                       .Select(
                           x => new string[] { x["datetimeid"].S, x["led"].S }
                       )
                       .OrderByDescending(
                           y => DateTime.ParseExact(y[0],
                           _AppConfigurations.AppDateTimeFormat,
                           CultureInfo.InvariantCulture)
                       )
                       .First()[1];

                    string[] lines = message.Split(' ');

                    Document inputItem = new Document();
                    inputItem["deviceid"] = CurrentRoomId;
                    inputItem["datetimeid"] = DateTime.Now.ToString(_AppConfigurations.AppDateTimeFormat);
                    inputItem["led"] = latestLed;
                    inputItem["line1"] = lines[0];
                    inputItem["line2"] = lines[1];

                    response = _IAwsService.awsDDB_PutObjectAsync(
                        _AmazonDynamoDBClient, inputItem, _IoTConfigurations.AwsDynamo_WebAppMessageTable).Result;
                    if (response.HasError)
                    {
                        Message = "alert alert-danger|Failed to post lcd message.";
                        return RedirectToPage("Index", CurrentRoomId);
                    }

                    Message = "alert alert-success|Lcd message posted successfully.";
                    return RedirectToPage("Index", CurrentRoomId);
                }
                catch
                {
                    Message = "alert alert-danger|Failed to post lcd message.";
                    return RedirectToPage("Index", CurrentRoomId);
                }
            }
            else if (PostType == "DeleteBooking")
            {
                CurrentRoomId = Request.Form["_CurrentRoomId"];
                try
                {
                    string targetBookingId = Request.Form["targetBookingId"];
                    Models.Booking booking = _ApplicationDbContext.Bookings.Where(x => x.Id == targetBookingId).First();

                    _ApplicationDbContext.Bookings.Remove(booking);
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Booking successfully deleted.";
                    return RedirectToPage("Index", CurrentRoomId);
                }
                catch
                {
                    Message = "alert alert-danger|Failed to delete booking.";
                    return RedirectToPage("Index", CurrentRoomId);
                }
            }
            else if (PostType == "DeleteRoom")
            {
                try
                {
                    Models.Room room = _ApplicationDbContext.Rooms.Where(x => x.Id == CurrentRoomId).First();

                    _ApplicationDbContext.Rooms.Remove(room);
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Room successfully deleted.";
                    return Redirect("~/Location/Details/" + room.LocationId);
                }
                catch
                {
                    Message = "alert alert-danger|Failed to delete room.";
                    return RedirectToPage("Index", CurrentRoomId);
                }
            }

            Message = "alert alert-danger|No post type detected.";
            return RedirectToPage("Index", CurrentRoomId);
        }
    }
}
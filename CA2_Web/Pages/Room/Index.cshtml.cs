using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
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
        public int CurrentUserAl = 0;
        public string ImgBaseUrl { get; set; }
        public Models.Room Room = new Models.Room();
        public Models.Booking[] Bookings = new Models.Booking[0];
        public string[][] RfIdAudits = new string[0][];
        public string[][] DataAudits = new string[0][];

        private readonly IHostingEnvironment _IHostingEnvironment;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AppConfigurations _AppConfigurations;
        private readonly AwsConfigurations _AwsConfigurations;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly AwsDynamoConfigurations _AwsDynamoConfigurations;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly IAwsService _IAwsService;
        private readonly AmazonDynamoDBClient _AmazonDynamoDBClient;
        public IndexModel(
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

            ImgBaseUrl = _AwsS3Configurations.Surveillance_ImgBaseUrl;

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
                    CurrentUserAl = currentUserAl;
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

                if (!string.IsNullOrEmpty(Room.DeviceId))
                {

                    try
                    {
                        Response response;
                        QueryRequest queryRequest = new QueryRequest
                        {
                            TableName = "kopiokosong_room",
                            KeyConditionExpression = "deviceid = :deviceId",
                            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                                {":deviceId", new AttributeValue { S = Room.DeviceId }}}
                        };
                        response = _IAwsService.awsDDB_GetObjectAsync(
                            _AmazonDynamoDBClient, queryRequest).Result;
                        if (response.HasError)
                        {
                            Message = "alert alert-danger|Failed to load room's details.";
                            return;
                        }

                        QueryResponse rfidFilter = (QueryResponse)response.Payload;
                            string[][] rfids = rfidFilter.Items
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
                            .Select(x => new string[] { x[1], x[0]})
                            .ToArray();
                        List<string[]> rfidList = new List<string[]>();
                        foreach(string[] rfid in rfids)
                        {
                            Models.UserProperty user = _ApplicationDbContext.UserProperties
                                .Where(x => x.RfId.Trim() == rfid[0].Trim().Replace("\r\n", string.Empty))
                                .FirstOrDefault();
                            string username = (user == null) ? "Unregistered RfId" : user.Email;

                            rfidList.Add(new string[] { rfid[0].Trim().Replace("\r\n", string.Empty), username, rfid[1] });
                        }
                        RfIdAudits = rfidList.ToArray();

                        QueryResponse queryResponse = (QueryResponse)response.Payload;
                        string[][] data = queryResponse.Items.Select(x => new string[] {
                            x["datetimeid"].S,
                            x["humidity"].N,
                            x["light"].N,
                            x["temperature"].N}
                        ).ToArray();
                        DataAudits = data;
                    }
                    catch(Exception e)
                    {
                        Message = "alert alert-danger|Failed to load room's details.";
                    }
                }
            }
            catch(Exception e)
            {
                Message = "alert alert-danger|Failed to load room's details.";
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
            else if(postType == "updateDeviceId")
            {
                try
                {
                    string targetRoomId = Request.Form["targetRoomId"];
                    string deviceId = Request.Form["txt_DeviceId"];
                    Models.Room tempRoom = _ApplicationDbContext.Rooms.Where(x => x.Id == targetRoomId).First();
                    tempRoom.DeviceId = deviceId;
                    _ApplicationDbContext.SaveChangesAsync();

                    Message = "alert alert-success|Device Id successfully updated.";
                    return Redirect("/Location/Index");
                }
                catch(Exception e)
                {
                    Message = "alert alert-danger|Failed to update device Id.";
                    return RedirectToPage("Index/" + Room.Id);
                }
            }
            else if(postType == "updateDeviceMsg")
            {
                try
                {
                    Response response;
                    string message = Request.Form["txt_postMessage"];
                    string deviceId = Request.Form["txt_DeviceId"];
                    
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
                        Message = "alert alert-danger|Failed to post lcd message.";
                        return RedirectToPage("Index/" + Room.Id);
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
                    inputItem["deviceid"] = deviceId;
                    inputItem["datetimeid"] = DateTime.Now.ToString(_AppConfigurations.AppDateTimeFormat);
                    inputItem["led"] = latestLed;
                    inputItem["line1"] = lines[0];
                    inputItem["line2"] = lines[1];

                    response = _IAwsService.awsDDB_PutObjectAsync(
                        _AmazonDynamoDBClient, inputItem, "kopiokosong_controller").Result;
                    if (response.HasError)
                    {
                        Message = "alert alert-danger|Failed to post lcd message.";
                        return RedirectToPage("Index/" + Room.Id);
                    }

                    Message = "alert alert-success|Lcd message posted successfully.";
                    return Redirect("/Location/Index");
                }
                catch (Exception e)
                {
                    Message = "alert alert-danger|Failed to post lcd message.";
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
﻿using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using CA2_Web.Models;
using CA2_Web.Services;
using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace CA2_Web.Pages.Location
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

        [BindProperty]
        public InputModel Input { get; set; }
        public class InputModel
        {
            [Required(ErrorMessage = "Name"), Display(Name = "Name")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Description"), Display(Name = "Description")]
            public string Description { get; set; }

            [Required(ErrorMessage = "Photo"), Display(Name = "Photo")]
            public IFormFile Photo { get; set; }
        }

        private readonly IHostingEnvironment _IHostingEnvironment;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly ApplicationDbContext _ApplicationDbContext;
        private readonly AwsConfigurations _AwsConfigurations;
        private readonly AwsS3Configurations _AwsS3Configurations;
        private readonly AmazonS3Client _AmazonS3Client;
        private readonly IAwsService _IAwsService;
        public AddModel(
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

            string awsCredentialsPath = _IHostingEnvironment.ContentRootPath + _AwsConfigurations.CredentialsPath;
            _AmazonS3Client = new AmazonS3Client(
                new StoredProfileAWSCredentials(
                    _AwsS3Configurations.CredentialsProfile,
                    awsCredentialsPath),
                RegionEndpoint.GetBySystemName(_AwsS3Configurations.BucketRegion));
        }

        public void OnGet()
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
                Response returnedResponse;
                string generatedId = Guid.NewGuid().ToString();

                IFormFile photo = Input.Photo;
                using (Stream stream = photo.OpenReadStream())
                {
                    returnedResponse = _IAwsService.awsS3_UploadStreamAsync(
                        _AmazonS3Client,
                        _AwsS3Configurations.BucketName,
                        "Ca2Iot/" + generatedId + ".jpg",
                        stream,
                        true).Result;
                }
                if (returnedResponse.HasError)
                {
                    Message = "alert alert-danger|Failed to add location.";
                    return RedirectToPage("Add");
                }

                Models.Location newLocation = new Models.Location
                {
                    Id = generatedId,
                    Name = Input.Name,
                    Description = Input.Description
                };
                _ApplicationDbContext.Locations.Add(newLocation);
                _ApplicationDbContext.SaveChangesAsync();

                Message = "alert alert-success|Location added successfully.";
                return RedirectToPage("Index");
            }
            catch
            {
                Message = "alert alert-danger|Failed to add location.";
                return RedirectToPage("Add");
            }
        }
    }
}
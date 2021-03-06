﻿namespace CA2_Web.Configurations
{
    public class AppConfigurations
    {
        public string AppName { get; set; }
        public string AppFooter { get; set; }

        public string AppDateTimeFormat { get; set; }
    }

    public class SendGridConfigurations
    {
        public string ClientId { get; set; }
        public string ClientKey { get; set; }
        public string ServerEmail { get; set; }
        public string ServerEmailName { get; set; }
    }
    public class GoogleAuthConfigurations
    {
        public string ClientId { get; set; }
        public string ClientKey { get; set; }
    }
    public class CaptchaConfigurations
    {
        public string GenerationSeed { get; set; }
    }

    public class AwsConfigurations
    {
        public string CredentialsPath { get; set; }
    }
    public class AwsS3Configurations
    {
        public string CredentialsProfile { get; set; }
        public string BucketRegion { get; set; }
        public string BucketName { get; set; }
    }
    public class AwsDynamoConfigurations
    {
        public string CredentialsProfile { get; set; }
        public string Region { get; set; }
    }
    public class IoTConfigurations
    {
        public string AwsS3_LocationsImgBaseUrl { get; set; }
        public string AwsS3_SurveillanceImgBaseUrl { get; set; }

        public string AwsDynamo_DeviceMessageTable { get; set; }
        public string AwsDynamo_WebAppMessageTable { get; set; }
    }
}

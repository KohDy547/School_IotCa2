using Amazon.S3;
using Amazon.S3.Model;
using CA2_Assignment.Models;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CA2_Assignment.Services
{
    public interface IAwsService
    {
        Task<Response> awsS3_ReadFileAsync(
            AmazonS3Client inputS3Client, string inputS3Bucket, string inputS3FileKey);
        Task<Response> awsS3_UploadTextAsync(
            AmazonS3Client inputS3Client, string inputS3Bucket, string inputS3FileKey, string inputData, bool makeFilePublic = false);
        Task<Response> awsS3_UploadFileAsync(
            AmazonS3Client inputS3Client, string inputS3Bucket, string inputS3FileKey, string inputFilePath, bool makeFilePublic = false);
        Task<Response> awsS3_UploadStreamAsync(
            AmazonS3Client inputS3Client, string inputS3Bucket, string inputS3FileKey, Stream inputFileStream, bool makeFilePublic = false);
        Task<Response> awsS3_DeleteFileAsync(
            AmazonS3Client inputS3Client, string inputS3Bucket, string inputS3FileKey);


    }

    public class AwsService : IAwsService
    {
        public async Task<Response> awsS3_ReadFileAsync(
            AmazonS3Client inputS3Client,
            string inputS3Bucket,
            string inputS3FileKey)
        {
            try
            {
                string responseContent;
                using (GetObjectResponse response = await inputS3Client.GetObjectAsync(inputS3Bucket, inputS3FileKey))
                using (Stream responseStream = response.ResponseStream)
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    responseContent = reader.ReadToEnd();
                }

                return new Response
                {
                    HttpStatus = HttpStatusCode.OK,
                    Payload = (string)responseContent
                };
            }
            catch (AmazonS3Exception e)
            {
                return new Response
                {
                    HttpStatus = e.StatusCode,
                    ExceptionPayload = e
                };
            }
            catch (Exception e)
            {
                return new Response
                {
                    HttpStatus = HttpStatusCode.InternalServerError,
                    ExceptionPayload = e
                };
            }
        }
        public async Task<Response> awsS3_UploadTextAsync(
            AmazonS3Client inputS3Client,
            string inputS3Bucket,
            string inputS3FileKey,
            string inputData,
            bool makeFilePublic = false)
        {
            try
            {
                PutObjectRequest objectRequest = new PutObjectRequest
                {
                    BucketName = inputS3Bucket,
                    Key = inputS3FileKey,
                    ContentBody = inputData
                };
                if (makeFilePublic) objectRequest.CannedACL = S3CannedACL.PublicRead;

                PutObjectResponse response = await inputS3Client.PutObjectAsync(objectRequest);

                return new Response
                {
                    HttpStatus = HttpStatusCode.OK
                };
            }
            catch (AmazonS3Exception e)
            {
                return new Response
                {
                    HttpStatus = e.StatusCode,
                    ExceptionPayload = e
                };
            }
            catch (Exception e)
            {
                return new Response
                {
                    HttpStatus = HttpStatusCode.InternalServerError,
                    ExceptionPayload = e
                };
            }
        }
        public async Task<Response> awsS3_UploadFileAsync(
            AmazonS3Client inputS3Client,
            string inputS3Bucket,
            string inputS3FileKey,
            string inputFilePath,
            bool makeFilePublic = false)
        {
            try
            {
                PutObjectRequest objectRequest = new PutObjectRequest
                {

                    BucketName = inputS3Bucket,
                    Key = inputS3FileKey,
                    FilePath = inputFilePath
                };
                if (makeFilePublic) objectRequest.CannedACL = S3CannedACL.PublicRead;

                PutObjectResponse response = await inputS3Client.PutObjectAsync(objectRequest);

                return new Response
                {
                    HttpStatus = HttpStatusCode.OK
                };
            }
            catch (AmazonS3Exception e)
            {
                return new Response
                {
                    HttpStatus = e.StatusCode,
                    ExceptionPayload = e
                };
            }
            catch (Exception e)
            {
                return new Response
                {
                    HttpStatus = HttpStatusCode.InternalServerError,
                    ExceptionPayload = e
                };
            }
        }
        public async Task<Response> awsS3_UploadStreamAsync(
            AmazonS3Client inputS3Client,
            string inputS3Bucket,
            string inputS3FileKey,
            Stream inputFileStream,
            bool makeFilePublic = false)
        {
            try
            {
                PutObjectRequest objectRequest = new PutObjectRequest
                {

                    BucketName = inputS3Bucket,
                    Key = inputS3FileKey,
                    InputStream = inputFileStream
                };
                if (makeFilePublic) objectRequest.CannedACL = S3CannedACL.PublicRead;

                PutObjectResponse response = await inputS3Client.PutObjectAsync(objectRequest);

                return new Response
                {
                    HttpStatus = HttpStatusCode.OK
                };
            }
            catch (AmazonS3Exception e)
            {
                return new Response
                {
                    HttpStatus = e.StatusCode,
                    ExceptionPayload = e
                };
            }
            catch (Exception e)
            {
                return new Response
                {
                    HttpStatus = HttpStatusCode.InternalServerError,
                    ExceptionPayload = e
                };
            }
        }
        public async Task<Response> awsS3_DeleteFileAsync(
            AmazonS3Client inputS3Client,
            string inputS3Bucket,
            string inputS3FileKey)
        {
            try
            {
                DeleteObjectRequest objectRequest = new DeleteObjectRequest
                {
                    BucketName = inputS3Bucket,
                    Key = inputS3FileKey
                };

                DeleteObjectResponse response = await inputS3Client.DeleteObjectAsync(objectRequest);

                return new Response
                {
                    HttpStatus = HttpStatusCode.OK
                };
            }
            catch (AmazonS3Exception e)
            {
                return new Response
                {
                    HttpStatus = e.StatusCode,
                    ExceptionPayload = e
                };
            }
            catch (Exception e)
            {
                return new Response
                {
                    HttpStatus = HttpStatusCode.InternalServerError,
                    ExceptionPayload = e
                };
            }
        }
    }
}

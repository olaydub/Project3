using System;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Util;
using Amazon.Runtime.Internal;
using System.Collections;

namespace UploadData
{
    class Program
    {
        static async Task Main(string[] args)
        {

            string plate1 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal1.jpg";
            string plate2 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal2.jpg";
            string plate3 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal3.jpg";
            string plate4 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal4.jpg";
            string plate5 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal5.jpg";
            string plate6 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\michigan.jpg";
            string plate7 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\ny.jpg";
            string plate8 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\oregon.jpg";

            List<string> plateList = new List<string>();
            plateList.Add(plate1);
            plateList.Add(plate2);
            plateList.Add(plate3);
            plateList.Add(plate4);
            plateList.Add(plate5);
            plateList.Add(plate6);
            plateList.Add(plate7);
            plateList.Add(plate8);

            foreach (string filePath in plateList)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("File not found.");
                    return;
                }
                else
                {
                    await UploadFileToS3(filePath, "jpeg");
                    Console.WriteLine("Uploading jpeg file: " + filePath);
                }
                Console.WriteLine("File upload completed successfully.");
            }       
        }

        static async Task UploadFileToS3(string filePath, string fileType)
        {
            // Get credentials to use to authenticate to AWS
            AWSCredentials credentials = GetAWSCredentialsByName("default");

            // Get object to interact with S3
            AmazonS3Client s3Client = new AmazonS3Client(credentials, RegionEndpoint.USEast1);

            // Set S3 bucket name and key for the uploaded file
            string bucketName = "plates69420";
            string key = Path.GetFileName(filePath);

            // Read file content
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Upload file to S3
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentType = "image/jpeg",
                InputStream = new MemoryStream(fileBytes),
            };
            await s3Client.PutObjectAsync(putRequest);

        }
        private static AWSCredentials GetAWSCredentialsByName(string profileName)
        {
            if (String.IsNullOrEmpty(profileName))
            {
                throw new ArgumentNullException("profileName cannot be null or empty");
            }

            SharedCredentialsFile credFile = new SharedCredentialsFile();
            CredentialProfile profile = credFile.ListProfiles().Find(p => p.Name.Equals(profileName));

            if (profile == null)
            {
                throw new Exception(String.Format("Profile named {0} not found", profileName));
            }
            return AWSCredentialsFactory.GetAWSCredentials(profile, new SharedCredentialsFile());
        }

    }
}

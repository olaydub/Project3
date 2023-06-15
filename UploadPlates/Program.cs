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
using System.Runtime.CompilerServices;

namespace UploadData
{
    class Program
    {
        static async Task Main(string[] args)
        {

            //Hard coded filepaths, intersections, etc...

            //get list of license plate filepaths
            List<string> plateList = getPlates();

            //get queue of intersections
            Queue<string> intersectionList = getIntersections();

            //get queue of types
            Queue<string> typeList = getTypes();


            //have to add location, dateTime, and Type
            //Location: intersection address
            //DateTime: DateTime.Now
            //Type: no_stop - 300.00
            //      no_full_stop_on_right - 75.00
            //      no_right_onred - 125.00


            foreach (string filePath in plateList)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("File not found.");
                    return;
                }
                else
                {
                    string intersection = intersectionList.Dequeue();
                    string type = typeList.Dequeue();
                    await UploadFileToS3(filePath, "jpeg", intersection, type);
                    Console.WriteLine("Uploading jpeg file: " + filePath);
                }
                Console.WriteLine("File upload completed successfully.");
            }
        }

        static async Task UploadFileToS3(string filePath, string fileType, string intersection, string type)
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

            // Add tags to the upload
            putRequest.TagSet = new List<Tag>
            {
                new Tag { Key = "Location", Value = intersection },
                new Tag { Key = "DateTime", Value = DateTime.Now.ToString() },
                new Tag { Key = "Type", Value = type }
            };

            await s3Client.PutObjectAsync(putRequest);
        }


        /*
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
        */
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

        private static List<string> getPlates()
        {
            //put file paths for all license plates here
            string plate1 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal_plate1.jpg";
            string plate2 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal_plate2.jpg";
            string plate3 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal_plate3.jpg";
            string plate4 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal_plate4.jpg";
            string plate5 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\cal_plate5.jpg";
            string plate6 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\michigan_plate6.jpg";
            string plate7 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\ny_plate7.jpg";
            string plate8 = "C:\\Users\\Morpheus\\Documents\\BC\\Cloud\\Project3\\Project3\\LicencePlates\\oregon_plate8.jpg";

            List<string> plateList = new List<string>();
            plateList.Add(plate1);
            plateList.Add(plate2);
            plateList.Add(plate3);
            plateList.Add(plate4);
            plateList.Add(plate5);
            plateList.Add(plate6);
            plateList.Add(plate7);
            plateList.Add(plate8);

            return plateList;
        }

        //8 elements - one for each plate
        private static Queue<string> getIntersections()
        {
            Queue<string> intersection = new Queue<string>();
            intersection.Enqueue("Main St and 116th Ave intersection - Bellevue");
            intersection.Enqueue("1st St and 117th Ave intersection - Bellevue");
            intersection.Enqueue("2nd St and 118th Ave intersection - Bellevue");
            intersection.Enqueue("3rd St and 119th Ave intersection - Bellevue");
            intersection.Enqueue("4th St and 120th Ave intersection - Bellevue");
            intersection.Enqueue("5th St and 121th Ave intersection - Bellevue");
            intersection.Enqueue("6th St and 122th Ave intersection - Bellevue");
            intersection.Enqueue("7th St and 123th Ave intersection - Bellevue");

            return intersection;

        }


        //Type: no_stop - 300.00
        //      no_full_stop_on_right - 75.00
        //      no_right_onred - 125.00
        private static Queue<string> getTypes()
        {
            Queue<string> types = new Queue<string>();
            types.Enqueue("no_stop");
            types.Enqueue("no_full_stop_on_right");
            types.Enqueue("no_right_onred");
            types.Enqueue("no_stop");
            types.Enqueue("no_full_stop_on_right");
            types.Enqueue("no_right_onred");
            types.Enqueue("no_stop");
            types.Enqueue("no_full_stop_on_right");

            return types;

        }

    }
}

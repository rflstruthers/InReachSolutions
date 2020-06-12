using System;
using System.Web;
using System.Web.Mvc;
using Amazon.S3;
using System.IO;
using Amazon;
using System.Configuration;
using Amazon.S3.Model;
using System.Net.Mail;
using System.Net;

namespace S3FileUpload.Controllers
{
    public class HomeController : Controller
    {
        private static readonly string bucketName = ConfigurationManager.AppSettings["BucketName"];
        private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USWest2;
        private static readonly string accesskey = ConfigurationManager.AppSettings["AWSAccessKey"];
        private static readonly string secretkey = ConfigurationManager.AppSettings["AWSSecretKey"];
        private static readonly string Gpassword = ConfigurationManager.AppSettings["GooglePassword"];
        private static IAmazonS3 s3Client;

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile(HttpPostedFileBase file, string emailAddress)
        {
            try
            {
                // upload file to app
                string _FileName = Path.GetFileName(file.FileName);
                string _path = Path.Combine(Server.MapPath("~/UploadedFiles"), _FileName);
                file.SaveAs(_path);

                // upload file to S3
                s3Client = new AmazonS3Client(bucketRegion);
                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    //Key = keyName,
                    FilePath = _path,
                    ContentType = "text/plain"
                };
                PutObjectResponse response = s3Client.PutObject(putRequest);

                // generate presigned URL for file
                string urlString = "";
                try
                {
                    GetPreSignedUrlRequest request1 = new GetPreSignedUrlRequest
                    {
                        BucketName = bucketName,
                        Key = _FileName,
                        Expires = DateTime.Now.AddHours(24)
                    };
                    urlString = s3Client.GetPreSignedURL(request1);
                }

                catch (AmazonS3Exception e)
                {
                    Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                }

                // send email
                var senderEmail = new MailAddress("rflstrutherstest@gmail.com", "Sender");
                var receiverEmail = new MailAddress(emailAddress, "Receiver");
                var password = Gpassword;
                var subject = "Pre Signed URL";
                var body = urlString;
                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(senderEmail.Address, password)
                };
                using (var mess = new MailMessage(senderEmail, receiverEmail)
                {
                    Subject = subject,
                    Body = body
                })
                smtp.Send(mess);

                ViewBag.Message = "File uploaded successfully. An email has been sent to " + emailAddress + 
                    " containing a link to the uploaded file.";
                return View();
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                ViewBag.Message = "File upload failed.";
                return View();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                ViewBag.Message = "File upload failed.";
                return View();
            }
        }
    }
}
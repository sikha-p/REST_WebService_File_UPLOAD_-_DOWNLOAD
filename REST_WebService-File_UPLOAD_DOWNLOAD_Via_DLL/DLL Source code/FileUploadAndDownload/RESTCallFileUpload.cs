﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace FileUploadAndDownload
{
    public class RESTCallFileUpload
    {
        private static readonly Encoding encoding = Encoding.UTF8;
    
        public static string FileUploadRequest(string uri, string token, string paramsJson)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(paramsJson);
                Dictionary<string, object> postParameters = new Dictionary<string, object>();
                foreach (var data in dict)
                {
                    if (data.Key == "file")
                    {
                        string filePath = data.Value;
                        string filename = Path.GetFileName(filePath);
                        FileParameter f = new FileParameter(File.ReadAllBytes(filePath), filename, "multipart/form-data");
                        postParameters.Add("file", f);
                    }
                    else
                    {
                        postParameters.Add(data.Key, data.Value);
                    }
                }
                string response = MultipartFormDataPost(uri, postParameters, token);


                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

       
        public static string MultipartFormDataPost(string uri, Dictionary<string, object> postParameters, string token)
        {
            try
            {
                string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
                string contentType = "multipart/form-data; boundary=" + formDataBoundary;

                byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

                return PostForm(uri, contentType, formData, token);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public static string PostForm(string postUrl, string contentType, byte[] formData, string token)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = contentType;
           // request.Headers.Add("X-Authorization", token);
            request.Headers.Add("Authorization", token);
            request.ContentLength = formData.Length;


            // Send the form data to the request.
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
            }

            StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream());
            string Result = sr.ReadToEnd();
            return Result;
        }

        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;
                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }


        public class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }

        }
        public class param
        {
            public string file { get; set; }
            public string overwriteOption { get; set; }

            public string productionVersionOption { get; set; }

            public string password { get; set; }
        }
   


    
    }
}


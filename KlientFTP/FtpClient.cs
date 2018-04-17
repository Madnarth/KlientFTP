using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Collections;

namespace KlientFTP
{
    class FtpClient
    {
        #region Pola
        private string host;
        private string username;
        private string password;
        private string ftpPath;
        private bool downloadCompleted;
        private bool uploadCompleted;
        #endregion

        #region Własności
        public string Host
        {
            get
            {
                return host;
            }
            set
            {
                host = value;
            }
        }
        public string UserName
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
            }
        }
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }
        public string FtpDirectory
        {
            get
            {
                if (ftpPath.StartsWith("ftp://"))
                    return ftpPath;
                else
                    return "ftp://" + ftpPath;
            }
            set
            {
                ftpPath = value;
            }
        }
        public bool DownloadCompleted
        {
            get
            {
                return downloadCompleted;
            }
            set
            {
                downloadCompleted = value;
            }
        }
        public bool UploadCompleted
        {
            get
            {
                return uploadCompleted;
            }
            set
            {
                uploadCompleted = value;
            }
        }
        #endregion

        public FtpClient(string host, string userName, string password)
        {
            this.host = host;
            this.username = userName;
            this.password = password;
            ftpPath = "ftp://" + this.host;
        }
        public FtpClient()
        {
            downloadCompleted = true;
            uploadCompleted = true;
        }

        public ArrayList GetDirectories()
        {
            ArrayList directories = new ArrayList();
            FtpWebRequest request;
            try
            {
                request = (FtpWebRequest)WebRequest.Create(ftpPath);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                request.Credentials = new NetworkCredential(this.username, this.password);
                request.KeepAlive = false;
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Stream stream = response.GetResponseStream();
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string directory;
                        while ((directory = reader.ReadLine()) != null)
                        {
                            directories.Add(directory);
                        }
                    }
                }
                return directories;
                }
            catch
            {
                throw new Exception("Błąd: Nie można nawiązać połączenia z " + host);
            }
        }

        public ArrayList ChangeDirectory(string DirectoryName)
        {
            ftpPath += "/" + DirectoryName;
            return GetDirectories();
        }
        public ArrayList ChangeDirectoryUp()
        {
            if (ftpPath != "ftp://" + host)
            {
                ftpPath = ftpPath.Remove(ftpPath.LastIndexOf("/"), ftpPath.Length - ftpPath.LastIndexOf("/"));
                return GetDirectories();
            }
            else
                return GetDirectories();
        }

        public void DownloadFileAsync(string ftpFileName, string localFileName)
        {
            WebClient client = new WebClient();
            try
            {
                Uri uri = new Uri(ftpPath + "/" + ftpFileName);
                FileInfo file = new FileInfo(localFileName);
                if (file.Exists)
                    throw new Exception("Błąd: Plik " + localFileName + " istnieje");
                else
                {
                    client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(client_DownloadFileCompleted);
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.Credentials = new NetworkCredential(this.UserName,this.password);
                    client.DownloadFileAsync(uri, localFileName);
                    downloadCompleted = false;
                }
            }
            catch
            {
                client.Dispose();
                throw new Exception("Błąd: Pobranie pliku niemożliwe");
            }
        }
        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.OnDownloadProgressChanged(sender, e);
        }
        void client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            this.OnDownloadCompleted(sender, e);
        }
        #region Zdarzenia
            public delegate void DownProgressChangedEventHandler(object sender, DownloadProgressChangedEventArgs e);
            public event DownProgressChangedEventHandler DownProgressChanged;
            protected virtual void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
            {
                if (DownProgressChanged != null) DownProgressChanged(sender, e);
            }

            public delegate void DownCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
            public event DownCompletedEventHandler DownCompleted;
            protected virtual void OnDownloadCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
            {
                if (DownCompleted != null) DownCompleted(sender, e);
            }

        public delegate void UpCompletedEventHandler(object sender, UploadFileCompletedEventArgs e);
        public event UpCompletedEventHandler UpCompleted;
        protected virtual void OnUploadCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            if (UpCompleted != null) UpCompleted(sender, e);
        }

        public delegate void UpProgressChangedEventHandler(object sender, UploadProgressChangedEventArgs e);
        public event UpProgressChangedEventHandler UpProgressChanged;
        protected virtual void OnUploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            if (UpProgressChanged != null) UpProgressChanged(sender, e);
        }
        #endregion
        public void UploadFileAsync(string FileName)
        {
            try
            {
                System.Net.Cache.RequestCachePolicy cache = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.Reload);
                WebClient client = new WebClient();
                FileInfo file = new FileInfo(FileName);
                Uri uri = new Uri((FtpDirectory + '/' + file.Name).ToString());
                client.Credentials = new NetworkCredential(this.username, this.password);
                uploadCompleted = false;
                if (file.Exists)
                {
                    client.UploadFileCompleted += new UploadFileCompletedEventHandler(client_UploadFileCompleted);
                    client.UploadProgressChanged += new UploadProgressChangedEventHandler(client_UploadProgressChanged);
                    client.UploadFileAsync(uri, FileName);
                }
            }
            catch
            {
                throw new Exception("Błąd: Nie można wysłać pliku");
            }
        }
        void client_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            this.OnUploadProgressChanged(sender, e);
        }
        void client_UploadFileCompleted(object sender, UploadFileCompletedEventArgs e)
        {
            this.OnUploadCompleted(sender, e);
        }
        public string DeleteFile(string nazwa)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpPath + "//" + nazwa);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(this.username, this.password);
                request.KeepAlive = false;
                request.UsePassive = true;
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                return response.StatusDescription;
            }
            catch (Exception ex)
            {
                throw new Exception("Błąd: Nie można usunąć pliku " + nazwa + " (" + ex.Message + ")");
            }
        }
    }
}

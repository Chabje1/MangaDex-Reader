using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Net;
using System.Configuration;
using System.IO;
using Newtonsoft.Json;

namespace MangaDexReader
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : MetroWindow
    {
        public MainWindow parent;

        public LoginWindow(MainWindow _parent)
        {
            Hide();
            InitializeComponent();
            parent = _parent;
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameTextBox.Text == "" || PasswordTextBox.Text == "") return;
            HttpWebRequest webRequest = WebRequest.CreateHttp(String.Format("{0}auth/login", ConfigurationManager.AppSettings.Get("API_URL")));
            if (webRequest == null)
            {
                return;
            }
            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";
            webRequest.Method = "POST";

            byte[] data = Encoding.UTF8.GetBytes("{"+String.Format("\"username\":\"{0}\",\"password\":\"{1}\"", new string[] { UsernameTextBox.Text , PasswordTextBox.Text})+"}");

            webRequest.ContentLength = data.Length;

            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            AUTH_RESPONSE currentAUTH;
            using (Stream s = webRequest.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    string responseJSON = sr.ReadToEnd();
                    currentAUTH = JsonConvert.DeserializeObject<AUTH_RESPONSE>(responseJSON);
                }
            }

            if(currentAUTH.result == "ko")
            {
                GoButton.Content = "Wrong Username/Password";
            }
            else
            {
                parent.LoginButton.Content = "Logout";
                parent.isLoggedIn = true;
                parent.currentToken = currentAUTH.token;
                parent.RefreshTokenTimer.Start();
                parent.JWTokenTimer.Start();
                parent.GetUserFeedButton.Visibility = Visibility.Visible;
                Hide();
            }
        }

        private void LoginWindow_ContentRendered(object sender, EventArgs e)
        {
            GoButton.Content = "Go";
        }
    }
}

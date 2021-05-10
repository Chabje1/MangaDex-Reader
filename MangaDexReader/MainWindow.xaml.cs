using System;
using System.Collections.Generic;
using System.Net;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using ControlzEx.Theming;
using MangaDexReader.ResponseClasses;
using System.IO;
using System.Timers;

namespace MangaDexReader
{
    public class MDATHOME_RESPONSE
    {
        public string baseUrl;
    }

    public class AUTH_RESPONSE
    {
        public string result;
        public JWToken token;
    }

    public class JWToken
    {
        public string session;
        public string refresh;
    }

    public class REFRESH_RESPONSE
    {
        public string result;
        public JWToken token;
        public string message;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MangaResponses currentList;
        public Manga currentManga;
        public ChapterResponses currentChapters;
        public Chapter currentChapter;
        public MDATHOME_RESPONSE currentATHOME;
        public int currentPage = 0;
        public int maxperpage = 10;
        public int listPage = 0;
        public int NumberOfPagesSearch = 0;

        public int maxChapPerPage = 10;
        public int currentChapterPage = 0;
        public int NumberOfPagesChapters = 0;

        public bool isLoggedIn = false;
        public bool isUserFeed = false;
        public bool isJWTokenExpired = true;
        public JWToken currentToken = null;
        public LoginWindow child;
        public Timer JWTokenTimer = new Timer(15 * 60 * 1000);
        public Timer RefreshTokenTimer = new Timer(4*60*60*1000);

        public bool DarkLight = false;

        public MainWindow()
        {
            InitializeComponent();
            GetMangaList("");
            child = new LoginWindow(this);
            RefreshTokenTimer.Elapsed += new ElapsedEventHandler(RefreshToken_Elasped);
            JWTokenTimer.Elapsed += new ElapsedEventHandler(TokenExpired_Elasped);
        }

        public void GetMangaList(string search)
        {
            HttpWebRequest webRequest;
            isUserFeed = false;
            if (search == "")
            {
                webRequest = WebRequest.CreateHttp(ConfigurationManager.AppSettings.Get("API_URL") + "manga?limit=" + maxperpage.ToString() + "&offset=" + (maxperpage * listPage).ToString());
            }
            else
            {
                search = HttpUtility.UrlEncode(search);
                webRequest = WebRequest.CreateHttp(ConfigurationManager.AppSettings.Get("API_URL") + "manga?title="+search+"&limit=" + maxperpage.ToString() + "&offset=" + (maxperpage * listPage).ToString());
            }
            if(webRequest == null)
            {
                return;
            }

            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";

            using (var s = webRequest.GetResponse().GetResponseStream())
            {
                using (var sr = new StreamReader(s))
                {
                    var responseJSON = sr.ReadToEnd();
                    currentList = JsonConvert.DeserializeObject<MangaResponses>(responseJSON);
                }
            }
            UpdateListUI();
        }

        public void UpdateListUI()
        {
            CurrentMangaListBox.Items.Clear();
            if (currentList == null)
            {
                MangaListPageCounter.Content = "0/0";
                return;
            }
            NumberOfPagesSearch = (int)Math.Ceiling(((float)currentList.total) / maxperpage);
            MangaListPageCounter.Content = (listPage + 1).ToString() + "/" + NumberOfPagesSearch.ToString();

            foreach (MangaResponse manga in currentList.results)
            {
                string bufout = null;
                manga.data.attributes.title.TryGetValue("en", out bufout);
                if (bufout != null)
                {
                    bufout = WebUtility.HtmlDecode(bufout);
                    CurrentMangaListBox.Items.Add(bufout);
                }
            }
        }

        public void GetCurrentChapterList()
        {
            HttpWebRequest webRequest = WebRequest.CreateHttp(String.Format("{0}manga/{1}/feed?locales[0]=en&limit={2}&offset={3}", new string[] { ConfigurationManager.AppSettings.Get("API_URL"), currentManga.id, maxChapPerPage.ToString(), (maxChapPerPage * currentChapterPage).ToString() }));
            if (webRequest == null)
            {
                return;
            }

            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";

            using (var s = webRequest.GetResponse().GetResponseStream())
            {
                using (var sr = new StreamReader(s))
                {
                    var responseJSON = sr.ReadToEnd();
                    currentChapters = JsonConvert.DeserializeObject<ChapterResponses>(responseJSON);
                }
            }
            CurrentChaptersListBox.Items.Clear();
            NumberOfPagesChapters = (int)Math.Ceiling(((float)currentChapters.total) / maxChapPerPage);
            CurrentMangaChapterCounter.Content = (currentChapterPage + 1).ToString() + "/" + NumberOfPagesChapters.ToString();
            if (currentChapters != null)
            {
                foreach (ChapterResponse chapter in currentChapters.results)
                {
                    if (chapter.data.attributes.title != "")
                    {
                        string title = WebUtility.HtmlDecode(chapter.data.attributes.title);
                        if (chapter.data.attributes.chapter != "")
                        {
                            CurrentChaptersListBox.Items.Add(String.Format("Chapter {0}: {1}", new string[] { chapter.data.attributes.chapter,  title}));
                        }
                        else
                        {
                            CurrentChaptersListBox.Items.Add(title);
                        }
                    }
                    else
                    {
                        CurrentChaptersListBox.Items.Add(String.Format("Chapter {0}", new string[] { chapter.data.attributes.chapter }));
                    }
                }
            }
        }

        private void CurrentMangaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentMangaListBox.SelectedIndex == -1) return;
            currentManga = currentList.results[CurrentMangaListBox.SelectedIndex].data;
            string bufout = null;
            currentManga.attributes.title.TryGetValue("en", out bufout);
            CurrentMangaTitle.Content = "Title (En): " + bufout;
            currentChapterPage = 0;

            GetCurrentChapterList();
        }

        private void CurrentChaptersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentChapters == null) return;
            if (CurrentChaptersListBox.SelectedIndex == -1) {
                PageViewer.Source = null;
                MaxPageCounter.Content = "0/0";
                return;
            };
            currentChapter = currentChapters.results[CurrentChaptersListBox.SelectedIndex].data;
            currentPage = 0;

            HttpWebRequest webRequest = WebRequest.CreateHttp(ConfigurationManager.AppSettings.Get("API_URL") + "at-home/server/" + currentChapter.id);
            if (webRequest == null)
            {
                return;
            }

            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";

            using (var s = webRequest.GetResponse().GetResponseStream())
            {
                using (var sr = new StreamReader(s))
                {
                    var responseJSON = sr.ReadToEnd();
                    currentATHOME = JsonConvert.DeserializeObject<MDATHOME_RESPONSE>(responseJSON);
                }
            }

            GetPage(0);
            MaxPageCounter.Content = "1/" + currentChapter.attributes.data.Length;
        }

        private void GetPage(int num)
        {
            BitmapImage bi3 = new BitmapImage();
            bi3.BeginInit();
            bi3.UriSource = new Uri(String.Format("{0}/data/{1}/{2}", new string[] { currentATHOME.baseUrl, currentChapter.attributes.hash, currentChapter.attributes.data[num] }), UriKind.RelativeOrAbsolute);
            bi3.CacheOption = BitmapCacheOption.OnLoad;
            bi3.EndInit();
            PageViewer.Source = bi3;
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if(currentPage < currentChapter.attributes.data.Length-1)
            {
                currentPage++;
                GetPage(currentPage);
            }
            else if (CurrentChaptersListBox.SelectedIndex < currentChapters.total - 1)
            {
                CurrentChaptersListBox.SelectedIndex += 1;
            }
            MaxPageCounter.Content = (currentPage+1).ToString() + "/" + currentChapter.attributes.data.Length;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 0)
            {
                currentPage--;
                GetPage(currentPage);
            }
            else if (CurrentChaptersListBox.SelectedIndex > 0)
            {
                CurrentChaptersListBox.SelectedIndex -= 1;
                currentPage = currentChapter.attributes.data.Length - 1;
                GetPage(currentPage);
            }
            MaxPageCounter.Content = (currentPage + 1).ToString() + "/" + currentChapter.attributes.data.Length;
        }

        private void MetroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                NextPage_Click(sender, e);
            }
            else if(e.Key == Key.Right)
            {
                PrevPage_Click(sender, e);
            }
            else if(e.Key == Key.Enter)
            {
                listPage = 0;
                GetMangaList(SearchBox.Text);
            }
        }

        private void NextSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if(listPage < NumberOfPagesSearch - 1)
            {
                listPage++;
                if (isUserFeed)
                {
                    int a = 0;
                    GetUserFeed();
                }
                else
                {
                    GetMangaList(SearchBox.Text);
                }
            }
        }

        private void PrevSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (listPage > 0)
            {
                listPage--;
                if (isUserFeed)
                {
                    GetUserFeed();
                }
                else
                {
                    GetMangaList(SearchBox.Text);
                }
            }
        }

        private void PrevChapterPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChapterPage > 0)
            {
                currentChapterPage--;
                GetCurrentChapterList();
            }
        }

        private void NextChapterPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChapterPage < NumberOfPagesChapters-1)
            {
                currentChapterPage++;
                GetCurrentChapterList();
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoggedIn)
            {
                HttpWebRequest webRequest = WebRequest.CreateHttp(String.Format("{0}auth/logout", ConfigurationManager.AppSettings.Get("API_URL")));
                if (webRequest == null)
                {
                    return;
                }
                webRequest.ContentType = "application/json";
                webRequest.UserAgent = "Nothing";
                webRequest.Method = "POST";
                webRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + currentToken.session);

                using (Stream s = webRequest.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        string responseJSON = sr.ReadToEnd();
                        if(responseJSON == "{\"result\":\"ok\"}")
                        {
                            Logout();
                        }
                    }
                }
            }
            else
            {
                child.Show();
            }
        }

        public void Logout()
        {
            isLoggedIn = false;
            currentToken = null;
            LoginButton.Content = "Login";
            GetUserFeedButton.Visibility = Visibility.Hidden;
            isJWTokenExpired = true;

            RefreshTokenTimer.Stop();
            JWTokenTimer.Stop();
        }

        public void GetNewAuthToken()
        {
            if (!isLoggedIn) return;

            HttpWebRequest webRequest = WebRequest.CreateHttp(String.Format("{0}auth/refresh", ConfigurationManager.AppSettings.Get("API_URL")));
            if (webRequest == null)
            {
                return;
            }
            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";
            webRequest.Method = "POST";

            byte[] data = Encoding.UTF8.GetBytes("{" + String.Format("\"token\":\"{0}\"\"", currentToken.refresh) + "}");

            webRequest.ContentLength = data.Length;

            using (var stream = webRequest.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            REFRESH_RESPONSE refresh_response;
            using (Stream s = webRequest.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    string responseJSON = sr.ReadToEnd();
                    refresh_response = JsonConvert.DeserializeObject<REFRESH_RESPONSE>(responseJSON);
                }
            }

            if(refresh_response.result == "ok")
            {
                currentToken = refresh_response.token;
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            child.Close();
        }

        private void GetUserFeedButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 0;
            GetUserFeed();
        }

        public void GetUserFeed()
        {
            if (!isLoggedIn) return;
            isUserFeed = true;
            HttpWebRequest webRequest = WebRequest.CreateHttp(String.Format("{0}user/follows/manga/feed?limit={1}&offset={2}&locales[]=en", ConfigurationManager.AppSettings.Get("API_URL"), maxperpage, maxperpage * listPage));
            if (webRequest == null)
            {
                return;
            }
            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";
            webRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + currentToken.session);

            ChapterResponses chapterResponses;

            using (Stream s = webRequest.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    string responseJSON = sr.ReadToEnd();
                    chapterResponses = JsonConvert.DeserializeObject<ChapterResponses>(responseJSON);
                }
            }

            List<string> mangaIDs = new List<string>();

            foreach (ChapterResponse chap in chapterResponses.results)
            {
                string mangaID = "";
                foreach (Relationship rel in chap.relationships)
                {
                    if (rel.type == "manga") mangaID = rel.id;
                }
                mangaIDs.Add(mangaID);
            }

            MangaResponses temp = new MangaResponses
            {
                total = chapterResponses.total,
                limit = chapterResponses.limit,
                offset = chapterResponses.offset,
                results = GetMangaFromIDs(mangaIDs)
            };

            currentList = temp;

            UpdateListUI();
        }

        private void RefreshToken_Elasped(object sender, ElapsedEventArgs e)
        {
            Logout();
    }

        private void TokenExpired_Elasped(object sender, ElapsedEventArgs e)
        {
            isJWTokenExpired = false;
        }

        public MangaResponse GetMangaFromID(string id)
        {
            HttpWebRequest webRequest = WebRequest.CreateHttp(String.Format("{0}manga/{1}", ConfigurationManager.AppSettings.Get("API_URL"), id));
            if (webRequest == null)
            {
                return new MangaResponse();
            }
            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";

            MangaResponse mangaResponse;

            using (Stream s = webRequest.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    string responseJSON = sr.ReadToEnd();
                    mangaResponse = JsonConvert.DeserializeObject<MangaResponse>(responseJSON);
                }
            }

            return mangaResponse;
        }

        public List<MangaResponse> GetMangaFromIDs(List<string> ids)
        {
            string queryParam = "";

            foreach(string id in ids)
            {
                queryParam += "&ids[]=" + id;
            }
            queryParam = queryParam.Remove(0, 1);
            queryParam = "?" + queryParam;
            HttpWebRequest webRequest = WebRequest.CreateHttp(String.Format("{0}manga{1}", ConfigurationManager.AppSettings.Get("API_URL"), queryParam));
            if (webRequest == null)
            {
                return new List<MangaResponse>();
            }
            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "Nothing";

            MangaResponses mangaResponses;

            using (Stream s = webRequest.GetResponse().GetResponseStream())
            {
                using (StreamReader sr = new StreamReader(s))
                {
                    string responseJSON = sr.ReadToEnd();
                    mangaResponses = JsonConvert.DeserializeObject<MangaResponses>(responseJSON);
                }
            }

            return mangaResponses.results;
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!DarkLight)
            {
                ThemeManager.Current.ChangeTheme(this, "Dark.Blue");
                ThemeIcon.Kind = MahApps.Metro.IconPacks.PackIconBoxIconsKind.RegularSun;
            }
            else
            {
                ThemeManager.Current.ChangeTheme(this, "Light.Blue");
                ThemeIcon.Kind = MahApps.Metro.IconPacks.PackIconBoxIconsKind.SolidMoon;
            }
            DarkLight ^= true;
        }
    }
}

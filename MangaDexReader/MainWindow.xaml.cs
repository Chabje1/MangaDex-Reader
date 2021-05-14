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
        public List<MangaResponse> currentMangaList;
        public Manga currentManga;
        public List<ChapterResponse> currentChapters;
        public Chapter currentChapter;

        public MDATHOME_RESPONSE currentATHOME;

        public int currentPage = 0;
        public int maxperpage = 10;
        public int listPage = 0;
        public int NumberOfPagesSearch = 0;

        public int maxChapPerPage = 10;
        public int currentChapterPage = 0;
        public int NumberOfPagesChapters = 0;

        public List<BitmapImage> pageList;

        public bool isLoggedIn = false;
        public bool isUserFeed = false;
        public bool isJWTokenExpired = true;
        public JWToken currentToken = null;
        public LoginWindow loginChild;
        public Timer JWTokenTimer = new Timer(15 * 60 * 1000);
        public Timer RefreshTokenTimer = new Timer(4 * 60 * 60 * 1000);

        public bool DarkLight = false;

        public ProgressWindow progressWindow;

        public MainWindow()
        {
            InitializeComponent();
            loginChild = new LoginWindow(this);
            progressWindow = new ProgressWindow();
            RefreshTokenTimer.Elapsed += new ElapsedEventHandler(RefreshToken_Elasped);
            JWTokenTimer.Elapsed += new ElapsedEventHandler(TokenExpired_Elasped);
        }

        public void GetMangaList(string search)
        {
            isUserFeed = false;
            currentMangaList = new List<MangaResponse>();

            int limit = 100;

            int total = limit;
            int current = 0;

            string reqQuery;
            MangaResponses mangaResponses;
            HttpWebRequest webRequest;
            if (search == "")
            {
                reqQuery = ConfigurationManager.AppSettings.Get("API_URL") + String.Format("manga?limit={0}&offset={1}", limit, current);
            }
            else
            {
                search = HttpUtility.UrlEncode(search);
                reqQuery = ConfigurationManager.AppSettings.Get("API_URL") + String.Format("manga?title={0}&limit={1}&offset={2}", search, limit, current);
            }
            while (current < total)
            {
                webRequest = WebRequest.CreateHttp(reqQuery);
                if (webRequest == null)
                {
                    return;
                }
                webRequest.ContentType = "application/json";
                webRequest.UserAgent = "Nothing";
                
                using (Stream s = webRequest.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        string responseJSON = sr.ReadToEnd();
                        mangaResponses = JsonConvert.DeserializeObject<MangaResponses>(responseJSON);
                    }
                }

                currentMangaList.AddRange(mangaResponses.results);

                total = mangaResponses.total;
                current += (limit < total - current) ? limit : (total - current);
                this.Invoke(() => UpdateBar(current, total));
                System.Threading.Thread.Sleep(100);
            }
            this.Invoke(EndOfGet);
        }

        public void UpdateListUI()
        {
            CurrentMangaListBox.Items.Clear();
            if (currentMangaList == null)
            {
                MangaListPageCounter.Content = "0/0";
                return;
            }
            NumberOfPagesSearch = (int)Math.Ceiling(((float)currentMangaList.Count) / maxperpage);
            MangaListPageCounter.Content = (listPage + 1).ToString() + "/" + NumberOfPagesSearch.ToString();

            for (int i = listPage * maxperpage; i < (listPage + 1) * maxperpage && i < currentMangaList.Count; i++)
            {
                currentMangaList[i].data.attributes.title.TryGetValue("en", out string bufout);
                if (bufout != null)
                {
                    bufout = WebUtility.HtmlDecode(bufout);
                    CurrentMangaListBox.Items.Add(bufout);
                }
            }
        }

        public void GetCurrentChapterList()
        {

            currentChapters = new List<ChapterResponse>();

            int limit = 500;

            int total = limit;
            int current = 0;

            string reqQuery;
            ChapterResponses chapterResponses;
            HttpWebRequest webRequest;

            while (current < total)
            {
                reqQuery = String.Format("{0}manga/{1}/feed?locales[]=en&limit={2}&offset={3}", ConfigurationManager.AppSettings.Get("API_URL"), currentManga.id, limit, current);

                webRequest = WebRequest.CreateHttp(reqQuery);
                if (webRequest == null)
                {
                    return;
                }
                webRequest.ContentType = "application/json";
                webRequest.UserAgent = "Nothing";

                using (Stream s = webRequest.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        string responseJSON = sr.ReadToEnd();
                        chapterResponses = JsonConvert.DeserializeObject<ChapterResponses>(responseJSON);
                    }
                }

                currentChapters.AddRange(chapterResponses.results);

                total = chapterResponses.total;
                current += (limit < total - current) ? limit : (total - current);
                this.Invoke(() => UpdateBar(current, total));
            }

            currentChapters.Sort((x,y)=> (int)(10000 * ((x.data.attributes.volume.HasValue && y.data.attributes.volume.HasValue)?(x.data.attributes.volume - y.data.attributes.volume):0) + 100*(float.Parse(x.data.attributes.chapter) - float.Parse(y.data.attributes.chapter))));

            this.Invoke(EndOfGet);
            this.Invoke(UpdateChapterListUI);
        }

        public void UpdateChapterListUI()
        {
            CurrentChaptersListBox.Items.Clear();
            NumberOfPagesChapters = (int)Math.Ceiling(((float)currentChapters.Count) / maxChapPerPage);
            CurrentMangaChapterCounter.Content = (currentChapterPage + 1).ToString() + "/" + NumberOfPagesChapters.ToString();
            if (currentChapters != null)
            {
                for(int i=currentChapterPage*maxChapPerPage; i<(currentChapterPage+1)*maxChapPerPage && i<currentChapters.Count; i++)
                {
                    ChapterResponse chapter = currentChapters[i];
                    string title = WebUtility.HtmlDecode(chapter.data.attributes.title);
                    string buf="";
                    if (chapter.data.attributes.volume != null) buf = "Volume " + chapter.data.attributes.volume;
                    if (chapter.data.attributes.chapter != "") buf = (buf != "")?(buf+", Chapter "+ chapter.data.attributes.chapter) : ("Chapter " + chapter.data.attributes.chapter);
                    if (chapter.data.attributes.title != "") buf = (buf != "") ? (buf + ": " + title) : title;
                    CurrentChaptersListBox.Items.Add(buf);
                }
            }
        }

        private void CurrentMangaListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentMangaListBox.SelectedIndex == -1) return;
            currentManga = currentMangaList[listPage*maxperpage+CurrentMangaListBox.SelectedIndex].data;
            currentManga.attributes.title.TryGetValue("en", out string bufout);
            CurrentMangaTitle.Content = "Title (En): " + bufout;
            currentChapterPage = 0;

            progressWindow.Show();
            progressWindow.ProgressBar.Value = 0;
            progressWindow.ProgressCounter.Content = "Beginning";
            new Task(GetCurrentChapterList).Start();
        }

        private void CurrentChaptersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentChapters == null) return;
            if (CurrentChaptersListBox.SelectedIndex == -1) {
                PageViewer.Source = null;
                MaxPageCounter.Content = "0/0";
                return;
            };
            currentChapter = currentChapters[CurrentChaptersListBox.SelectedIndex].data;
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

            GetPages();
            MaxPageCounter.Content = "1/" + currentChapter.attributes.data.Length;
            PageViewer.Source = pageList[0];
            NextButton.Focus();
        }

        public void GetPages()
        {
            if (currentChapter == null) return;
            BitmapImage bi3;
            pageList = new List<BitmapImage>();
            foreach(string imageLink in currentChapter.attributes.data)
            {
                bi3 = new BitmapImage();
                bi3.BeginInit();
                bi3.UriSource = new Uri(String.Format("{0}/data/{1}/{2}", new string[] { currentATHOME.baseUrl, currentChapter.attributes.hash, imageLink }), UriKind.RelativeOrAbsolute);
                bi3.CacheOption = BitmapCacheOption.OnLoad;
                bi3.EndInit();
                pageList.Add(bi3);
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (pageList == null || pageList.Count == 0) return;

            if (currentPage < currentChapter.attributes.data.Length-1)
            {
                currentPage++;
                PageViewer.Source = pageList[currentPage];
            }
            else if (CurrentChaptersListBox.SelectedIndex < currentChapters.Count - 1)
            {
                CurrentChaptersListBox.SelectedIndex += 1;
            }
            MaxPageCounter.Content = (currentPage+1).ToString() + "/" + currentChapter.attributes.data.Length;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (pageList == null || pageList.Count == 0) return;

            if (currentPage > 0)
            {
                currentPage--;
                PageViewer.Source = pageList[currentPage];
            }
            else if (CurrentChaptersListBox.SelectedIndex > 0)
            {
                CurrentChaptersListBox.SelectedIndex -= 1;
                currentPage = currentChapter.attributes.data.Length - 1;
                PageViewer.Source = pageList[currentPage];
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
                string search = SearchBox.Text;
                progressWindow.Show();
                progressWindow.ProgressBar.Value = 0;
                progressWindow.ProgressCounter.Content = "Beginning";
                new Task(()=>GetMangaList(search)).Start();
            }
        }

        private void NextSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if(listPage < NumberOfPagesSearch - 1)
            {
                listPage++;
                UpdateListUI();
            }
        }

        private void PrevSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (listPage > 0)
            {
                listPage--;
                UpdateListUI();
            }
        }

        private void PrevChapterPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChapterPage > 0)
            {
                currentChapterPage--;
                UpdateChapterListUI();
            }
        }

        private void NextChapterPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentChapterPage < NumberOfPagesChapters-1)
            {
                currentChapterPage++;
                UpdateChapterListUI();
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
                loginChild.Show();
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
            loginChild.Close();
            progressWindow.Close();
        }

        private void GetUserFeedButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 0;
            progressWindow.Show();
            progressWindow.ProgressBar.Value = 0;
            progressWindow.ProgressCounter.Content = "Beginning";
            new Task(GetUserFeed).Start();
        }

        public void GetUserFeed()
        {
            if (!isLoggedIn) return;
            isUserFeed = true;

            currentMangaList = new List<MangaResponse>();

            int limit = 100;

            int total = limit;
            int current = 0;

            string reqQuery;
            List<string> mangaIDs;
            string mangaID;
            ChapterResponses chapterResponses;
            HttpWebRequest webRequest;

            while (current < total)
            {
                reqQuery = ConfigurationManager.AppSettings.Get("API_URL") + String.Format("user/follows/manga/feed?locales[]=en&limit={0}&offset={1}", limit, current);
                webRequest = WebRequest.CreateHttp(reqQuery);
                if (webRequest == null)
                {
                    return;
                }
                webRequest.ContentType = "application/json";
                webRequest.UserAgent = "Nothing";

                webRequest.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + currentToken.session);

                using (Stream s = webRequest.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(s))
                    {
                        string responseJSON = sr.ReadToEnd();
                        chapterResponses = JsonConvert.DeserializeObject<ChapterResponses>(responseJSON);
                    }
                }

                mangaIDs = new List<string>();

                foreach (ChapterResponse chap in chapterResponses.results)
                {
                    mangaID = "";
                    foreach (Relationship rel in chap.relationships)
                    {
                        if (rel.type == "manga") mangaID = rel.id;
                    }
                    mangaIDs.Add(mangaID);
                }

                foreach (MangaResponse manga in GetMangaFromIDs(mangaIDs)) if (!currentMangaList.Exists(new Predicate<MangaResponse>(x => x.data.attributes.title["en"] == manga.data.attributes.title["en"]))) currentMangaList.Add(manga);

                total = chapterResponses.total;
                current += (limit < total - current) ? limit : (total - current);
                this.Invoke(() => UpdateBar(current, total));
            }
            this.Invoke(EndOfGet);
        }

        public void UpdateBar(int current, int total)
        {
            progressWindow.ProgressBar.Maximum = total;
            progressWindow.ProgressBar.Value = current;
            progressWindow.ProgressCounter.Content = current + "/" + total;
        }

        public void EndOfGet()
        {
            progressWindow.Hide();
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

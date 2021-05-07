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
using MangaDexReader.ResponseClasses;
using System.IO;

namespace MangaDexReader
{
    public class MDATHOME_RESPONSE
    {
        public string baseUrl;
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

        public MainWindow()
        {
            InitializeComponent();
            GetMangaList("");
        }

        public void GetMangaList(string search)
        {
            HttpWebRequest webRequest;
            if (search == "")
            {
                webRequest = WebRequest.Create(ConfigurationManager.AppSettings.Get("API_URL") + "manga?limit=" + maxperpage.ToString() + "&offset=" + (maxperpage * listPage).ToString()) as HttpWebRequest;
            }
            else
            {
                search = HttpUtility.UrlEncode(search);
                webRequest = WebRequest.Create(ConfigurationManager.AppSettings.Get("API_URL") + "manga?title="+search+"&limit=" + maxperpage.ToString() + "&offset=" + (maxperpage * listPage).ToString()) as HttpWebRequest;
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
            HttpWebRequest webRequest = WebRequest.Create(ConfigurationManager.AppSettings.Get("API_URL") + "chapter?manga=" + currentManga.id + "&order[publishAt]=desc&translatedLanguage=en&offset=" + (maxChapPerPage * currentChapterPage).ToString()) as HttpWebRequest;
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

            HttpWebRequest webRequest = WebRequest.Create(ConfigurationManager.AppSettings.Get("API_URL") + "at-home/server/" + currentChapter.id) as HttpWebRequest;
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
            MaxPageCounter.Content = (currentPage+1).ToString() + "/" + currentChapter.attributes.data.Length;
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 0)
            {
                currentPage--;
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
                GetMangaList(SearchBox.Text);
            }
        }

        private void PrevSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (listPage > 0)
            {
                listPage--;
                GetMangaList(SearchBox.Text);
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
    }
}

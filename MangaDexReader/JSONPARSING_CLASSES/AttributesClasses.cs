using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaDexReader
{
    public interface IAttributes
    {
    }

    public class TagAttributes : IAttributes
    {
        public Dictionary<string, string> name;
        public int version;
    }

    public class MangaAttributes : IAttributes
    {
        public Dictionary<string, string> title;
        public Dictionary<string, string>[] altTitles;
        public Dictionary<string, string> description;
        public bool isLocked;
        public Dictionary<string, string> links;
        public string originalLanguage;
        public string lastVolume;
        public string lastChapter;
        public string publicationDemographic;
        public string status;
        public int? year;
        public string contentRating;
        public Tag[] tags;
        public int version;
        public string createdAt;
        public string updatedAt;
    }

    public class ChapterAttributes : IAttributes
    {
        public string title;
        public int? volume;
        public string chapter;
        public string translatedLanguage;
        public string hash;
        public string[] data;
        public string[] dataSaver;
        public string uploader;
        public int version;
        public string createdAt;
        public string updatedAt;
        public string publishAt;
    }
}

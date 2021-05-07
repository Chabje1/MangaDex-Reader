using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaDexReader
{
    public class ResponseObject
    {
        public string id;
        public string type;
        public IAttributes attributes;
    }

    public class Manga : ResponseObject
    {
        public new MangaAttributes attributes;
    }

    public class Tag : ResponseObject
    {
        public new TagAttributes attributes;
    }

    public class Chapter : ResponseObject
    {
        public new ChapterAttributes attributes;
    }
}

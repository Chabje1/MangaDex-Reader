using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaDexReader.ResponseClasses
{
    public class Responses
    {
        public Response[] results;
        public int limit;
        public int offset;
        public int total;
    }

    public class Response
    {
        public string result;
        public ResponseObject data;
        public Relationship[] relationships;
    }

    public class Relationship
    {
        public string id;
        public string type;
    }

    public class MangaResponses : Responses
    {
        public new MangaResponse[] results;
    }

    public class MangaResponse : Response
    {
        public new Manga data;
    }

    public class ChapterResponses : Responses
    {
        public new ChapterResponse[] results;
    }

    public class ChapterResponse : Response
    {
        public new Chapter data;
    }
}

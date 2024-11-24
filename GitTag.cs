using System.Text.Json.Serialization;
using System.Text.Json;

namespace GitClone
{
    public class GitTag : GitObject
    {
        public string Object { get; set; }
        public string Type { get; set; }
        public string Tag { get; set; }
        public string Tagger { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public GitTag(byte[]? data = null) : base("tag", data) { }

        [JsonConstructor]
        public GitTag(string obj, string type, string tag, string tagger, string message) : base("tag")
        {
            this.Object = obj;
            this.Type = type;
            this.Tag = tag;
            this.Tagger = tagger;
            this.Message = message;
        }

        public override void Deserialize(byte[] jsonString)
        {
            var gitTag = JsonSerializer.Deserialize<GitTag>(jsonString);
            if (gitTag == null) throw new Exception("GitTag file error!");

            this.Object = gitTag.Object;
            this.Type = gitTag.Type;
            this.Tag = gitTag.Tag;
            this.Tagger = gitTag.Tagger;
            this.Message = gitTag.Message;
            this.Timestamp = gitTag.Timestamp;
        }

        public override byte[] Serialize()
        {
            return Data;
        }

        public override void Init()
        {
            throw new NotImplementedException();
        }
    }
}

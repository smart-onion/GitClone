using System.Text.Json.Serialization;

namespace GitClone
{
    public class Reference
    {
        public string Ref {  get; set; }
        [JsonConstructor]
        public Reference(string Ref) 
        {
            this.Ref = Ref;
        }
    }
}

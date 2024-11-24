using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GitClone
{
    public class GitObjectFile
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public byte[] Data { get; set; }

        public GitObjectFile() { }

        [JsonConstructor]
        public GitObjectFile(string name, int size, byte[] data) 
        {
            this.Name = name;
            this.Size = size;   
            this.Data = data;
        }
    }
}

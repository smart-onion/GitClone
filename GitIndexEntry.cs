using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone
{
    public class GitIndexEntry: ITreeLeaf
    {
        public bool Modified { get; set; }
        public GitIndexEntry(string path, string mode, string sha, bool modified = false) :base(path, mode, sha) 
        { 
            Modified = modified;
        }

        public override string ToString()
        {
            return $"{Mode} {Sha[..5]} {Path}";
        }
    }
}

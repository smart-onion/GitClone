namespace GitClone
{
    public class GitBlob : GitObject
    {
        public string FileContent { get; set; }
        public GitBlob(string? filePath = null) : base("blob") 
        {
            if (filePath != null)
            {
                if (File.Exists(filePath))
                {
                    this.Data = File.ReadAllBytes(filePath);
                }
               
            }
        }
        
        public override void Deserialize(byte[] file)
        {
           this.Data = file;
        }

        public override void Init()
        {
            throw new NotImplementedException();
        }

        public override byte[] Serialize()
        {
            return this.Data;
        }


    }
}

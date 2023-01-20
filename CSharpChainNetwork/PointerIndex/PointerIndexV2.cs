using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CSharpChainModel;
namespace CSharpChainNetwork.PointerIndex
{
    public class PointerIndexV2
    {
        string pointerPath = "C:/temp/PointerSystem/IndexFiles/";
        public PointerIndexV2()
        { 
        }

        public void CreateIndex(HashSet<string> users, long blockNum)
        {
            foreach (string user in users)
            {
                string path = $"{pointerPath}{user}.dat";
                if (!File.Exists(path))
                {
                    BinaryWriter writer = new BinaryWriter(File.Create(path),Encoding.ASCII);
                    writer.Write(Encoding.ASCII.GetBytes(blockNum.ToString()));
                    writer.Close();
                }
            }
        }

        public void AppendIndex(HashSet<string> users, long blockNum)
        {
            foreach (string user in users)
            {
                string path = $"{pointerPath}{user}.dat";
                BinaryWriter writer = new BinaryWriter(File.Open(path,FileMode.Append),Encoding.ASCII);
                writer.Write(Encoding.ASCII.GetBytes($",{blockNum}"));
                writer.Close();
            }
        }

        public string[] ReadIndex(string key) 
        {
            BinaryReader reader = new BinaryReader(File.OpenRead($"{pointerPath}{key}.dat"));
            reader.BaseStream.Seek(1,SeekOrigin.Begin);
            string file = Encoding.ASCII.GetString(reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length))) ;
            string[] array = file.Split(',');
            


            return array;
        }

        public void GenerateIndexFromFile(string master, long blocksize)
        {
            BinaryReader reader = new BinaryReader(File.Open(master,FileMode.Open),Encoding.ASCII);
            long fileLength = reader.BaseStream.Length / blocksize;
            Transaction util = new Transaction();
            Dictionary<string, StringBuilder> index = new Dictionary<string, StringBuilder>();
            for (int i = 3000;i < 5000;i++)
            {
                index.Add(i.ToString(),new StringBuilder());
            }
            
            for (long i = 1;i < fileLength; i++)
            {
                Console.WriteLine($"{i}/{fileLength}");
                reader.BaseStream.Seek((i * blocksize) + 85,SeekOrigin.Begin);
                string blockData = Encoding.ASCII.GetString(reader.ReadBytes(37657));
                HashSet<string> users = util.GetUsersForPointerIndex(blockData);
                foreach (string user in users)
                {
                    index[user].Append($",{i}");
                }
            }
            foreach (KeyValuePair<string,StringBuilder> kvp in index)
            {
                BinaryWriter writer = new BinaryWriter(File.Create($"{pointerPath}{kvp.Key}.dat"),Encoding.ASCII);
                writer.Write(Encoding.ASCII.GetBytes(kvp.Value.ToString()));
                writer.Close();
            }
            reader.Close();
        }
    }
}

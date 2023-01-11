using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
    }
}

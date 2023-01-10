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

        public List<int> ReadIndex(string key) 
        {
            BinaryReader reader = new BinaryReader(File.OpenRead($"{pointerPath}{key}.dat"));
            StringBuilder builder = new StringBuilder();
            while (reader.PeekChar() > -1)
            {
                builder.Append(reader.ReadChar());
            }
            string[] array = builder.ToString().Split(',');
            List<int> toReturn = new List<int>();
            foreach (string location in array)
            {
                if (location != "")
                {
                    toReturn.Add(int.Parse(location));
                }
            }


            return toReturn;
        }
    }
}

using CSharpChainModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
namespace CSharpChainNetwork.PointerIndex
{
    public class PointerForIndex
    {

		string pointerPath = "C:/temp/PointerSystem/";
		public void CreateLastSeen(long blockNum, HashSet<string> users)
		{
			string pathV1 = $"{pointerPath}/LastSeenV1.txt";
			string pathV2 = $"{pointerPath}/LastSeenV2.txt";
			Stream streamV2;
			Stream streamV1;
			bool fileWasEmpty = false;
			HashSet<string> appearedUsers = new HashSet<string>();

			if (!File.Exists(pathV1) && !File.Exists(pathV2))
			{
				streamV1 = new FileStream(pathV1, FileMode.Create);
				StreamWriter writer = new StreamWriter(streamV1);
				foreach (string user in users)
				{
					writer.WriteLine($"{user}-{blockNum}");
				}
				fileWasEmpty = true;
				writer.Close();
			}

			if (!File.Exists(pathV1) && !fileWasEmpty)
			{
				streamV1 = new FileStream(pathV1, FileMode.Create);
				streamV2 = new FileStream(pathV2, FileMode.Open, FileAccess.Read);
			}
			else
			{
				streamV2 = new FileStream(pathV2, FileMode.Create);
				streamV1 = new FileStream(pathV1, FileMode.Open, FileAccess.Read);
			}


			if (streamV1.CanRead && !streamV1.CanWrite)
			{
				StreamReader reader = new StreamReader(streamV1);
				StreamWriter writer = new StreamWriter(streamV2);
				while (reader.Peek() > -1)
				{
					string line = reader.ReadLine();
					string wallet = line.Substring(0, line.IndexOf('-'));
					if (users.Contains(wallet) && !appearedUsers.Contains(wallet))
					{
						writer.WriteLine($"{wallet}-{blockNum}");
						appearedUsers.Add(wallet);
					}
					else
					{
						writer.WriteLine(line);
					}
				}
				reader.Close();
				string[] temp = users.Except(appearedUsers).ToArray();

				if (temp.Length > 0)
				{
					foreach (string user in temp)
					{
						writer.WriteLine($"{user}-{blockNum}");
					}
				}
				writer.Close();
				File.Delete(pathV1);
			}
			else
			{
				StreamReader reader = new StreamReader(streamV2);
				StreamWriter writer = new StreamWriter(streamV1);

				while (reader.Peek() > -1)
				{
					string line = reader.ReadLine();
					string wallet = line.Substring(0, line.IndexOf('-'));
					if (users.Contains(wallet) && !appearedUsers.Contains(wallet))
					{
						writer.WriteLine($"{wallet}-{blockNum}");
						appearedUsers.Add(wallet);
					}
					else
					{
						writer.WriteLine(line);
					}
				}
				reader.Close();
				string[] temp = users.Except(appearedUsers).ToArray();

				if (temp.Length > 0)
				{
					foreach (string user in temp)
					{
						writer.WriteLine($"{user}-{blockNum}");
					}
				}

				writer.Close();
				File.Delete(pathV2);

			}
		}

		public string GetLocationFromLastSeen(string wallet)
        {
			Stream stream;
			string pathV1 = $"{pointerPath}/LastSeenV1.txt";
			string pathV2 = $"{pointerPath}/LastSeenV2.txt";
            if (File.Exists(pathV1))
            {
				stream = File.Open(pathV1,FileMode.Open);
            }else if (File.Exists(pathV2))
            {
				stream = File.Open(pathV2,FileMode.Open);
            }else
            {
				return "";
            }
			StreamReader reader = new StreamReader(stream, Encoding.ASCII);
			while(reader.Peek() > -1)
            {
				string line = reader.ReadLine();
                if (line.Contains(wallet))
                {
					reader.Close();
					stream.Close();
					return line.Substring(line.IndexOf('-')+1);
                }
            }

			reader.Close();
			stream.Close();

			return "";
        }

		public void CreatePointerIndexFilesForNewSystem(HashSet<string> users, long blockNum)
        {
			string path = $"{pointerPath}/IndexFiles/{blockNum}.txt";
			Stream stream = File.Create(path);
			StreamWriter writer = new StreamWriter(stream,Encoding.ASCII);
            foreach (string user in users)
            {
				writer.Write($"{user}-_");
            }
			writer.Close();
			stream.Close();

		}

		public Dictionary<string,int> CreateDictionaryForUpdating(HashSet<string> users)
        {
			Dictionary<string, int> foundLocations = new Dictionary<string, int>();
            foreach (string user in users)
            {
				string stringLoc = GetLocationFromLastSeen(user);
				if(stringLoc.All(char.IsDigit) && stringLoc != "")
                {
					int location = int.Parse(stringLoc);
					foundLocations.Add(user,location);

                }
			}
			return foundLocations;
        }

		public void GoToTextFilesByDictionary(Dictionary<string,int> foundLocations, long blockNum)
        {
			StreamWriter writer;
			string pathStub = $"{pointerPath}/IndexFiles/";
            foreach (KeyValuePair<string,int> kvp in foundLocations)
            {
				string path = $"{pathStub}{kvp.Value}.txt";
				string temp = File.ReadAllText(path);
				int pos = temp.IndexOf(kvp.Key);
				pos += kvp.Key.Length + 1;
				StringBuilder builder = new StringBuilder(temp);
				builder.Insert(pos,blockNum);
				writer = new StreamWriter(File.Create(path),Encoding.ASCII);
				writer.Write(builder.ToString());
				writer.Close();
            }

			
		}
	}
}

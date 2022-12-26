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
			
			string pathStub = $"{pointerPath}/IndexFiles/";
			HashSet<int> locations = new HashSet<int>();
			List<string> wallets = new List<string>();
			List<string> appeared = new List<string>();
            foreach (KeyValuePair<string,int> kvp in foundLocations)
            {
				locations.Add(kvp.Value);
				wallets.Add(kvp.Key);
            }



			Console.WriteLine("Count:"+locations.Count);
			Console.WriteLine("Wallets:"+wallets.Count);
            foreach (int loc in locations)
            {
				string path = $"{pathStub}{loc}.txt";
				string temp = File.ReadAllText(path);
				StringBuilder builder = new StringBuilder(temp);
				int pos = 0;
                foreach (string wallet in wallets)
                {
					pos = builder.ToString().IndexOf(wallet);
                    if (builder.Length > pos+wallet.Length+1)
                    {
						if (builder.ToString()[pos + wallet.Length] == '-' && pos != -1)
						{
							if (builder.ToString()[pos + wallet.Length + 1] == '_')
							{
								pos += wallet.Length + 1;
								builder.Insert(pos, blockNum);
								appeared.Add(wallet);
							}

						}
					}
                    
                    
				}
				wallets = wallets.Except(appeared).ToList();
				File.WriteAllText(path,builder.ToString());
				
            }	
		}

		public List<int> SearchByPointer(string wallet,long blockNum)
		{
			bool breaker = true;
			string pathStub = $"{pointerPath}/IndexFiles/";
			int loc = 0;
			List<int> locations = new List<int>();
            for (int i = 1; i < blockNum;i++)
            {
				string temp = File.ReadAllText($"{pathStub}{i}.txt");
                if (temp.Contains(wallet))
                {
					loc = i;
					break;
                }
            }
            if (loc > 0)
            {
				locations.Add(loc);
			}

            if (locations.Count == 0)
            {
				return locations;
            }
            while (breaker)
            {
				string blockData = File.ReadAllText($"{pathStub}{loc}.txt");
				int location = blockData.IndexOf(wallet);
                
				string fraction = blockData.Substring(blockData.IndexOf(wallet));
				fraction = fraction.Substring(0, fraction.IndexOf('_'));
				string nextBlock = fraction.Substring(fraction.IndexOf('-') + 1);
				if (nextBlock.All(char.IsDigit) && nextBlock != "")
				{
					loc = int.Parse(nextBlock);
					locations.Add(loc);
				}
				else
				{
					breaker = false;
				}
				
				
				
            }

			return locations;
        }
	}
}

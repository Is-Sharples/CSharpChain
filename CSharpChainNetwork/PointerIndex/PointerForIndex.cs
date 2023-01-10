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

		public void CreateFirstSeen(long blockNum, HashSet<string> users)
        {
			string path = $"{pointerPath}/FirstSeen.txt";
			string file = "";
			if (File.Exists(path))
			{
				file = File.ReadAllText(path);
			}
			Stream stream = File.Open(path,FileMode.Append);
			
			StreamWriter writer = new StreamWriter(stream,Encoding.ASCII);
            foreach (string user in users)
            {
				if (!file.Contains(user)) {
					writer.WriteLine($"{user}-{blockNum}");
				}
			}

			writer.Close();
			stream.Close();
	
		}

		public string GetLocationFromLastSeen(string wallet)
        {
			Stream stream;
			string pathV1 = $"{pointerPath}/LastSeenV1.txt";
			string pathV2 = $"{pointerPath}/LastSeenV2.txt";

			wallet = $"{wallet}-";
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
			string path = $"{pointerPath}/IndexFiles/{blockNum}.dat";
			List<string> userString = users.ToList<string>();
			userString.Sort();
			Stream stream = File.Create(path);
			BinaryWriter writer = new BinaryWriter(stream,Encoding.ASCII);
            foreach (string user in userString)
            {
				writer.Write(Encoding.ASCII.GetBytes($"_{user}-%"));
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
				wallets.Add($"_{kvp.Key}-%");
            }

            foreach (int loc in locations)
            {
				string path = $"{pathStub}{loc}.dat";
				Stream stream = File.Open(path,FileMode.OpenOrCreate);
				BinaryReader reader = new BinaryReader(stream,Encoding.ASCII);
				StringBuilder builder = new StringBuilder();
				FileInfo fi = new FileInfo(path);
				string temp = fi.Length.ToString();
				builder.Append(Encoding.ASCII.GetString(reader.ReadBytes(int.Parse(temp))));
				reader.Close();
				stream.Close();
				int pos = 0;
                foreach (string wallet in wallets)
                {
					pos = builder.ToString().IndexOf(wallet);
					
                    if (builder.Length >= pos+wallet.Length)
                    {
						if (pos != -1)
						{
							char temper = builder.ToString()[pos + wallet.Length-1];
							string testTemp = builder.ToString().Substring(pos);
							if (temper == '%')
							{
								pos += wallet.Length-1;
								builder.Insert(pos, blockNum);
								appeared.Add(wallet);
							}
                        }
					}else
                    {
						Console.WriteLine("ERROR!!");
                    } 
				}
				wallets = wallets.Except(appeared).ToList();
				stream = File.OpenWrite(path);
				BinaryWriter writer = new BinaryWriter(stream);
				writer.Write(Encoding.ASCII.GetBytes(builder.ToString()));
				
				writer.Close();
				stream.Close();
            }	
		}

		public List<int> SearchByPointer(string wallet)
		{
			int loc = 0;
			int walletValue = int.Parse(wallet);
			bool breaker = true;
			string pathStub = $"{pointerPath}/IndexFiles/";
			StreamReader firstReader = new StreamReader($"{pointerPath}/FirstSeen.txt");
			long firstMid = firstReader.BaseStream.Length / 2;
			firstReader.BaseStream.Seek(firstMid,SeekOrigin.Begin);
			string firstTemp = firstReader.ReadLine();
			firstTemp = firstReader.ReadLine();
			int firstTempValue = int.Parse(firstTemp.Substring(0, 4));
            if (firstTempValue < walletValue)
            {
				bool breaking = true;
                while (breaking)
                {
					firstTemp = firstReader.ReadLine();
                    if (firstTemp.Contains(wallet))
                    {
						breaking = false;
                    }
                }
            }else if (firstTempValue > walletValue)
            {
				firstReader.BaseStream.Seek(0,SeekOrigin.Begin);
				bool breaking = true;
				while (breaking)
				{
					firstTemp = firstReader.ReadLine();
					if (firstTemp.Contains(wallet))
					{
						breaking = false;
					}
				}
			}
			else
            {
				loc = int.Parse(firstTemp.Substring(firstTemp.IndexOf('-')+1));
            }
			wallet = $"_{wallet}";
			List<int> locations = new List<int>();
            if (loc == 0)
            {
				loc = int.Parse(firstTemp.Substring(firstTemp.IndexOf('-')+1));
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
				string path = $"{pathStub}{loc}.dat";
				Stream stream = File.OpenRead(path);
				BinaryReader reader = new BinaryReader(stream,Encoding.ASCII);
				long mid = reader.BaseStream.Length / 2;
				FileInfo fi = new FileInfo(path);
				reader.BaseStream.Seek(mid,SeekOrigin.Begin);
				char temper = reader.ReadChar();
				bool found = false;
                while (temper != '_')
                {
					temper = reader.ReadChar();
					mid++;
                }
				string testWallet = Encoding.ASCII.GetString(reader.ReadBytes(4));
				string blockData = "";
				if (walletValue > int.Parse(testWallet))
                {
					blockData = Encoding.ASCII.GetString(reader.ReadBytes(int.Parse((fi.Length-mid).ToString())));
                }else if (walletValue < int.Parse(testWallet))
                {
					reader.BaseStream.Seek(0,SeekOrigin.Begin);
					blockData = Encoding.ASCII.GetString(reader.ReadBytes(int.Parse(mid.ToString())));
				}else
                {
					List<char> vs = new List<char>();
					temper = reader.ReadChar();
					
					while (temper != '%')
                    {
                        if (temper != '-')
                        {
							vs.Add(temper);
						}
						
						temper = reader.ReadChar();
                    }
                    if (vs.Count > 0)
                    {
						if (vs.All(char.IsDigit))
						{
							loc = int.Parse(string.Join("", vs));
							locations.Add(loc);
							found = true;
						}
					}else
                    {
						found = true;
						breaker = false;
                    }
					
                }
                if (!found)
                {
					int location = blockData.IndexOf(wallet);
					string fraction = blockData.Substring(blockData.IndexOf(wallet));
					fraction = fraction.Substring(1, fraction.Substring(1).IndexOf('%'));
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
            }

			return locations;
        }

		public void SortFirstSeen()
        {

			string[] allLines = File.ReadAllLines($"{pointerPath}/FirstSeen.txt");
			List<int> tempLines = new List<int>();
            foreach (string line in allLines)
            {
				tempLines.Add(int.Parse(line.Substring(0, 4)));
            }
			tempLines.Sort();
			List<string> finalLines = new List<string>();
            foreach (int num in tempLines)
            {
                foreach (string line in allLines)
                {
					if(num.ToString() == line.Substring(0, 4))
                    {
						finalLines.Add(line);
                    }
                }
            }
			File.WriteAllLines($"{pointerPath}/FirstSeen.txt",finalLines.ToArray());
        }

		public List<int> ReadFromIndexFile(string key)
        {
			bool breakLoop = false;
			List<int> toReturn = new List<int>();
			int walletValue = int.Parse(key);
			string pathStub = $"{pointerPath}/IndexFiles/";
			string[] firstSeen = File.ReadAllLines($"{pointerPath}/FirstSeen.txt");
			string firstLocation = firstSeen[int.Parse(key)-3000];
			firstLocation = firstLocation.Substring(firstLocation.IndexOf('-')+1);
			int loc = int.Parse(firstLocation);
			toReturn.Add(loc);
			while (!breakLoop) {
				BinaryReader reader = new BinaryReader(File.OpenRead($"{pathStub}{loc}.dat"));
				reader.BaseStream.Seek(reader.BaseStream.Length / 2, SeekOrigin.Begin);
				char peek = reader.ReadChar();
				while (peek != '_')
				{
					peek = reader.ReadChar();
				}
				int foundWallet = int.Parse(getString(reader.ReadBytes(4)));
				bool found = false;
				if (walletValue == foundWallet)
				{
					List<char> toParse = new List<char>();
					peek = reader.ReadChar();
					while (peek != '%')
					{
						peek = reader.ReadChar();
						if (peek != '%')
						{
							toParse.Add(peek);
						}
					}
					if (toParse.Count > 0)
					{
						loc = int.Parse(string.Join("", toParse));
						toReturn.Add(loc);
						found = true;
					}
					else
					{
						breakLoop = true;
					}

				}
				else if (walletValue < foundWallet)
				{

					reader.BaseStream.Seek(0, SeekOrigin.Begin);
				}

				while (!found)
				{
					peek = reader.ReadChar();
					while (peek != '_')
					{
						peek = reader.ReadChar();
					}
					foundWallet = int.Parse(getString(reader.ReadBytes(4)));
					if (walletValue == foundWallet)
					{
						List<char> toParse = new List<char>();
						peek = reader.ReadChar();
						while (peek != '%')
						{
							peek = reader.ReadChar();
							if (peek != '%')
							{
								toParse.Add(peek);
							}

						}
						if (toParse.All(char.IsDigit) && toParse.Count > 0)
						{
							loc = int.Parse(string.Join("", toParse));
							toReturn.Add(loc);
						}
						else
						{
							breakLoop = true;
						}
						found = true;
					}
				}
				reader.Close();
			}
			
			return toReturn;
		}

		public string getString(byte [] input)
        {
			return Encoding.ASCII.GetString(input);
        }
	}
}
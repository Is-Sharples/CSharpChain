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

		public void GenerateIndexFromFile(string master, long blockSize) 
		{
			
			Transaction util = new Transaction();
			BinaryReader reader = new BinaryReader(File.OpenRead(master),Encoding.ASCII);
			long fileLength = reader.BaseStream.Length;
			
			Dictionary<long, HashSet<string>> tempIndex = new Dictionary<long, HashSet<string>>();
			//fileLength/blockSize
			for (long i = 1; i < fileLength/blockSize;i++)
            {
				Console.WriteLine($"{i}/{fileLength / blockSize}");
				reader.BaseStream.Seek((i * blockSize) + 85,SeekOrigin.Begin);
				string blockData = Encoding.ASCII.GetString(reader.ReadBytes(12044));
				tempIndex.Add(i, util.GetUsersForPointerIndex(blockData));
                if (i == 25000)
                {
					InternalWriteToDisk(tempIndex);
					
                }
			}
		}

		private void InternalWriteToDisk(Dictionary<long, HashSet<string>> tempIndex)
        {
			Dictionary<string, int> foundUserLocs;
			Dictionary<long, StringBuilder> index = new Dictionary<long, StringBuilder>();
			foreach (KeyValuePair<long, HashSet<string>> kvp in tempIndex)
			{
				Console.WriteLine(kvp.Key);
				StringBuilder builder = new StringBuilder();
				foreach (string user in kvp.Value)
				{
					builder.Append($"_{user}-%");
				}
				index.Add(kvp.Key, builder);
				foundUserLocs = CreateDictionaryForUpdating(kvp.Value);
				foreach (KeyValuePair<string, int> kvp2 in foundUserLocs)
				{
					string[] arr = index[kvp2.Value].ToString().Split('%');
                    for (int i = 0; i < arr.Length;i++)
                    {
                        if (arr[i].Contains($"_{kvp2.Key}-"))
                        {
							arr[i] = $"{arr[i]}{kvp2.Value}";
						}	
                    }
					index[kvp2.Value] = new StringBuilder(string.Join("%",arr));
				}
				CreateLastSeen(kvp.Key, kvp.Value);
				if (kvp.Key < 1500)
				{
					CreateFirstSeen(kvp.Key, kvp.Value);
				}
			}
		}

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

		public Dictionary<string,string> GetLocationFromLastSeen(HashSet<string> wallets)
        {
			//Stream stream;
			string pathV1 = $"{pointerPath}/LastSeenV1.txt";
			string pathV2 = $"{pointerPath}/LastSeenV2.txt";
			string[] temp;
			Dictionary<string, string> locations = new Dictionary<string, string>();
			//wallet = $"{wallet}-";
            if (File.Exists(pathV1))
            {
				temp = File.ReadAllLines(pathV1);
				//stream = File.Open(pathV1,FileMode.Open);

            }else if (File.Exists(pathV2))
            {
				temp = File.ReadAllLines(pathV2);
				//stream = File.Open(pathV2,FileMode.Open);
            }else
            {
				return null;
            }
            foreach (string line in temp)
            {
                foreach (string wallet in wallets)
                {
					if (line.Contains($"{wallet}-"))
					{
						locations.Add(wallet,line.Substring(line.IndexOf('-')+1));
					}
				}
            }

			return locations;
        }

		public void CreatePointerIndexFilesForNewSystem(HashSet<string> users, long blockNum)
        {
			string path = $"{pointerPath}/PointerIndexFiles/{blockNum}.dat";
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
			Dictionary<string, string> locations = GetLocationFromLastSeen(users);

            if (locations != null)
            {
				foreach (KeyValuePair<string, string> kvp in locations)
				{
					int location = int.Parse(kvp.Value);
					foundLocations.Add(kvp.Key, location);
				}
			}
			
			return foundLocations;
        }

		public void GoToTextFilesByDictionary(Dictionary<string,int> foundLocations, long blockNum)
        {
			string pathStub = $"{pointerPath}/PointerIndexFiles/";
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

		public string [] SearchByPointer(string wallet)
		{
			string loc = "";
			int walletValue = int.Parse(wallet);
			bool breaker = true;
			string pathStub = $"{pointerPath}/PointerIndexFiles/";
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
				loc = firstTemp.Substring(firstTemp.IndexOf('-')+1);
            }
			wallet = $"_{wallet}";
			List<string> locations = new List<string>();
            if (loc == "")
            {
				loc = firstTemp.Substring(firstTemp.IndexOf('-')+1);
            }
            
			locations.Add(loc);
			

            if (locations.Count == 0)
            {
				return locations.ToArray();
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
							loc = string.Join("", vs);
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
						loc = nextBlock;
						Console.WriteLine(loc);
						locations.Add(loc);
					}
					else
					{
						breaker = false;
					}
				}
            }

			return locations.ToArray();
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

		public string[] ReadFromIndexFile(string key)
        {
			bool breakLoop = false;
			List<string> toReturn = new List<string>();
			int walletValue = int.Parse(key);
			string pathStub = $"{pointerPath}/PointerIndexFiles/";
			string[] firstSeen = File.ReadAllLines($"{pointerPath}/FirstSeen.txt");
			string firstLocation = firstSeen[int.Parse(key)-3000];
			firstLocation = firstLocation.Substring(firstLocation.IndexOf('-')+1);
			string loc = firstLocation;
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
						loc = string.Join("", toParse);
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
							loc = string.Join("", toParse);
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
			
			return toReturn.ToArray();
		}

		public string getString(byte [] input)
        {
			return Encoding.ASCII.GetString(input);
        }


	}
}
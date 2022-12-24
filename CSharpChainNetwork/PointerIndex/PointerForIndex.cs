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

		public void AppendPointerIndexBlockList(long blockNum,int location)
		{
			string pathV1 = $"{pointerPath}/BlockList1.txt";
			int pointer = 0;
			int temp = 0;
			if (File.Exists(pathV1))
            {
				Stream readStream = new FileStream(pathV1, FileMode.Open);
				StreamReader reader = new StreamReader(readStream, Encoding.ASCII);
				
				while (reader.Peek() > -1)
				{
					string line = reader.ReadLine();
					string tempPointer = line.Substring(line.IndexOf('-') + 1);
					tempPointer = tempPointer.Substring(0,tempPointer.IndexOf('+'));
					string tempLoc = line.Substring(line.IndexOf('+')+1);

					if (tempPointer.All(char.IsDigit) && tempLoc.All(char.IsDigit))
					{
						temp = int.Parse(tempLoc);
						pointer = temp;
					}
				}
				reader.Close();
				readStream.Close();
			}
			
			Stream streamV1 = new FileStream(pathV1, FileMode.Append);
			StreamWriter writer = new StreamWriter(streamV1);
			writer.WriteLine($"{blockNum}-{pointer}+{location}");
			writer.Close();
			streamV1.Close();
		}

		public void OverWritePointerIndexBlockList(Dictionary<long,long> positions)
        {
			string pathV1 = $"{pointerPath}/IndexFile1.dat";
			string pathV2 = $"{pointerPath}/IndexFile2.dat";
			Stream readStream;
			BinaryReader reader;
			string path = $"{pointerPath}/BlockList1.txt";
			Stream stream = File.Create(path);
			StreamWriter writer = new StreamWriter(stream);
			long prevPosition = 0;

            if (File.Exists(pathV1))
            {
				readStream = File.OpenRead(pathV1);
            }else
            {
				readStream = File.OpenRead(pathV2);
            }
			reader = new BinaryReader(readStream);
			int count = 1;
            while (reader.PeekChar() != -1)
            {
				byte temp = reader.ReadByte();
                if (Convert.ToChar(temp) == '%')
                {
					writer.WriteLine($"{count}-{prevPosition}+{reader.BaseStream.Position}");
					prevPosition = reader.BaseStream.Position;
					count++;
                }
            }
			reader.Close();
			readStream.Close();
			writer.Close();
			stream.Close();
		}

		public Dictionary<string,string> CreatePointerIndexFile(long blockNum, HashSet<string> users)
		{
			string pathV1 = $"{pointerPath}/IndexFile1.dat";
			string pathV2 = $"{pointerPath}/IndexFile2.dat";
			bool fileWasEmpty = false;
			Stream stream;
			byte [] writable = Encoding.ASCII.GetBytes($"Start_Block_{blockNum}_");
			int length = 0;


			if (!File.Exists(pathV1) && !File.Exists(pathV2))
			{
				stream = new FileStream(pathV1, FileMode.Create);
				BinaryWriter writer = new BinaryWriter(stream);
				
				writer.Write(writable);
				length += writable.Length;
				foreach (string user in users)
				{
					writable = Encoding.ASCII.GetBytes($"{user}-_"); 
					length += writable.Length;
					writer.Write(writable);
				}
				writable = Encoding.ASCII.GetBytes("End_Block%");
				writer.Write(writable);
				length += writable.Length;
				writer.Close();
				stream.Close();
				fileWasEmpty = true;
				
			}

            if (fileWasEmpty)
            {
				CreateLastSeen(blockNum, users);
				AppendPointerIndexBlockList(blockNum, length);
				return null;
			}

			if (File.Exists(pathV1))
			{
				stream = new FileStream(pathV1, FileMode.Append);
			}
			else
			{
				stream = new FileStream(pathV2, FileMode.Append);
			}

			BinaryWriter appender = new BinaryWriter(stream,Encoding.ASCII);
            appender.Write(writable);
			length += writable.Length;
			Dictionary<string, string> foundLocations = new Dictionary<string, string>();
			foreach (string user in users)
            {
				writable = Encoding.ASCII.GetBytes($"{user}-_");
				appender.Write(writable);
				length += writable.Length;
				string found = GetLocationFromLastSeen(user);
				if (found != "" && found.All(char.IsDigit))
                {
					int location = int.Parse(found);
					KeyValuePair<int,int> startAndLength = GoToBlockByPointer(location);
					string temp = $"{startAndLength.Key}-{startAndLength.Value}";

					foundLocations.Add(user,temp);
				}
			}
			CreateLastSeen(blockNum,users);
			writable = Encoding.ASCII.GetBytes("End_Block%");
			length += writable.Length;
			appender.Write(writable);
			AppendPointerIndexBlockList(blockNum,length);
			appender.Close();
			stream.Close();


			return foundLocations;

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

		public KeyValuePair<int,int> GoToBlockByPointer(int location)
        {
			Stream stream = File.OpenRead($"{pointerPath}/BlockList1.txt");
			StreamReader reader = new StreamReader(stream,Encoding.ASCII);
			string temp = "";
			for (int i = 0; i < location;i++)
            {
				temp = reader.ReadLine();
            }
            
			temp = temp.Substring(temp.IndexOf("-")+1);
			string largePointer = temp.Substring(temp.IndexOf('+')+1);
			string smallPointer = temp.Substring(0,temp.IndexOf("+"));
			stream.Close();
			reader.Close();
			int min = 0;
			int max = 0;
            if (largePointer.All(char.IsDigit) && smallPointer.Trim() != "")
            {
				max = int.Parse(largePointer);
            }

            if (smallPointer.All(char.IsDigit) && smallPointer.Trim() != "")
            {
				min = int.Parse(smallPointer);
			}
			KeyValuePair<int,int> startAndLength = new KeyValuePair<int, int>(min,max-min);
			return startAndLength;
        }

		public HashSet<long> PreparePositionsForEditBlockIndexFile(Dictionary<string,string> foundLocations,long blockNum)
        {
			string pathV1 = $"{pointerPath}/IndexFile1.dat";
			string pathV2 = $"{pointerPath}/IndexFile2.dat";

			Stream stream;

            if (File.Exists(pathV1))
            {
				stream = File.OpenRead(pathV1);
            }else
            {
				stream = File.OpenRead(pathV2);
            }
			HashSet<long> positionsEdit = new HashSet<long>();
			BinaryReader reader = new BinaryReader(stream, Encoding.ASCII);
            foreach (KeyValuePair<string, string> keyValue in foundLocations)
            {
					
				string position = keyValue.Value.Substring(0,keyValue.Value.IndexOf('-'));
				string length = keyValue.Value.Substring(keyValue.Value.IndexOf('-')+1);
				byte[] stringDataInByte;
                if (position.All(char.IsDigit) && length.All(char.IsDigit))
                {
					long positionValue = long.Parse(position);
					int lengthValue = int.Parse(length);

					stream.Seek(positionValue, SeekOrigin.Begin);
					stringDataInByte = reader.ReadBytes(lengthValue);
					string blockData = Encoding.ASCII.GetString(stringDataInByte);
					int num = blockData.IndexOf($"{keyValue.Key}");
                    if (num > 0)
                    {
						positionValue += num + 4;
						stream.Seek(positionValue, SeekOrigin.Begin);
						positionsEdit.Add(positionValue);
					}
					
                }
            }

			stream.Close();
			reader.Close();

			return positionsEdit;
        }

		public Dictionary<long,long> EditBlockIndexFile(HashSet<long> positionsToEdit, long blockNum)
        {
			string pathV1 = $"{pointerPath}/IndexFile1.dat";
			string pathV2 = $"{pointerPath}/IndexFile2.dat";
			Stream streamV1;
			Stream streamV2;
			BinaryReader reader;
			BinaryWriter writer;
			Dictionary<long, long> positions = new Dictionary<long, long>();

            if (File.Exists(pathV1))
            {
				streamV1 = File.OpenRead(pathV1);
				reader = new BinaryReader(streamV1);
				streamV2 = File.Create(pathV2);
				writer = new BinaryWriter(streamV2);
            }else
            {
				streamV1 = File.Create(pathV1);
				writer = new BinaryWriter(streamV1);
				streamV2 = File.OpenRead(pathV2);
				reader = new BinaryReader(streamV2);
            }
            while (reader.PeekChar() != -1)
            {
				byte temp = reader.ReadByte();
                if (positionsToEdit.Contains(reader.BaseStream.Position))
                {
					writer.Write(temp);
					temp = reader.ReadByte();
					writer.Write(temp);
					writer.Write(Encoding.ASCII.GetBytes(blockNum.ToString()));
                }
				else
                {
					writer.Write(temp);
                }
            }

			bool delete = !streamV1.CanWrite;

			reader.BaseStream.Close();
			reader.Close();
			writer.BaseStream.Close();
			writer.Close();
            if (delete)
            {
				File.Delete(pathV1);
            }else
            {
				File.Delete(pathV2);
            }

			return positions;
        }
	}
}

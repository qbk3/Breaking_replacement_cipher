using System;
using System.Linq;
using System.Text;
using System.IO;

namespace Breaking_replacement_cipher
{
	abstract public class N_Gram
	{
		public string Type; // Тип n-грам
		public string Alphabet = ""; // Символы алфавита
		public long[] Data; // Частотная статистика триграмм
		public int Count; // Количество символов в алфавите
		public long Length; // Размер репрезентативной выборки
	}
	public class Unogram : N_Gram// Класс частотной статистики символов
	{
		public Unogram(ref string fileName)
		{
			Type = "unogram";
			StreamReader file = new StreamReader(fileName, encoding: Encoding.Default);
			string text = file.ReadToEnd();
			file.Close();
			
			for (var i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				if (Alphabet.Contains(ch) == false)
					Alphabet += ch;
			}
			Count = Alphabet.Length;
			Data = new long[Count];
			
			for (var i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				Length++;
				Data[ch]++;
			}
		}
		public Unogram(Bigram b)
		{
			Type = "unogram";
			Alphabet = b.Alphabet;
			Length = b.Length;
			Count = b.Count;
			Data = new long[Count];
			
			for (var i = 0; i < Count; i++)
			{
				Data[i] = b.Data[i];
				for (var j = 1; j < Count; j++)
				{
					Data[i] += b.Data[i + j * Count];
				}
			}
		}
	}
	public class Bigram : N_Gram// Класс частотной статистики биграмм
	{
		public Bigram(ref string fileName)
		{
			Type = "bigram";
			StreamReader file = new StreamReader(fileName, encoding: Encoding.Default);
			string text = file.ReadToEnd();
			
			file.Close();
			for (var i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				if (Alphabet.Contains(ch) == false)
					Alphabet += ch;
			}
			Count = Alphabet.Length;
			Data = new long[Count * Count];
			
			var index0 = 0;
			for (var i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				var index1 = Alphabet.IndexOf(ch);
				if (++Length >= 2)
				{
					Data[index0 + index1 * Count]++;
					index0 = index1;
				}
			}
		}
		public Bigram(Trigram t)
		{
			Type = "bigram";
			Alphabet = t.Alphabet;
			Length = t.Length;
			Count = t.Count;
			Data = new long[Count * Count];
			
			for (var i = 0; i < Count * Count; i++)
			{
				Data[i] = t.Data[i];
				for (var j = 1; j < Count; j++)
				{
					Data[i] += t.Data[i + j * Count * Count];
				}
			}
	
		}
	}
	public class Trigram : N_Gram // Класс частотной статистики триграмм
	{
		public Trigram(ref string fileName)
		{
			Type = "trigram";
			StreamReader file = new StreamReader(fileName, encoding: Encoding.Default);
			string text = file.ReadToEnd();
			file.Close();
			
			for (var i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				if (Alphabet.Contains(ch) == false)
					Alphabet += ch;
			}
			Count = Alphabet.Length;
			Data = new long[Count * Count * Count];
			
			var index0 = 0;
			var index1 = 0;
			for (var i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				var index2 = Alphabet.IndexOf(ch);
				if (++Length >= 3)
				{
					Data[index0 + index1 * Count + index2 * Count * Count]++;
					index0 = index1;
					index1 = index2;
				}
			}
		}
	}
	class Program
    {
        static void Main(string[] args)
        {
			string cipherFileName = @"../../texts/cipher.txt"; // Имя файла с зашифрованным текстом
			string plainFileName = @"../../texts/plain.txt"; // Имя файла с расшифрованным текстом
			string sampleFileName = @"../../texts/sample1.txt"; // Имя файла с образцом открытого текста

			StreamReader file = new StreamReader(@"../../texts/cipher.txt", encoding: Encoding.Default);
			Console.WriteLine("--------------------------------\n" + file.ReadToEnd() + "\n--------------------------------");
			file.Close();

			// Получение частотных статистик триграмм из файлов
			Trigram cipher = new Trigram(ref cipherFileName);
			Trigram sample = new Trigram(ref sampleFileName);

			// Получение частотных статистик биграмм из частотных статистик триграмм
			Bigram bicipher = new Bigram(cipher);
			Bigram bisample = new Bigram(sample);

			// Получение частотных статистик символов из частотных статистик биграмм
			Unogram unocipher = new Unogram(bicipher);
			Unogram unosample = new Unogram(bisample);

			// Определение максимального количества символов в алфавитах
			var count = Math.Max(cipher.Count, sample.Count);

			// Аллокирование и инициализация подстановок
			int[] p1 = new int[count];
			int[] p2 = new int[count];

			for (var i = 0; i < count; i++) p1[i] = i;
			for (var i = 0; i < count; i++) p2[i] = i;

			BreakingReplacementCipher(ref p1, ref p2, unocipher, unosample);
			BreakingReplacementCipher(ref p1, ref p2, bicipher, bisample);
			BreakingReplacementCipher(ref p1, ref p2, cipher, sample);

			string key="";
			string value="";

			for (var i = 0; i < count; i++) key+=(p1[i] < cipher.Count ? cipher.Alphabet[p1[i]] : ' ');
			for (var i = 0; i < count; i++) value+=(p2[i] < sample.Count ? sample.Alphabet[p2[i]] : ' ');

			Console.WriteLine("Таблица для расшифровки\n\n key = value");

			for (int i = 0; i < key.Length; i++)
			{
				Console.WriteLine("  "+key[i] + "  =   " + value[i]+"  ");
			}

			Console.WriteLine("--------------------------------");
			Console.WriteLine(Replace(ref cipherFileName, ref plainFileName, ref key, ref value));
			Console.WriteLine("--------------------------------");
			Console.WriteLine("Хотите ли вы изменить соответствие букв? (да / нет)");
			var ansver = Console.ReadLine();
			while (ansver == "да")
			{
				Console.WriteLine("Какие 2 буквы вы хотите поменять местами в столбце value? (вводить через пробел)");
				var str = Console.ReadLine().Split(' ');
				var first = value.IndexOf(str[0]);
				var second = value.IndexOf(str[1]);
				value = SwapChars(value, first, second);
				Console.WriteLine("Таблица для расшифровки\n\n key = value");
				for (int i = 0; i < key.Length; i++)
				{
					Console.WriteLine("  " + key[i] + "  =   " + value[i] + "  ");
				}
				Console.WriteLine("--------------------------------");
				Console.WriteLine(Replace(ref cipherFileName, ref plainFileName, ref key, ref value));
				Console.WriteLine("--------------------------------");
				Console.WriteLine("Хотите ли вы изменить соответствие букв? (y / n)");
				ansver = Console.ReadLine();
			}

			Console.WriteLine("\nТакже Текст расшифровки находится в папке texts под именем plain.txt :)\nА для замены открытого текста нужно лезть в код :(\n");

			return;
		}
		static string SwapChars(string str, int index1, int index2)
		{
			char[] strChar = str.ToCharArray();
			char temp = strChar[index1];
			strChar[index1] = strChar[index2];
			strChar[index2] = temp;
			return new string(strChar);
		}

		static double Fitness(ref int[] p1, ref int[] p2,N_Gram t1,N_Gram t2)
		{
			if (t1.Type == "unogram") // Расчёт корреляции по частотной статистике символов
			{
				double s = 0;
				var count = Math.Min(p1.Length, p2.Length);
				for (var i = 0; i < count; i++)
				{
					double x = (p1[i] < t1.Count) ? t1.Data[p1[i]] : 0;
					double y = (p2[i] < t2.Count) ? t2.Data[p2[i]] : 0;
					s += x * y;
				}
				return s;
			}
			else if (t1.Type == "bigram") // Расчёт корреляции по частотной статистике биграмм
			{
				double s = 0;
				var count = Math.Min(p1.Length, p2.Length);
				var count1 = t1.Count;
				var count2 = t2.Count;
				for (var i = 0; i < count; i++)
				{
					for (var j = 0; j < count; j++)
					{
						double x = (p1[i] < count1 && p1[j] < count1) ? t1.Data[p1[i] + p1[j] * count1] : 0;
						double y = (p2[i] < count2 && p2[j] < count2) ? t2.Data[p2[i] + p2[j] * count2] : 0;
						s += x * y;
					}
				}
				return s;
			}
			else // Расчёт корреляции по частотной статистике триграмм
			{
				double s = 0;
				var count = Math.Min(p1.Length, p2.Length);
				var count1 = t1.Count;
				var count2 = t2.Count;
				var count12 = count1 * count1;
				var count22 = count2 * count2;

				for (var i = 0; i < count; i++)
				{
					for (var j = 0; j < count; j++)
					{
						for (var k = 0; k < count; k++)
						{
							double x = (p1[i] < t1.Count && p1[j] < count1 && p1[k] < count1) ? t1.Data[p1[i] + p1[j] * count1 + p1[k] * count12] : 0;
							double y = (p2[i] < t2.Count && p2[j] < count2 && p2[k] < count2) ? t2.Data[p2[i] + p2[j] * count2 + p2[k] * count22] : 0;
							s += x * y;
						}
					}
				}
				return s;
			}
		}
		// Применение алгоритма простой замены для файла
		// Символы из стороки key заменяются на символы из строки value
		static string Replace(ref string sourceFileName,ref string destFileName,ref string key,ref string value)
		{
			StreamReader reader = new StreamReader(sourceFileName, encoding: Encoding.Default);
			StreamWriter writer = new StreamWriter(destFileName);
			var text = reader.ReadToEnd();

			StringBuilder breaktext = new StringBuilder("");

			for (var i = 0; i < text.Length; i++)
			{
				var ch = text[i];
				var index = key.IndexOf(ch);
				writer.Write(index < value.Length ? value[index] : ' ');
				breaktext.Append(index < value.Length ? value[index] : ' ');
			}

			reader.Close();
			writer.Close();

			return breaktext.ToString();
		}
		// Aлгоритм поиска восхождением к вершине:
		// применение парных замен в перестановках для максимизации корелляции по частотной статистике символов
		static void BreakingReplacementCipher(ref int[] p1, ref int[] p2,  N_Gram t1,  N_Gram t2)
		{
			var current = Fitness(ref p1, ref p2, t1, t2);
			
			for (var loop = true; loop;)
			{
				loop = false;
				for (var i = 0; i < p1.Length - 1; i++)
				{
					for (var j = i + 1; j < p1.Length; j++)
					{
						var t = p1[i];
						p1[i] = p1[j];
						p1[j] = t;
						var next = Fitness(ref p1, ref p2, t1, t2);
						if (next > current)
						{
							current = next;
							loop = true;
						}
						else
						{
							t = p1[i];
							p1[i] = p1[j];
							p1[j] = t;
						}
					}
				}
				for (var i = 0; i < p2.Length - 1; i++)
				{
					for (var j = i + 1; j < p2.Length; j++)
					{
						var t = p2[i];
						p2[i] = p2[j];
						p2[j] = t;
						var next = Fitness(ref p1, ref p2, t1, t2);
						if (next > current)
						{
							current = next;
							loop = true;
						}
						else
						{
							t = p2[i];
							p2[i] = p2[j];
							p2[j] = t;
						}
					}
				}
			}
		}
    }
}

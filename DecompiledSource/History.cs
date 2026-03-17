using System.Collections.Generic;
using UnityEngine;

public static class History
{
	private class HistorySlice
	{
		public List<AntCasteHistoryStats> antCasteStats = new List<AntCasteHistoryStats>();

		public void Write(Save save)
		{
			save.Write(antCasteStats.Count);
			foreach (AntCasteHistoryStats antCasteStat in antCasteStats)
			{
				save.Write((ushort)antCasteStat.antCaste);
				save.Write((ushort)antCasteStat.nBorn);
				save.Write((ushort)antCasteStat.nRepurposed);
				save.Write((ushort)antCasteStat.nDied);
			}
		}

		public void Read(Save save)
		{
			int num = save.ReadInt();
			for (int i = 0; i < num; i++)
			{
				AntCasteHistoryStats item = new AntCasteHistoryStats
				{
					antCaste = (AntCaste)save.ReadUShort(),
					nBorn = save.ReadUShort(),
					nRepurposed = save.ReadUShort(),
					nDied = save.ReadUShort()
				};
				antCasteStats.Add(item);
			}
		}

		public void RegisterAntEnd(AntCaste ant_caste, bool repurposed, bool also_birth)
		{
			for (int i = 0; i < antCasteStats.Count; i++)
			{
				AntCasteHistoryStats value = antCasteStats[i];
				if (value.antCaste == ant_caste)
				{
					if (repurposed)
					{
						value.nRepurposed++;
					}
					else
					{
						value.nDied++;
					}
					if (also_birth)
					{
						value.nBorn++;
					}
					antCasteStats[i] = value;
					return;
				}
			}
			AntCasteHistoryStats item = new AntCasteHistoryStats
			{
				antCaste = ant_caste
			};
			if (repurposed)
			{
				item.nRepurposed++;
			}
			else
			{
				item.nDied++;
			}
			if (also_birth)
			{
				item.nBorn++;
			}
			antCasteStats.Add(item);
		}

		public void RegisterAntBirth(AntCaste ant_caste)
		{
			for (int i = 0; i < antCasteStats.Count; i++)
			{
				AntCasteHistoryStats value = antCasteStats[i];
				if (value.antCaste == ant_caste)
				{
					value.nBorn++;
					antCasteStats[i] = value;
					return;
				}
			}
			AntCasteHistoryStats item = new AntCasteHistoryStats
			{
				antCaste = ant_caste
			};
			item.nBorn++;
			antCasteStats.Add(item);
		}

		public int GetPopulationChange(AntCaste ant_caste)
		{
			for (int i = 0; i < antCasteStats.Count; i++)
			{
				AntCasteHistoryStats antCasteHistoryStats = antCasteStats[i];
				if (antCasteHistoryStats.antCaste == ant_caste)
				{
					return antCasteHistoryStats.nBorn - (antCasteHistoryStats.nDied + antCasteHistoryStats.nRepurposed);
				}
			}
			return 0;
		}
	}

	private static List<HistorySlice> history;

	private static HistorySlice lastHistorySlice;

	public static void Init()
	{
		history = new List<HistorySlice>
		{
			new HistorySlice()
		};
		lastHistorySlice = history[^1];
	}

	public static void Write(Save save)
	{
		save.Write(1);
		save.Write(history.Count);
		foreach (HistorySlice item in history)
		{
			item.Write(save);
		}
	}

	public static void Read(Save save)
	{
		if (save.version >= 62)
		{
			save.ReadInt();
			int num = save.ReadInt();
			history.Clear();
			for (int i = 0; i < num; i++)
			{
				HistorySlice historySlice = new HistorySlice();
				historySlice.Read(save);
				history.Add(historySlice);
			}
			lastHistorySlice = history[^1];
		}
	}

	public static int GetCount()
	{
		return history.Count;
	}

	private static int TimeToIndex(float time)
	{
		return Mathf.FloorToInt(time / 60f);
	}

	private static void CheckCurrentTime()
	{
		int num = TimeToIndex((float)GameManager.instance.gameTime);
		for (int i = history.Count; i <= num; i++)
		{
			history.Add(new HistorySlice());
		}
		lastHistorySlice = history[^1];
	}

	public static void RegisterAntEnd(Ant ant, bool repurposed)
	{
		if (ant.birthTime != -1f)
		{
			CheckCurrentTime();
			AntCaste caste = ant.caste;
			int num = TimeToIndex(ant.birthTime);
			if (num == history.Count - 1)
			{
				lastHistorySlice.RegisterAntEnd(caste, repurposed, also_birth: true);
			}
			else
			{
				history[num].RegisterAntBirth(caste);
				lastHistorySlice.RegisterAntEnd(caste, repurposed, also_birth: false);
			}
			ant.birthTime = -1f;
		}
	}

	public static List<AntCasteHistoryStats> GetAntCasteTotals(float from_time, float to_time)
	{
		AntCasteHistoryStats[] array = new AntCasteHistoryStats[43];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new AntCasteHistoryStats
			{
				antCaste = (AntCaste)i
			};
		}
		int num = TimeToIndex(from_time);
		int num2 = TimeToIndex(to_time);
		int count = history.Count;
		for (int j = num; j <= num2 && j < count; j++)
		{
			foreach (AntCasteHistoryStats antCasteStat in history[j].antCasteStats)
			{
				int antCaste = (int)antCasteStat.antCaste;
				AntCasteHistoryStats antCasteHistoryStats = array[antCaste];
				antCasteHistoryStats.nBorn += antCasteStat.nBorn;
				antCasteHistoryStats.nRepurposed += antCasteStat.nRepurposed;
				antCasteHistoryStats.nDied += antCasteStat.nDied;
				array[antCaste] = antCasteHistoryStats;
			}
		}
		foreach (Ant item2 in GameManager.instance.EAnts())
		{
			if (!(item2.birthTime < 0f))
			{
				int num3 = TimeToIndex(item2.birthTime);
				if (num3 >= num && num3 <= num2)
				{
					int caste = (int)item2.caste;
					AntCasteHistoryStats antCasteHistoryStats2 = array[caste];
					antCasteHistoryStats2.nBorn++;
					array[caste] = antCasteHistoryStats2;
				}
			}
		}
		List<AntCasteHistoryStats> list = new List<AntCasteHistoryStats>();
		for (int k = 0; k < array.Length; k++)
		{
			AntCasteHistoryStats item = array[k];
			if (item.nBorn + item.nRepurposed + item.nDied > 0)
			{
				list.Add(item);
			}
		}
		return list;
	}

	public static List<float> GetPopulationHistory(AntCaste ant_caste, int amount)
	{
		List<float> list = new List<float>(amount);
		for (int i = 0; i < amount; i++)
		{
			list.Add(0f);
		}
		int num = TimeToIndex((float)GameManager.instance.gameTime);
		int num2 = 0;
		foreach (Ant item in GameManager.instance.EAnts())
		{
			if (item.caste != ant_caste || item.IsDead())
			{
				continue;
			}
			num2++;
			int num3 = num - TimeToIndex(item.birthTime);
			if (num3 >= 0)
			{
				int num4 = amount - 1 - num3;
				if (num4 >= 0)
				{
					list[num4]++;
				}
			}
		}
		int count = history.Count;
		for (int num5 = amount - 1; num5 >= 0; num5--)
		{
			int num6 = (int)list[num5];
			list[num5] = num2;
			num2 -= num6;
			if (num >= 0)
			{
				if (num < count)
				{
					num2 -= history[num].GetPopulationChange(ant_caste);
				}
				num--;
			}
		}
		return list;
	}

	public static IEnumerable<AntCaste> EAntCastes()
	{
		HashSet<AntCaste> done = new HashSet<AntCaste>();
		foreach (HistorySlice item in history)
		{
			foreach (AntCasteHistoryStats antCasteStat in item.antCasteStats)
			{
				if (!done.Contains(antCasteStat.antCaste))
				{
					done.Add(antCasteStat.antCaste);
					yield return antCasteStat.antCaste;
				}
			}
		}
		foreach (Ant item2 in GameManager.instance.EAnts())
		{
			if (!done.Contains(item2.caste))
			{
				done.Add(item2.caste);
				yield return item2.caste;
			}
		}
	}
}

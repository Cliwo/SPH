using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Text;
public static class FrameDebuggerUtil {

	public static List<string> buffer;
	public static List<string> secondBuffer;
	private static FileStream file;
	private static int FlushThreshold = 1000;
	private static bool bufferDir = false;
	private static string path = "C:\\Users\\Chan\\Documents\\SPH\\Assets\\log_forces.csv";

	public static string EncodeInCSV(params KeyValuePair<string, string>[] csvItems)
	{
		StringBuilder build = new StringBuilder();
		foreach (var item in csvItems)
		{
			build.Append(string.Format("{0},{1},", item.Key, item.Value));
		}
		build.Remove(build.Length-1, 1); //마지막 콤마 삭제
		return build.ToString();
	}
	public static void EnqueueString(string line)
	{
		if(buffer == null)
			buffer = new List<string>();
		if(secondBuffer == null)
			secondBuffer = new List<string>();

		List<string> c_buffer = bufferDir ? secondBuffer : buffer;
		c_buffer.Add(line);
		if(c_buffer.Count > FlushThreshold)
		{
			FlushToFile();
		}
	}
	public static void Free()
	{
		file.Close();
	}
	private static void FlushToFile()
	{
		if(file == null)
		{
			file = File.Open(path,FileMode.Create);
		}

		List<string> c_buffer = bufferDir ? secondBuffer : buffer;
		bufferDir = !bufferDir;
		foreach(string s in c_buffer)
		{
			byte[] line = new UTF8Encoding(true).GetBytes(s + "\n");
			file.Write(line, 0, line.Length);
		}
		c_buffer.Clear();
	}

}

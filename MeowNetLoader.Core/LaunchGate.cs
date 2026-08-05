using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MeowNetLoader.Core;

internal static class LaunchGate
{
	public static void WriteLoaderPath(string settingsDirectory, string exePath)
	{
		try
		{
			Directory.CreateDirectory(settingsDirectory);
			File.WriteAllText(Path.Combine(settingsDirectory, "loader.path"), exePath);
		}
		catch
		{
		}
	}

	public static void WriteLaunchToken(string settingsDirectory)
	{
		try
		{
			Directory.CreateDirectory(settingsDirectory);
			string text = DateTime.UtcNow.Ticks.ToString();
			string text2 = Guid.NewGuid().ToString("N");
			string text3 = Sign(text + "|" + text2);
			File.WriteAllText(Path.Combine(settingsDirectory, "launch.token"), text + "|" + text2 + "|" + text3);
		}
		catch
		{
		}
	}

	private static string Sign(string data)
	{
		using HMACSHA256 hMACSHA = new HMACSHA256(Encoding.UTF8.GetBytes(DecodeSecret()));
		byte[] array = hMACSHA.ComputeHash(Encoding.UTF8.GetBytes(data));
		StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
		byte[] array2 = array;
		foreach (byte b in array2)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	private static string DecodeSecret()
	{
		byte[] launchTokenSecretKey = AppConstants.LaunchTokenSecretKey;
		byte[] launchTokenSecretCipher = AppConstants.LaunchTokenSecretCipher;
		byte[] array = new byte[launchTokenSecretCipher.Length];
		for (int i = 0; i < launchTokenSecretCipher.Length; i++)
		{
			array[i] = (byte)(launchTokenSecretCipher[i] ^ launchTokenSecretKey[i % launchTokenSecretKey.Length]);
		}
		return Encoding.UTF8.GetString(array);
	}
}

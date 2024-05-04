using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FFMpegCore;
using System.Drawing;

public class VideoToGif
{
	public static async Task<string> FromUrl(string url)
	{
		string inputPath = "videos/downloadedVideo.mp4";
		string outputPath = "videos/output.gif";

		File.Delete(inputPath);
		File.Delete(outputPath);

		using (var httpClient = new HttpClient())
		{
			var videoData = await httpClient.GetByteArrayAsync(url);
			await File.WriteAllBytesAsync(inputPath, videoData);
		}

		await FFMpeg.GifSnapshotAsync(inputPath, outputPath, new Size(240, -1));
		return outputPath;
	}
}
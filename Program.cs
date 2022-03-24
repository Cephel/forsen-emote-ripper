using System.Net;
using System.Net.Http.Headers;
using System.Drawing;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

public class Program
{
	struct betterttvapi
	{
		public string id;
		public string name;
		public string displayName;
		public string providerId;

		public string[] bots;

		public bttvemote[] channelEmotes;
		public bttvemote[] sharedEmotes;
	}

	struct bttvemote
	{
		public string id;
		public string code;
		public string imageType;
		public string userId;
		public string createdAt;
		public string updatedAt;
		public bool global;
		public bool live;
		public bool sharing;
		public string approvalStatus;
	}

	const string forsenFFZURI = "https://www.frankerfacez.com";
	const string forsenFFZURIEMOTE = "https://cdn.frankerfacez.com";


	const string forsenBTTVAPI = "https://api.betterttv.net";
	const string forsenBTTVAPIURI = "3/users/555943515393e61c772ee968?limited=true&personal=false";
	const string forsenBTTVEMOTEAPI = "https://cdn.betterttv.net";

	// Pretend to be a Firefox instance
	const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:98.0) Gecko/20100101 Firefox/98.0";

	// Not too high, else we might get detected as DDOS or something
	const int maxConcurrentDownloads = 8;

	public static void Main(string[] args)
	{
		// Clean output folder
		var files = Directory.EnumerateFiles("output");
		foreach (var file in files)
		{
			if (file.Split("\\").Last() == ".gitkeep")
			{
				continue;
			}

			// Pretty sure this can nuke your OS if someone adds a symlink in the output folder or something
			File.Delete(file);
		}

		// BTTV
		using var bttvClient = new HttpClient();
		bttvClient.BaseAddress = new Uri(forsenBTTVAPI);
		bttvClient.DefaultRequestHeaders.Add("User-Agent", userAgent);

		var bttvResponse = bttvClient.GetAsync(forsenBTTVAPIURI).Result;
		bttvResponse.EnsureSuccessStatusCode();

		var content = bttvResponse.Content.ReadAsStringAsync().Result;
		var bttvjson = JsonConvert.DeserializeObject<betterttvapi>(content);

		var listOfEmotes = bttvjson.channelEmotes.Concat(bttvjson.sharedEmotes);


		// FFZ
		using var ffzClient = new HttpClient();
		ffzClient.BaseAddress = new Uri(forsenFFZURI);
		ffzClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
		var ffzResponse = ffzClient.GetAsync("channel/forsen").Result;

		var ffzhtml = ffzResponse.Content.ReadAsStringAsync().Result;

		// Never ever do this, I don't even know where to start
		// 1. Don't parse HTML with regex
		// 2. Don't leave such a monolithic pattern uncommented
		// 3. Never learn Regex
		// 4. Never tell anyone you learned Regex, else you have to help others with it constantly
		var pattern = "<tr>.*?<td.*?emote-name.*?<a.*?>(.*?)<.*?</td>.*?<td.*?emoticon light.*?img.*?src.*?\".*?com/(.*?)\".*?</td>.*?</tr>";
		var regex = new Regex(pattern, RegexOptions.Singleline);
		var matches = regex.Matches(ffzhtml);

		// Never ever do this either, at least make a proper DTO for disjunct data formats and not just "hotrod" your way through the scheme
		var ffzEmoteList = new List<bttvemote>();
		foreach (Match match in matches)
		{
			var code = match.Groups[1].Value;
			var val = match.Groups[2].Value;
			var sub = val.Substring(0, val.Length - 1);
			var emoteURI = $"{sub}4";

			// Assume everything is png, which is definitely wrong, let's hope they never add gifs
			ffzEmoteList.Add(new bttvemote() { code = code, imageType = "png", id = emoteURI });
		}

		// Download stuff
		var pOpt = new ParallelOptions() { MaxDegreeOfParallelism = maxConcurrentDownloads };
		Parallel.ForEach(listOfEmotes.ToList(), pOpt, emote => RipImageWorkerBTTV(emote));
		Parallel.ForEach(ffzEmoteList.ToList(), pOpt, emote => RipImageWorkerFFZ(emote));
	}

	// These 2 methods could probably be combined, because most code is shared
	private static void RipImageWorkerBTTV(bttvemote emote)
	{
		using var client = new HttpClient();
		client.BaseAddress = new Uri(forsenBTTVEMOTEAPI);

		client.DefaultRequestHeaders.Add("User-Agent", userAgent);

		var response = client.GetAsync($"emote/{emote.id}/3x").Result;
		response.EnsureSuccessStatusCode();

		var stream = response.Content.ReadAsStreamAsync().Result;

		using var image = Image.FromStream(stream);

		ImageFormat imageformat;
		switch (emote.imageType)
		{
			case "png":
				// TODO: Somehow, png formats are binary different to what you download from the website, but there doesn't seem to be a visual difference
				imageformat = ImageFormat.Png;
				break;
			case "gif":
				imageformat = ImageFormat.Gif;
				break;
			default:
				throw new InvalidOperationException($"Unknown emote.imageType: {emote.imageType}");
		}

		image.Save($"output/{emote.code}.{emote.imageType}", imageformat);
	}

	// Do not reuse DTOs like this
	private static void RipImageWorkerFFZ(bttvemote emote)
	{
		using var client = new HttpClient();
		client.BaseAddress = new Uri(forsenFFZURIEMOTE);

		client.DefaultRequestHeaders.Add("User-Agent", userAgent);

		var response = client.GetAsync(emote.id).Result;
		response.EnsureSuccessStatusCode();

		var stream = response.Content.ReadAsStreamAsync().Result;

		using var image = Image.FromStream(stream);
		image.Save($"output/{emote.code}.{emote.imageType}", ImageFormat.Png);
	}
}
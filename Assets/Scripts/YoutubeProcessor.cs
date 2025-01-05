using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using NAudio.Wave;
using System.Threading.Tasks;

public class YoutubeProcessor : MonoBehaviour
{
	public static YoutubeProcessor Instance { get; private set; } = null;
	public AudioSource AudioPlayer { get; private set; }

	readonly YoutubeClient youtube = new();

	const int CACHE_SIZE = 100;
	readonly Dictionary<string, AudioClip> audioCache = new();
	readonly Queue<string> audioCacheQueue = new();
	readonly Dictionary<string, Video> infoCache = new();
	readonly Queue<string> infoCacheQueue = new();

	bool isLoading = false;

	public async Task<Video> GetYoutubeInfo(VideoId videoID)
	{
		if (infoCache.TryGetValue(videoID, out var cacheInfo))
			return cacheInfo;

		isLoading = true;

		// Load youtube audio and save locally ========================================

		var videoInfo = await youtube.Videos.GetAsync(videoID);
		if (videoInfo == null)
		{
			Debug.LogWarning($"Can not load youtube video with ID = {videoID}");
			return null;
		}

		infoCache.Add(videoID, videoInfo);
		infoCacheQueue.Enqueue(videoID);
		if (infoCacheQueue.Count > CACHE_SIZE)
			infoCache.Remove(infoCacheQueue.Dequeue());

		Debug.Log($"Successfully loaded {videoID} info!");
		return videoInfo;
	}

	public async Task<AudioClip> GetYoutubeAudio(VideoId videoID)
	{
		if (isLoading)
		{
			Debug.LogError("Already loading a youtube video!");
			return null;
		}

		AudioClip audioClip = null;
		if (audioCache.TryGetValue(videoID, out audioClip))
			return audioClip;

		isLoading = true;

		// Load youtube audio and save locally ========================================

		var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoID);
		if (streamManifest == null)
		{
			Debug.LogWarning($"Can not load youtube video stream with ID = {videoID}");
			isLoading = false;
			return null;
		}

		var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
		await youtube.Videos.Streams.DownloadAsync(streamInfo, "temp.mka");

		var reader = new MediaFoundationReader("temp.mka");
		MediaFoundationEncoder.EncodeToMp3(reader, "temp.mp3");
		File.Delete("temp.mka");

		// Load locally saved file ========================================

		var uwr = UnityWebRequestMultimedia.GetAudioClip("file:///temp.mp3", AudioType.MPEG);
		((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
		await uwr.SendWebRequest();

		DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;
		if (dlHandler.isDone)
		{
			audioClip = dlHandler.audioClip;
			if (audioClip != null)
			{
				audioCache.Add(videoID, audioClip);
				audioCacheQueue.Enqueue(videoID);
				if (audioCacheQueue.Count > CACHE_SIZE)
					audioCache.Remove(audioCacheQueue.Dequeue());

				Debug.Log($"Successfully loaded {videoID} audio!");
			}
			else
			{
				Debug.Log("Couldn't find a valid AudioClip!");
			}
		}
		else
		{
			Debug.Log("The download process is not completely finished.");
		}

		File.Delete("temp.mp3");
		isLoading = false;
		return audioClip;
	}

	void Awake()
	{
		if (Instance == null)
		{
			AudioPlayer = GetComponent<AudioSource>();
			if (AudioPlayer == null)
				AudioPlayer = gameObject.AddComponent<AudioSource>();

			DontDestroyOnLoad(gameObject);
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	async void TestPlay()
	{
		AudioClip clip = await GetYoutubeAudio("https://www.youtube.com/watch?v=PTUJRM0Ls4s");
		AudioPlayer.clip = clip;
		AudioPlayer.Play();
	}

	// Start is called before the first frame update
	void Start()
    {
		TestPlay();
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}

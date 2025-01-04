using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using NAudio.Wave;

public class YoutubeProcessor : MonoBehaviour
{
	readonly YoutubeClient youtube = new();

	public AudioSource audioPlayer;

	IEnumerator LoadMusic(string songPath)
	{
		if (File.Exists(songPath))
		{
			var uwr = UnityWebRequestMultimedia.GetAudioClip("file:///" + songPath, AudioType.MPEG);
			{
				((DownloadHandlerAudioClip)uwr.downloadHandler).streamAudio = true;
				yield return uwr.SendWebRequest();

				if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
				{
					Debug.LogError(uwr.error);
					yield break;
				}

				DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;

				if (dlHandler.isDone)
				{
					AudioClip audioClip = dlHandler.audioClip;
					if (audioClip != null)
					{
						audioPlayer.clip = audioClip;
						audioPlayer.Play();
						File.Delete("temp.mp3");
						Debug.Log("Playing song using Audio Source!");
					}
					else
					{
						Debug.Log("Couldn't find a valid AudioClip :(");
					}
				}
				else
				{
					Debug.Log("The download process is not completely finished.");
				}
			}
		}
		else
		{
			Debug.Log("Unable to locate converted song file.");
		}
	}

	async void LoadYoutube(string videoUrl)
	{
		var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);
		var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

		await youtube.Videos.Streams.DownloadAsync(streamInfo, "temp.mka");

		var reader = new MediaFoundationReader("temp.mka");
		MediaFoundationEncoder.EncodeToMp3(reader, "temp.mp3");
		
		File.Delete("temp.mka");
		StartCoroutine(LoadMusic("temp.mp3"));
	}

	// Start is called before the first frame update
	void Start()
    {
		LoadYoutube("https://www.youtube.com/watch?v=PTUJRM0Ls4s");
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}

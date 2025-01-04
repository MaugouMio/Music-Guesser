using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YoutubeExplode;

public class YoutubeProcessor : MonoBehaviour
{
	async void Load()
	{
		var youtube = new YoutubeClient();

		var videoURL = "https://www.youtube.com/watch?v=pgXpM4l_MwI";
		var video = await youtube.Videos.GetAsync(videoURL);

		Debug.Log(video.Title);
	}

    // Start is called before the first frame update
    void Start()
    {
		Load();
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}

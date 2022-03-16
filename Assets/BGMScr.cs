using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using TMPro;

public class BGMScr : MonoBehaviour
{

    public AudioSource AudSrc;
    public TextMeshProUGUI NowPlaying;
    public AssetReference[] BGMAddArr = null;
    public List<AudioClip> BGMArr = null;
    public List<AudioClip> AlreadyPlayed;



    // Start is called before the first frame update
    IEnumerator Start()
    {
        BGMArr = new List<AudioClip>();
        AlreadyPlayed = new List<AudioClip>();
        for (int i = 0; i < BGMAddArr.Length; i++)
        {
           var LoadingSong = BGMAddArr[i].LoadAssetAsync<AudioClip>();
           while (!LoadingSong.IsDone)
           {
                yield return new WaitForEndOfFrame();
           }
           BGMArr.Add(LoadingSong.Result);
        }
        Shuffle();
        StartCoroutine(SongFinished());
    }

    public void Shuffle()
    {
        if(BGMArr.Count == 0)
        {
            BGMArr = new List<AudioClip>(AlreadyPlayed);
            AlreadyPlayed.Clear();
        }
        int RNGroll = Random.Range(0, BGMArr.Count - 1);
        AudioClip CandidateSong = BGMArr[RNGroll];
        BGMArr.RemoveAt(RNGroll);
        AlreadyPlayed.Add(CandidateSong);
        AudSrc.clip = CandidateSong;
        NowPlaying.text = "NOW PLAYING: " + AudSrc.clip.name;
        AudSrc.Play();
    }

    public IEnumerator SongFinished()
    {
        yield return new WaitForSeconds(AudSrc.clip.length);
        Shuffle();
    }

    // Update is called once per frame
    void Update() 
    {

    }
}

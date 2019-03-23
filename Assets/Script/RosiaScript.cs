using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Connection;
using IBM.Watson.DeveloperCloud.Utilities;

public class RosiaScript : MonoBehaviour 
{
	[Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/speech-to-text/api\"")]
    [SerializeField]
    private string _serviceUrl;
    [Header("CF Authentication")]
    [Tooltip("IAM API key")]
    [SerializeField]
    private string _apikey;

	private Animator anim;
	private int state = 0;
	private SpeechToText m_SpeechToText;

	public GameObject userSpeechText;
	public GameObject userSpeechUI;
	public GameObject recText;
	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator>();
		Runnable.Run(createService());

		// UIの制御
		userSpeechUI.SetActive(false);
		userSpeechText.GetComponent<Text>().text = "";
		recText.SetActive(false);
	}

	private IEnumerator createService() {
		if (string.IsNullOrEmpty(_apikey)) {
            throw new WatsonException("Plesae provide IAM ApiKey for the service.");
        }
		TokenOptions iamTokenOptions = new TokenOptions() {
			IamApiKey = _apikey
		};
		Credentials credentials = new Credentials(iamTokenOptions, _serviceUrl);

		//  Wait for tokendata
        while (!credentials.HasIamTokenData()) yield return null;

		Debug.Log ("watson connection success");
		m_SpeechToText = new SpeechToText(credentials);
		m_SpeechToText.StreamMultipart = false;
		// SpeechToText を日本語指定して、録音音声をテキストに変換
		m_SpeechToText.Keywords = new string[] { "ibm" };
		m_SpeechToText.KeywordsThreshold = 0.1f;
        m_SpeechToText.RecognizeModel = "ja-JP_BroadbandModel";
	}
	
	IEnumerator transformSpeechToText() {
		// 音声をマイクから 3 秒間取得する
        Debug.Log ("Start record"); //集音開始
		recText.SetActive(true);
        var audioSource = GetComponent<AudioSource>();
        audioSource.clip = Microphone.Start(null, true, 10, 44100);
        audioSource.loop = false;
        audioSource.spatialBlend = 0.0f;
        yield return new WaitForSeconds (3f);
        Microphone.End (null); //集音終了
        Debug.Log ("Finish record");
		recText.SetActive(false);
 
        m_SpeechToText.Recognize(HandleOnRecognize, OnFail, audioSource.clip);
	}

	void HandleOnRecognize(SpeechRecognitionEvent result, Dictionary<string, object> customData) {
		/*
		recognition success handler
		*/
        if (result != null && result.results.Length > 0) {
            foreach (var res in result.results) {
                foreach (var alt in res.alternatives) {
					userSpeechUI.SetActive(true);
                    string recognitionText = alt.transcript;
                    Debug.Log(string.Format("{0} ({1}, {2:0.00})\n", recognitionText, res.final ? "Final" : "Interim", alt.confidence));
					userSpeechText.GetComponent<Text>().text = recognitionText;
					// StartCoroutine(chatAPICall(recognitionText));
					// StartCoroutine(setSpeech( responseRosia(recognitionText) ));
                }
            }
        }
    }

	private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData) {
        Debug.Log("SampleSpeechToText.OnFail() Error received: " + error.ToString());
    }

	// Update is called once per frame
	void Update () {
		anim.SetInteger("state", state);
	}

	public void changeState() {
		state = (state + 1) % 2; //マジックナンバー…
		StartCoroutine(transformSpeechToText());
	}
}

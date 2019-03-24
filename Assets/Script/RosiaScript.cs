using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Connection;
using IBM.Watson.DeveloperCloud.Utilities;
using UnityEngine.Networking;

[System.Serializable]
public class ChatRequestData{
    public string language = "ja-JP";
    public string botId = "Chatting";
    public string appId;
    public string voiceText;
    public string appRecvTime = "2018-06-11 22:44:22";
    public string appSendTime;
    public ChatRequestData(string mes, string appId){
        this.appId = appId;
        voiceText = mes;
        appSendTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

[System.Serializable]
public class ChatResponseJson{

    [SerializeField]
    private SystemText systemText;
    public SystemText SystemText{get{return systemText;}}

    [SerializeField]
    private string command;
    public string Command{get{return command;}}

    [SerializeField]
    private DialogStatus dialogStatus;
    public DialogStatus DialogStatus{get{return dialogStatus;}}

    [SerializeField]
    private string serverSendTime;
    public string ServerSendTime{get{return serverSendTime;}}
}

[System.Serializable]
public class SystemText{
    [SerializeField]
    private string expression;
    public string Expression{get{return expression;}}

    [SerializeField]
    private string utterance;
    public string Utterance{get{return utterance;}}
}

[System.Serializable]
public class DialogStatus{
    [SerializeField]
    private string commandId;
    public string CommandId{get{return commandId;}}

    [SerializeField]
    private IDictionary<string, string> task;
    public IDictionary<string, string> Task{get{return task;}}

    [SerializeField]
    private string loopCount;
    public string LoopCount{get{return loopCount;}}
}

public class RosiaScript : MonoBehaviour 
{
	[Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/speech-to-text/api\"")]
    [SerializeField]
    private string _serviceUrl;
    [Header("CF Authentication")]
    [Tooltip("IAM API key")]
    [SerializeField]
    private string _apikey;

    [Header("API Key")]
    [Tooltip("docomo API Key")]
    [SerializeField]
    private string _docomoApiToken;
    [Tooltip("docomo API Id")]
    [SerializeField]
    private string _docomoAppId;

	private Animator anim;
	private int state = 0;
	private SpeechToText m_SpeechToText;

	public GameObject userSpeechText;
	public GameObject userSpeechUI;
	public GameObject recText;
	public GameObject rosiaText;
	public GameObject rosiaFukidasi;
	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator>();
		Runnable.Run(createService());

		// UIの制御
		userSpeechUI.SetActive(false);
		userSpeechText.GetComponent<Text>().text = "";
		rosiaText.GetComponent<Text>().text = "";
		recText.SetActive(false);
		// rosiaFukidasi.SetActive(false);
		Runnable.Run(setSpeech( "ロージアちゃんにお話ししてね❤" ));
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
					StartCoroutine(chatAPICall(recognitionText));
					// StartCoroutine(setSpeech( responseRosia(recognitionText) ));
                }
            }
        }
    }

	private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData) {
        Debug.Log("SampleSpeechToText.OnFail() Error received: " + error.ToString());
    }

    IEnumerator chatAPICall(string message) {
        // 雑談API URL
        string apiUrl = "https://api.apigw.smt.docomo.ne.jp/naturalChatting/v1/dialogue?APIKEY=" + _docomoApiToken;
        // request bodyのオブジェクトセット
        ChatRequestData requestBody = new ChatRequestData(message, _docomoAppId);
        string requestJson = JsonUtility.ToJson(requestBody);
        byte[] postData = System.Text.Encoding.UTF8.GetBytes (requestJson);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");
        yield return request.SendWebRequest();

        // 通信エラーチェック
        if (request.isNetworkError) {
            Debug.Log(request.error);
        } else {
            if (request.responseCode == 200) {
                // Debug.Log(request.downloadHandler.text);
                ChatResponseJson responseJson = JsonUtility.FromJson<ChatResponseJson>(request.downloadHandler.text);
                Debug.Log(responseJson.SystemText.Expression);
                StartCoroutine(setSpeech( responseJson.SystemText.Expression ));
            }
        }
    }

	IEnumerator setSpeech(string text)
    {
        for (int i = 0; i <= text.Length; i++)
        {
            rosiaText.GetComponent<Text>().text = text.Substring(0, i);
            yield return new WaitForSeconds(0.05f);
        }
    }

	// Update is called once per frame
	void Update () {
		anim.SetInteger("state", state);
	}

	public void changeState() {
		state = 1; //マジックナンバー…
		StartCoroutine(transformSpeechToText());
	}
}

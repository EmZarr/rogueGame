using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class TelemetrySender : MonoBehaviour
{
    [SerializeField] private string webAppUrl;

    public void SendTelemetry(TelemetryData data)
    {
        StartCoroutine(PostTelemetry(data));
    }

    IEnumerator PostTelemetry(TelemetryData data)
    {
        string json = JsonUtility.ToJson(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        
        Debug.Log(data.ToString());
        UnityWebRequest request = new UnityWebRequest(webAppUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyRaw),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        if(request == null)
        {
            Debug.Log("Request not there");
        }

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + request.downloadHandler.text);
        }
        else
            Debug.LogError("Telemetry error: " + request.error);
    }
}
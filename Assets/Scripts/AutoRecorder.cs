#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

public class AutoRecorder : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private TelemetryManager telemetryManager;

    private RecorderController controller;
    private MovieRecorderSettings movieRecorder;

    private bool isRecording;
    private bool restartInProgress = false;

    // Exact path used for the currently active recording (without extension)
    private string currentRecordingPath;

    // Folder only
    private const string RecordingFolder = "C:/Users/olive/Thesis/rogueGame/MyRecordings";

    void Awake()
    {
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();

        Directory.CreateDirectory(RecordingFolder);

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        controllerSettings.SetRecordModeToManual();

        movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorder.name = "Auto Movie Recorder";
        movieRecorder.Enabled = true;
        movieRecorder.FrameRatePlayback = FrameRatePlayback.Variable;
        controllerSettings.CapFrameRate = true;
        controllerSettings.FrameRate = 60;

        var input = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        movieRecorder.ImageInputSettings = input;

        controllerSettings.AddRecorderSettings(movieRecorder);
        controllerSettings.SetRecordModeToManual();

        controller = new RecorderController(controllerSettings);
    }

    public void StartMyRecording()
    {
        if (isRecording) return;
        telemetryManager.GenerateRandomLevelID(10);

        currentRecordingPath = BuildOutputPathFromLevelBehavior();
        movieRecorder.OutputFile = currentRecordingPath;

        controller.PrepareRecording();
        isRecording = controller.StartRecording();

        Debug.Log("Started recording: " + movieRecorder.OutputFile);
    }

    public void StopMyRecording()
    {
        if (!isRecording) return;

        controller.StopRecording();
        isRecording = false;
    }

    public void RestartRecordingAndSave()
    {
        StartCoroutine(RestartRecordingRoutine());
    }

    // Run from level manager on death
    public void DiscardRecording()
    {
        StopMyRecording();
        StartCoroutine(DeleteRecordingFiles(currentRecordingPath));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Normal level transition: keep/save the previous recording
        RestartRecordingAndSave();
    }

    private IEnumerator RestartRecordingRoutine()
    {
        if (restartInProgress) yield break;
        restartInProgress = true;


        StopMyRecording();

        // Give Unity Recorder time to finalize the file on disk
        yield return new WaitForSeconds(1f);


        StartMyRecording();

        restartInProgress = false;
    }

    private IEnumerator DeleteRecordingFiles(string recordingPathWithoutExtension)
    {
        // Give Unity Recorder time to finalize the file on disk
        yield return new WaitForSeconds(1f);
        if (string.IsNullOrEmpty(recordingPathWithoutExtension))
            yield return null;

        string directory = Path.GetDirectoryName(recordingPathWithoutExtension);
        string fileNameWithoutExtension = Path.GetFileName(recordingPathWithoutExtension);

        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            yield return null;

        try
        {
            // Recorder may add its own extension, so delete any file with this exact basename
            string[] matchingFiles = Directory.GetFiles(directory, fileNameWithoutExtension + ".*");

            foreach (string file in matchingFiles)
            {
                File.Delete(file);
                Debug.Log("Deleted discarded recording: " + file);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Failed to delete discarded recording: " + e.Message);
        }
    }

    private string BuildOutputPathFromLevelBehavior()
    {
        var behavior = levelManager.GetCurrentBehaviorTuple();

        // Must match Python exactly:
        // repr((geoX, geoY, furnX, furnY, enemyX, enemyY))
        string tupleString =
            $"({behavior.geoX}, {behavior.geoY}, {behavior.furnX}, {behavior.furnY}, {behavior.enemyX}, {behavior.enemyY})";

        string hash8 = Md5Hex(tupleString).Substring(0, 8);

        // Concrete timestamp so we know exactly what file belongs to this recording

        return Path.Combine(
            RecordingFolder,
            $"{SystemInfo.deviceUniqueIdentifier}_behavior_{hash8}_{telemetryManager.levelPlayID}"
        );
    }

    private static string Md5Hex(string input)
    {
        using var md5 = MD5.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = md5.ComputeHash(bytes);

        StringBuilder sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));

        return sb.ToString();
    }
}
#endif
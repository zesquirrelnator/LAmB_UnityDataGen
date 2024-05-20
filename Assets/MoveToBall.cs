using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SimpleJSON;

[System.Serializable]
public class DatasetEntry
{
    public List<IterationData> data = new List<IterationData>();
}

[System.Serializable]
public class IterationData
{
    public string id;
    public string image; // This will hold the file name or path to the screenshot
    public List<Conversation> conversations = new List<Conversation>();
}

[System.Serializable]
public class Conversation
{
    public string from;
    public string value;
}


public class MoveToBall : MonoBehaviour {
    public GameObject robot;
    public GameObject ball;
    public Camera robotCamera;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private DatasetEntry dataset = new DatasetEntry();
    private int iteration = 0;
    private string screenshotPath;  // Declare this at the class level

    void Start() {
        startPosition = robot.transform.position; // Save initial position
        startRotation = robot.transform.rotation; // Save initial rotation
        MoveBallAndRecord();
    }

    void MoveBallAndRecord() {
        if (iteration >= 501) {
            SaveActionsToJson();
            return;
        }

        // Place the ball in a new random position within the camera's view
        PlaceBallInRandomPosition();

        // Take a snapshot and calculate required actions
        StartCoroutine(CaptureAndMove());

        // Debug
        Debug.Log($"Iteration {iteration}: Completed");
    }

    IEnumerator CaptureAndMove() {
        yield return StartCoroutine(TakeScreenshot(iteration.ToString()));

        float distance = (ball.transform.position - robot.transform.position).magnitude;
        float angle = Vector3.SignedAngle(robot.transform.forward, (ball.transform.position - robot.transform.position).normalized, Vector3.up);

        IterationData iterationData = new IterationData {
            id = iteration.ToString(),
            image = screenshotPath,
            conversations = new List<Conversation> {
                new Conversation { from = "human", value = "move to the red ball" },
                new Conversation { from = "gpt", value = $"action: {distance:0.00} {angle:0.00}" }
            }
        };
        dataset.data.Add(iterationData);

        iteration++;
        if (iteration < 501) {
            MoveBallAndRecord();
        } else {
            SaveActionsToJson();
        }
    }


    IEnumerator TakeScreenshot(string filename) {
        string relativeFolderPath = "/Screenshots/"; // Local path within the Unity project
        string folderPath = Application.dataPath + relativeFolderPath;

        if (!Directory.Exists(folderPath)) {
            Directory.CreateDirectory(folderPath);
        }

        string pathToFile = folderPath + filename + ".png";
        
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = new Texture2D(robotCamera.pixelWidth, robotCamera.pixelHeight, TextureFormat.RGB24, false);
        RenderTexture renderTex = RenderTexture.GetTemporary(robotCamera.pixelWidth, robotCamera.pixelHeight, 24);
        robotCamera.targetTexture = renderTex;
        robotCamera.Render();
        RenderTexture.active = renderTex;
        screenshot.ReadPixels(new Rect(0, 0, robotCamera.pixelWidth, robotCamera.pixelHeight), 0, 0);
        screenshot.Apply();

        robotCamera.targetTexture = null;
        RenderTexture.ReleaseTemporary(renderTex);
        RenderTexture.active = null;

        Texture2D resizedScreenshot = ResizeTexture(screenshot, 107, 53);
        byte[] bytes = resizedScreenshot.EncodeToPNG();
        File.WriteAllBytes(pathToFile, bytes);

        Destroy(screenshot);
        Destroy(resizedScreenshot);

        // Store the relative path in the class variable to use later
        screenshotPath = filename + ".png";
    }

    Texture2D ResizeTexture(Texture2D source, int width, int height) {
        source.filterMode = FilterMode.Bilinear;
        RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    void PlaceBallInRandomPosition() {
        // Distance from camera to place the ball
        float distanceFromCamera = Random.Range(5f, 15f); // Customize this range as needed

        // Calculate the maximum offset from the center (in world space) within 18 degrees
        float maxOffset = Mathf.Tan(Mathf.Deg2Rad * 18) * distanceFromCamera;

        // Randomize the offset within the -18 to +18 degree range
        float offsetX = Random.Range(-maxOffset, maxOffset);

        // The Z offset is based on the distance from the camera; no need for a random range here
        // as the variation is handled by the offsetX and the random distance from the camera.
        
        // Calculate the ball's new position in front of the camera with the randomized offset
        Vector3 newPosition = robotCamera.transform.position + robotCamera.transform.forward * distanceFromCamera + robotCamera.transform.right * offsetX;

        // Set the ball's height to 0.5f explicitly
        newPosition.y = 0.5f;

        // Set the ball's position
        ball.transform.position = newPosition;
    }

    void SaveActionsToJson() {
        // Define the path and filename for the CSV file
        string filePath = Application.dataPath + "/dataset.csv";
        
        // Create a StringBuilder to build the CSV content
        System.Text.StringBuilder csvContent = new System.Text.StringBuilder();
        
        // Write the CSV header
        csvContent.AppendLine("image,prompt,response");
        
        // Iterate through all entries in the dataset and format them as CSV rows
        foreach (IterationData data in dataset.data) {
            string prompt = "move to the red ball"; // Since all prompts are the same
            string response = data.conversations.Find(c => c.from == "gpt").value;
            string image = data.image;
            Debug.Log(image);
            
            // Create the CSV row and append it to the StringBuilder
            csvContent.AppendLine($"{image},{prompt},{response}");
        }
        
        // Write the CSV content to a file
        File.WriteAllText(filePath, csvContent.ToString());
        Debug.Log("Dataset saved to " + filePath);
    }
}
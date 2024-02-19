using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;


public class FeedbackSender : MonoBehaviour {

    public RawImage screenshotRaw;
    public Image screenshot;
    
    
    public bool isMenuActive = false;
    public GameObject menu;
    public RectTransform[] menuRectArea;

    public ToggleGroup feedbackType;
    public TMP_InputField details;

    public TMP_Text metaData;

    public Toggle includeScreenshot;
    public Toggle saveScreenshot;

    private void Start() {
        HideMenu();
    }


    private void Update() {
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (isMenuActive) {
                var rectVal = false;
                for (int i = 0; i < menuRectArea.Length; i++) {
                    var rect = menuRectArea[i].GetComponent<RectTransform>();
                    rectVal = rectVal || RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, OverlayCamsReference.s.uiCam);
                    
                }
                
                if (!rectVal) {
                    HideMenu();
                }
            }
        }
    }

    public void ToggleMenu() {
        if (isMenuActive) {
            HideMenu();
        } else {
            OpenFeedbackForm();
        }
    }

    public void ShowMenu() {
        if (PlayStateMaster.s.isCombatInProgress()) {
            Pauser.s.Pause();
        }
        
        isMenuActive = true;
        menu.SetActive(true);
        metaData.text = GetMetadataText();
    }

    public void HideMenu() {
        isMenuActive = false;
        menu.SetActive(false);
    }



    public void OpenFeedbackForm() {
        StartCoroutine(RecordFrame());
        //CaptureCamera();
    }

    public void SendEmail() {
        _SendEmail();
        Debug.Log("Email Send Successfully");
        HideMenu();
    }

    public void Cancel() {
        HideMenu();
        EmailCleanup();
    }

    IEnumerator RecordFrame()
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        // do something with texture

        string pngName = SSFileName (Screen.width, Screen.height);
        pngPath = Application.dataPath + "/Screenshots/" + pngName;
        SaveTextureAsPNG(texture, pngPath);
        
        
        //var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
        screenshotRaw.texture = texture;

        
        ShowMenu();
    }

    private string pngPath;
    
    readonly MailAddress fromAddress = new MailAddress("sendirontrackstd@gmail.com", "Iron Tracks TD Game");
    readonly MailAddress toAddress = new MailAddress("irontrackstd@gmail.com", "Iron Tracks TD Feedback Collection");
    const string fromPassword = "mfqguwkjonezmftg";
    
    bool _SendEmail() {
        string subject = $"{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss} - {feedbackType.GetFirstActiveToggle().GetComponentInChildren<TMP_Text>().text}";
        string body = GetMessageBody();

        var smtp = new SmtpClient {
            Host = "smtp.gmail.com",
            Port = 587,
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        };

        using (var message = new MailMessage(fromAddress, toAddress) {
                   Subject = subject,
                   Body = body
               }) {
            //message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
            if(includeScreenshot.isOn)
                message.Attachments.Add(new Attachment(pngPath,"image/png"));
            
            try {
                smtp.Send(message);
            } catch (SmtpException e) {
                Debug.LogError(e);
                EmailCleanup();
                return false;
            }
        }

        EmailCleanup();
        return true;
    }


    string GetMessageBody() {
        var body = $"Debug Form {System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
        body += $"\nGame Version: {VersionDisplay.s.GetVersionNumber()}";
        body += $"\nFeedback Type: {feedbackType.GetFirstActiveToggle().GetComponentInChildren<TMP_Text>().text}";
        body += $"\n\nMetadata:\n{metaData.text}";
        body += $"\n\nDetails:\n{details.text}";
        return body;
    }

    string GetMetadataText() {
        var metadata = $"Total playtime on this save: {GetSaveTime()}";
        metadata += $"\nGame State: {PlayStateMaster.s.myGameState}";
        metadata += $"\nLevel Name: {PlayStateMaster.s.currentLevel.levelName}";
        metadata += $"\nCurrent Distance: {SpeedController.s.currentDistance}";

        return metadata;
    }

    string GetSaveTime() {
        var time = DataSaver.s.GetCurrentSave().playtime + DataSaver.s.GetTimeSpentSinceLastSaving();
        return $"{NiceTime(time)}";
    }
    string NiceTime(float time) {
		
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time - minutes * 60);
        string niceTime = string.Format("{0:0}:{1:00}", minutes, seconds);

        return niceTime;
    }
    
    void EmailCleanup() {
        if (!saveScreenshot.isOn) {
            DeleteScreenshot();
        }
    }

    private void OnDisable() {
        if (!saveScreenshot.isOn) {
            if (pngPath != null && pngPath.Length > 0) {
                new FileInfo(pngPath).Delete();
                Debug.Log($"Screenshot deleted successfully: {pngPath}");
            }
        }
    }

    void DeleteScreenshot() {
        StartCoroutine(_DeleteFileAfterRead(pngPath));
    }

    IEnumerator _DeleteFileAfterRead(string path) {

        var file = new FileInfo(path);

        while (IsFileLocked(file)) {
            yield return null; // wait
        }
        
        file.Delete();
        Debug.Log($"Screenshot deleted successfully: {path}");
    }
    
    bool IsFileLocked(FileInfo file)
    {
        try
        {
            using(FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
    }
    
    public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        
        Directory.CreateDirectory(Path.GetDirectoryName(_fullPath));
        
        byte[] _bytes =_texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length/1024  + "Kb was saved as: " + _fullPath);
    }

    string SSFileName(int width, int height) {
        return $"IronTracksTD_SS_{width}x{height}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
    }


    private RenderTexture rt;
    void CaptureCamera()
    {
        // Frame data
        if (rt == null)
        {
            rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.filterMode = FilterMode.Bilinear;
            rt.anisoLevel = 0;
        }

        var baseCam = MainCameraReference.s.cam;
        var cameraData = baseCam.GetUniversalAdditionalCameraData();
        var uiCam = cameraData.cameraStack[0];
        
        baseCam.targetTexture = rt;
        uiCam.targetTexture = rt;
        uiCam.Render(); // not sure why this is necessary but the first render will not render the UI
        baseCam.Render(); // Will render both the base and overlay cameras
        baseCam.targetTexture = null;
        uiCam.targetTexture = null;

        
        var texture = toTexture2D(rt);
        // do something with texture

        //screenshot.texture = texture;
        string pngName = SSFileName (Screen.width, Screen.height);
        pngPath = Application.dataPath + "/Screenshots/" + pngName;
        
        SaveTextureAsPNG(texture, pngPath);
        
        ShowMenu();
    }
    
    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
}

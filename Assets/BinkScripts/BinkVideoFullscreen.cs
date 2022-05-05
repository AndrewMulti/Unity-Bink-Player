/* Bink Player for Unity3D v0.9
 * by AndrewMulti
 * BinkVideoFullscreen.cs draws bink textures on the screen
 */

using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using Bink;

public class BinkVideoFullscreen : MonoBehaviour
{
    public string filename = "";
    public bool Letterbox = true;
    public bool Transparent = false;
    public bool noSound = false;
    public bool loop = false;
    public bool playOnAwake = true;
    public bool turnSubsOff = false;
    public GUISkin subsSkin;
    [Range(0, 255)]
    public byte Volume = 255;
    
    BinkVideo.SubStr[] subs;
    public BinkVideo.TBink binkS;
    XmlDocument xDoc = new XmlDocument();
    Texture2D tex, blackTex;
    IntPtr hBink, Pointer;
    int size, n;
    byte[] bits;
    bool isPlaying, blackTexCreated, hasSubs = false;
    bool isStopped = true;
    float timer = 0.0f;
    uint pgoto = 0;
    bool isgoto = false;

    void Start()
    {
        if (playOnAwake)
            BinkPlay();
    }

    public void BinkPlay()
    {
        BinkVideo.BinkSetSoundSystem(BinkVideo.BinkOpenDirectSound(), IntPtr.Zero);
        hBink = BinkVideo.BinkOpen(Application.dataPath + "\\" + filename, BinkVideo.BinkOpenEnum.BINK_OPEN_STREAM);
        if (noSound)
        {
            Volume = 0;
            BinkVideo.BinkSetVolume(hBink, 0, 0);
        }
        else BinkVideo.BinkSetVolume(hBink, 0, Volume * (uint)655.36);
        binkS = (BinkVideo.TBink)Marshal.PtrToStructure(hBink, typeof(BinkVideo.TBink));
        if (Transparent)
            n = 4;
        else n = 3;
        size = (int)binkS.Width * n * (int)binkS.Height;
        bits = new byte[size];
        Pointer = Marshal.AllocHGlobal(size);
        if (Transparent)
            tex = new Texture2D((int)binkS.Width, (int)binkS.Height, TextureFormat.ARGB32, false);
        else tex = new Texture2D((int)binkS.Width, (int)binkS.Height, TextureFormat.RGB24, false);
        Vector3 vector = new Vector3(-transform.localScale.x, 1, 1);
        transform.localScale = vector;
        isPlaying = true;
        isStopped = false;
        if (File.Exists(Path.ChangeExtension(Application.dataPath + "\\" + filename, "xml")))
        {
            if (subsSkin != null)
            {
                xDoc.Load(Path.ChangeExtension(Application.dataPath + "\\" + filename, "xml"));
                XmlElement xRoot = xDoc.DocumentElement;
                subs = new BinkVideo.SubStr[xRoot.ChildNodes.Count];
                int i = 0;
                hasSubs = true;
                foreach (XmlNode xnode in xRoot)
                {
                    if (xnode.Name == "subtitle")
                    {
                        subs[i].Start = Convert.ToInt32(xnode.Attributes.GetNamedItem("start").Value);
                        subs[i].End = Convert.ToInt32(xnode.Attributes.GetNamedItem("end").Value);
                        subs[i].Text = xnode.ChildNodes[0].InnerText;
                        i++;
                    }
                }
            }
            else Debug.Log("GUI Skin is not assigned");
        }
        InvokeRepeating("DrawTexture", 0.0f, (1.0f / ((float)binkS.FrameRate / (float)binkS.FrameRate2)));
    }

    public bool isBinkStopped()
    {
        if (isStopped)
            return true;
        return false;
    }

    public void BinkStop()
    {
        binkS.CurrentFrame = 0;
        BinkVideo.BinkClose(hBink);
        isPlaying = false;
        isStopped = true;
        timer = 0.0f;
        hasSubs = false;
        CancelInvoke();
    }

    public void BinkGoToFrame(uint a)
    {
        isgoto = true;
        pgoto = a;
        BinkVideo.BinkGoto(hBink, pgoto - 100, 1);
        BinkVideo.BinkGoto(hBink, pgoto, 2);
        timer = 1.0f / ((float)binkS.FrameRate / (float)binkS.FrameRate2) * (float)a;
    }

    void OnGUI()
    {
        if (isPlaying)
        {
            if (Letterbox)
            {
                if (!blackTexCreated)
                {
                    blackTex = new Texture2D(1, 1, TextureFormat.RGB24, false);
                    blackTex.SetPixel(1, 1, Color.black);
                    blackTex.Apply();
                    blackTexCreated = true;
                }
                float xSc = (float)Screen.width / (float)binkS.Width;
                if (!Transparent)
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
                GUI.DrawTexture(new Rect(0, Screen.height - ((Screen.height - (binkS.Height) * xSc) / 2), Screen.width, -binkS.Height * xSc), tex);
            }
            else GUI.DrawTexture(new Rect(0, Screen.height, Screen.width, -Screen.height), tex);
            if ((hasSubs) && (!turnSubsOff))
            {
                for (int i = 0; i < subs.Length; i++)
                {
                    if ((timer >= (float)subs[i].Start / 1000.0) && (timer <= (float)subs[i].End / 1000.0))
                    {
                        subsSkin.GetStyle("Label").fontSize = Screen.height / 32;
                        subsSkin.GetStyle("Label").normal.textColor = Color.black;
                        GUI.Label(new Rect(Screen.width / 6 + 1, 1, Screen.width - Screen.width / 6 * 2 + 1, Screen.height + 1), subs[i].Text, subsSkin.GetStyle("Label"));
                        subsSkin.GetStyle("Label").normal.textColor = Color.white;
                        GUI.Label(new Rect(Screen.width/6, 0, Screen.width-Screen.width/6*2, Screen.height), subs[i].Text, subsSkin.GetStyle("Label"));
                    }
                }
            }
        }
    }

    void DrawTexture()
    {
        if ((binkS.CurrentFrame < binkS.Frames) || loop)
        {
            if (!isgoto)
            {
                BinkVideo.BinkDoFrame(hBink);
                if (Transparent)
                    BinkVideo.BinkCopyToBuffer(hBink, Pointer, binkS.Width * (uint)n, binkS.Height, 0, 0, BinkVideo.BinkSurface.BINKSURFACE32AR);
                else BinkVideo.BinkCopyToBuffer(hBink, Pointer, binkS.Width * 3, binkS.Height, 0, 0, BinkVideo.BinkSurface.BINKSURFACE24R);
                Marshal.Copy(Pointer, bits, 0, size);
                tex.LoadRawTextureData(bits);
                tex.Apply();
                BinkVideo.BinkSetVolume(hBink, 0, (uint)(Volume * 255));
                BinkVideo.BinkNextFrame(hBink);
                timer += 1.0f / ((float)binkS.FrameRate / (float)binkS.FrameRate2);
                binkS.CurrentFrame++;
            }
            else
            {
                BinkGoToFrame(pgoto);
                isgoto = false;
            }
        }
        else
        {
            binkS.CurrentFrame = 0;
            BinkVideo.BinkClose(hBink);
            isPlaying = false;
            isStopped = true;
            timer = 0.0f;
            hasSubs = false;
            CancelInvoke();
        }
    }
}
/* Bink Player for Unity3D v0.9
 * by AndrewMulti
 * BinkVideoObject.cs draws bink textures on object as material
 */

using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using System.Xml;
using System.IO;
using Bink;

public class BinkVideoObject : MonoBehaviour
{
    public string filename = "";
    public bool Transparent = false;
    public bool noSound = false;
    public bool loop = false;
    public bool playOnAwake = true;
    [Tooltip("Copy Bink texture to this planes")]
    public GameObject[] planes;
    public bool turnSubsOff = false;
    public GUISkin subsSkin;
    [Range(0, 255)]
    public byte Volume = 255;

    BinkVideo.TBink binkS;
    Texture2D tex;
    IntPtr hBink, Pointer;
    XmlDocument xDoc = new XmlDocument();
    BinkVideo.SubStr[] subs;
    int size, n;
    float timer = 0.0f;
    byte[] bits;
    bool isStopped = true;
    bool hasSubs = false;
    uint pgoto = 0;
    bool isgoto = false;

    void Start()
    {
        if (playOnAwake)
            BinkPlay();
    }

    public void BinkStop()
    {
        binkS.CurrentFrame = 0;
        BinkVideo.BinkClose(hBink);
        timer = 0.0f;
        hasSubs = false;
        CancelInvoke();
    }

    public bool isBinkStopped()
    {
        if (isStopped)
            return true;
        return false;
    }

    public void BinkPlay()
    {
        BinkVideo.BinkSetSoundSystem(BinkVideo.BinkOpenDirectSound(), IntPtr.Zero);
        hBink = BinkVideo.BinkOpen(Application.dataPath + "\\" + filename, BinkVideo.BinkOpenEnum.BINK_OPEN_STREAM);
        if (noSound)
            BinkVideo.BinkSetVolume(hBink, 0, 0);
        else BinkVideo.BinkSetVolume(hBink, 0, Volume * (uint)655.36);
        binkS = (BinkVideo.TBink)Marshal.PtrToStructure(hBink, typeof(BinkVideo.TBink));
        if (Transparent)
            n = 4;
        else n = 3;
        size = (int)binkS.Width * n * (int)binkS.Height;
        bits = new byte[size];
        isStopped = false;
        Pointer = Marshal.AllocHGlobal(size);
        if (Transparent)
            tex = new Texture2D((int)binkS.Width, (int)binkS.Height, TextureFormat.ARGB32, false);
        else tex = new Texture2D((int)binkS.Width, (int)binkS.Height, TextureFormat.RGB24, false);
        Vector3 vector = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        transform.localScale = vector;
        for (int i = 0; i < planes.Length; i++)
        {
            Vector3 vec = new Vector3(-planes[i].transform.localScale.x, planes[i].transform.localScale.y, planes[i].transform.localScale.z);
            planes[i].transform.localScale = vec;
        }
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

    public void BinkGoToFrame(uint a)
    {
        isgoto = true;
        pgoto = a;
        BinkVideo.BinkGoto(hBink, pgoto - 100, 1);
        BinkVideo.BinkGoto(hBink, pgoto, 0);
        timer = 1.0f / ((float)binkS.FrameRate / (float)binkS.FrameRate2) * (float)a;
    }

    void OnGUI()
    {
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
                    GUI.Label(new Rect(Screen.width / 6, 0, Screen.width - Screen.width / 6 * 2, Screen.height), subs[i].Text, subsSkin.GetStyle("Label"));
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
                else BinkVideo.BinkCopyToBuffer(hBink, Pointer, binkS.Width * (uint)n, binkS.Height, 0, 0, BinkVideo.BinkSurface.BINKSURFACE24R);
                Marshal.Copy(Pointer, bits, 0, size);
                tex.LoadRawTextureData(bits);
                tex.Apply();
                BinkVideo.BinkSetVolume(hBink, 0, (uint)(Volume * 255));
                GetComponent<Renderer>().material.mainTexture = tex;
                for (int i = 0; i < planes.Length; i++)
                    planes[i].GetComponent<Renderer>().material.mainTexture = tex;
                timer += 1.0f / ((float)binkS.FrameRate / (float)binkS.FrameRate2);
                BinkVideo.BinkNextFrame(hBink);
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
            isStopped = true;
            timer = 0.0f;
            hasSubs = false;
            CancelInvoke();
        }
    }
}


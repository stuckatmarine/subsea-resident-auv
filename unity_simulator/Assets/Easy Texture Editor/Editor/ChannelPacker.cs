using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
using System.Text;
using EasyTextureEditor;
#if ! (UNITY_2018 || UNITY_2017)
using System.Threading.Tasks;
#endif


namespace EasyTextureEditor {



    public class ChannelPacker : EditorWindow {

        public enum ChannelPreset {
            custom = 0, StandardMetallicOrURPMetallic = 1, StandardSpecular = 2, HDRPMaskMap = 3
        }


        [MenuItem("Window/Easy Texture Editor/Channel Packer")]
        public static void ShowWindow() {

            GetWindow(typeof(ChannelPacker), false, "Channel Packer");
        }

        Texture2D[] tex = new Texture2D[4];

        float[] texSlider = new float[4];
        bool[] texInvert = new bool[4];
        int[] channelPick = new int[4];
        bool[] resizeFlags = new bool[4];

        Color[] clrs;
        Texture2D output;
        Texture2D t;


        ChannelPreset preset = ChannelPreset.custom;

        SaveFormat saveFormat = SaveFormat._JPEG;
        public enum SaveFormat {
            _JPEG, _PNG
        }



        void LogSize(Texture2D texture, out int x, out int y) {

            string path = AssetDatabase.GetAssetPath(texture);

            string ext = Path.GetExtension(path);

            if (ext == ".png") {
                byte[] buff = new byte[32];

                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    fs.Read(buff, 0, buff.Length);
                    fs.Close();
                }
                const int wOff = 16;
                const int hOff = 20;
                x = BitConverter.ToInt32(new[] { buff[wOff + 3], buff[wOff + 2], buff[wOff + 1], buff[wOff + 0], }, 0);
                y = BitConverter.ToInt32(new[] { buff[hOff + 3], buff[hOff + 2], buff[hOff + 1], buff[hOff + 0], }, 0);

            } else if (ext == ".tga") {
                x = -1;
                y = -1;
                TGALoader.GetSize(path, ref x, ref y);

            } else {

                Vector2 v = new Vector2();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read)) {

                    v = GetDimensions(fs);

                }

                y = (int)v.x;
                x = (int)v.y;

            }
            // Debug.Log("> W: " + x + " H: " + y);
        }


        Texture2D CreateOutput() {

            if (output != null)
                DestroyImmediate(output);
            if (t != null)
                DestroyImmediate(output);


            System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            s.Start();
            int length = 4;
            bool d = false;

            for (int i = 0; i < length; i++) {
                resizeFlags[i] = false;
            }

            for (int i = 0; i < length; i++) {
                if (tex[i] != null) {
                    d = true;
                }
            }


            if (!d) { // no input texture case
                int arbSize = 512;
                Color[][] texSourceData = new Color[4][];
                for (int i = 0; i < length; i++) {
                    Texture2D tmp = new Texture2D(arbSize, arbSize);

                    texSourceData[i] = tmp.GetPixels();
                    for (int z = 0; z < texSourceData[i].Length; z++) {
                        texSourceData[i][z].r = texSlider[i];
                        texSourceData[i][z].g = texSlider[i];
                        texSourceData[i][z].b = texSlider[i];
                        texSourceData[i][z].a = texSlider[i];
                    }

                    DestroyImmediate(tmp);
                }
                output = new Texture2D(arbSize, arbSize);
                Color[] tmpClr = output.GetPixels();
                for (int i = 0; i < tmpClr.Length; i++) {
                    tmpClr[i].r = texSourceData[0][i].r;
                    tmpClr[i].g = texSourceData[1][i].g;
                    tmpClr[i].b = texSourceData[2][i].b;
                    tmpClr[i].a = texSourceData[3][i].a;
                }
                output.SetPixels(tmpClr);
                output.Apply();
                return output;
            }



            int maxIndex = -1;
            int maxSize = -1;
            int x = -1, y = -1;
            Color[] data;
            //Texture2D[] srcTex = new Texture2D[4];



            for (int i = 0; i < length; i++) {
                if (tex[i] != null) {
                    int cx, cy;
                    LogSize(tex[i], out cx, out cy);
                    if (maxSize < cx * cy) {
                        if (maxIndex != -1)
                            resizeFlags[maxIndex] = true;
                        maxSize = cx * cy;
                        x = cx;
                        y = cy;
                        maxIndex = i;
                    } else {
                        resizeFlags[i] = true;
                    }

                }
            }


            float ratio = (float)(y) / (float)x;

            data = new Color[maxSize];

            for (int i = 0; i < length; i++) {
                if (tex[i] != null) {
                    t = null;
                    if (resizeFlags[i]) {
                        //t = QuickEditor.ScaleTexture(LoadImageTex(tex[i]), x, y);
                        if (t != null)
                            DestroyImmediate(t);
                        t = LoadImageTex(tex[i]);
                        Debug.Log(t.width);
                        TextureScale.Bilinear(t, x, y);

                    } else {
                        t = LoadImageTex(tex[i]);
                    }
                    clrs = t.GetPixels(0);

                    // Debug.Log("D:" + data.Length + " vs " + clrs.Length);

                    for (int k = 0; k < maxSize; k++) {
                        float f = 0;

                        if (channelPick[i] == 0)
                            f = clrs[k].r;
                        if (channelPick[i] == 1)
                            f = clrs[k].g;
                        if (channelPick[i] == 2)
                            f = clrs[k].b;
                        if (channelPick[i] == 3)
                            f = clrs[k].a;


                        if (texInvert[i])
                            f = 1 - f;

                        if (i == 0)
                            data[k].r = f;
                        if (i == 1)
                            data[k].g = f;
                        if (i == 2)
                            data[k].b = f;
                        if (i == 3)
                            data[k].a = f;

                    }
                    t = null;
                    clrs = null;
                    GC.Collect(2);

                } else {

                    for (int k = 0; k < maxSize; k++) {
                        float f = texSlider[i];

                        if (i == 0)
                            data[k].r = f;
                        if (i == 1)
                            data[k].g = f;
                        if (i == 2)
                            data[k].b = f;
                        if (i == 3)
                            data[k].a = f;

                    }

                }

            }
            t = null;

            output = new Texture2D(x, y);
            output.SetPixels(data);
            return output;

        }

        Texture2D CreateOutput2() {

            System.Diagnostics.Stopwatch s = new System.Diagnostics.Stopwatch();
            s.Start();
            int length = 4;
            bool d = false;

            Texture2D output;

            for (int i = 0; i < length; i++) {
                if (tex[i] != null) {
                    d = true;
                }
            }


            if (!d) { // no input texture case
                int arbSize = 512;
                Color[][] texSourceData = new Color[4][];
                for (int i = 0; i < length; i++) {
                    Texture2D tmp = new Texture2D(arbSize, arbSize);

                    texSourceData[i] = tmp.GetPixels();
                    for (int z = 0; z < texSourceData[i].Length; z++) {
                        texSourceData[i][z].r = texSlider[i];
                        texSourceData[i][z].g = texSlider[i];
                        texSourceData[i][z].b = texSlider[i];
                        texSourceData[i][z].a = texSlider[i];
                    }
                }
                output = new Texture2D(arbSize, arbSize);
                Color[] tmpClr = output.GetPixels();
                for (int i = 0; i < tmpClr.Length; i++) {
                    tmpClr[i].r = texSourceData[0][i].r;
                    tmpClr[i].g = texSourceData[1][i].g;
                    tmpClr[i].b = texSourceData[2][i].b;
                    tmpClr[i].a = texSourceData[3][i].a;
                }
                output.SetPixels(tmpClr);
                output.Apply();
                return output;
            }



            int maxIndex = -1;
            int maxSize = -1;
            int x = -1, y = -1;
            Texture2D[] srcTex = new Texture2D[4];



            for (int i = 0; i < length; i++) {
                if (tex[i] != null) {
                    srcTex[i] = LoadImageTex(tex[i]);
                    if (maxSize < srcTex[i].width * srcTex[i].height) {
                        maxSize = srcTex[i].width * srcTex[i].height;
                        maxIndex = i;
                        x = srcTex[i].width;
                        y = srcTex[i].height;
                    }
                } else
                    srcTex[i] = null;
            }

            float ratio = (float)(srcTex[maxIndex].height) / srcTex[maxIndex].width;

            for (int i = 0; i < length; i++) {
                if (srcTex[i] != null) {
                    if (i != maxIndex) {
                        if (srcTex[i].width != srcTex[maxIndex].width && srcTex[maxIndex].height != srcTex[i].height)
                            srcTex[i] = QuickEditor.ScaleTexture(srcTex[i], (int)srcTex[maxIndex].width, (int)((int)srcTex[maxIndex].width * ratio));

                        // TextureScale.Bilinear(srcTex[i], (int)srcTex[maxIndex].width, (int)((int)srcTex[maxIndex].width * ratio));
                        //  QuickEditor.ScaleTexture(srcTex[i], (int)srcTex[maxIndex].width, (int)((int)srcTex[maxIndex].width * ratio));
                    }
                }
            }

            Color[][] inColors = new Color[4][];

            for (int i = 0; i < length; i++) {
                if (srcTex[i] != null) {
                    inColors[i] = srcTex[i].GetPixels();
                }
            }
            srcTex = null;
            GC.Collect();


            Color[] outColors = new Color[maxSize];

            for (int z = 0; z < outColors.Length; z++) {

                for (int i = 0; i < length; i++) {
                    float value = 0f;
                    if (inColors[i] != null) {
                        switch (channelPick[i]) {
                            case 0:
                                value = inColors[i][z].r;
                                break;
                            case 1:
                                value = inColors[i][z].g;
                                break;
                            case 2:
                                value = inColors[i][z].b;
                                break;
                            case 3:
                                value = inColors[i][z].a;
                                break;
                            default:
                                break;
                        }
                        if (texInvert[i])
                            value = 1 - value;

                    } else {
                        value = texSlider[i];
                    }

                    switch (i) {
                        case 0:
                            outColors[z].r = value;
                            break;
                        case 1:
                            outColors[z].g = value;
                            break;
                        case 2:
                            outColors[z].b = value;
                            break;
                        case 3:
                            outColors[z].a = value;
                            break;
                        default:
                            break;
                    }


                }

            }
            inColors = null;

            output = new Texture2D(x, y);
            output.SetPixels(outColors);
            output.Apply();

            s.Stop();
            Debug.Log("time: " + s.ElapsedMilliseconds);
            return output;

        }


        Color[] LoadImageSrc(Texture2D target) {

            string path = AssetDatabase.GetAssetPath(target);

            byte[] texData = File.ReadAllBytes(path);
            Texture2D fullTexture = new Texture2D(2, 2);
            ImageConversion.LoadImage(fullTexture, texData);
            return fullTexture.GetPixels(0);
        }

        Texture2D LoadImageTex(Texture2D target) {
            string path = AssetDatabase.GetAssetPath(target);

            if (Path.GetExtension(path) == ".tga") {
                return TGALoader.LoadTGA(path);
            }

            byte[] texData = File.ReadAllBytes(path);
            Texture2D fullTexture = new Texture2D(2, 2);
            ImageConversion.LoadImage(fullTexture, texData);
            return fullTexture;

        }

        Vector2 scrollPos;
        private void OnGUI() {

            preset = (ChannelPreset)EditorGUILayout.EnumPopup(preset);

            scrollPos = GUILayout.BeginScrollView(scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (preset == ChannelPreset.custom) {

                string[] s = {
                "R channel output",
                "G channel output",
                "B channel output",
                "A channel output"
                    };
                string[] s2 = {
                "R",
                "G",
                "B",
                "A"
                    };
                for (int i = 0; i < tex.Length; i++) {
                    tex[i] = EditorGUILayout.ObjectField(s[i], tex[i], typeof(Texture2D), false) as Texture2D;
                    if (tex[i] != null)
                        EditorGUILayout.LabelField("File name: " + Path.GetFileName(AssetDatabase.GetAssetPath(tex[i])));
                    if (tex[i] == null)
                        texSlider[i] = EditorGUILayout.Slider("Default value", texSlider[i], 0f, 1f);
                    else {

                        Color c = GUI.color;
                        if (saveFormat == SaveFormat._JPEG && i == tex.Length - 1) {
                            GUI.color = Color.red;
                            EditorGUILayout.LabelField("Use .PNG format if you need the Alpha channel!");

                        }

                        texInvert[i] = EditorGUILayout.ToggleLeft("invert", texInvert[i], GUILayout.Width(50));
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Input channel", GUILayout.Width(150));
                        channelPick[i] = GUILayout.SelectionGrid(channelPick[i], s2, 4);

                        EditorGUILayout.EndHorizontal();

                        GUI.color = c;


                    }
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }
                GUI.skin.button.wordWrap = true;
            } else if (preset == ChannelPreset.StandardMetallicOrURPMetallic) {
                saveFormat = SaveFormat._PNG;
                string[] s2 = {
                "R",
                "G",
                "B",
                "A"
                    };

                tex[0] = EditorGUILayout.ObjectField("Metallic", tex[0], typeof(Texture2D), false) as Texture2D;
                if (tex[0] != null)
                    EditorGUILayout.LabelField("File name: " + Path.GetFileName(AssetDatabase.GetAssetPath(tex[0])));




                if (tex[0] == null)
                    texSlider[0] = EditorGUILayout.Slider("Default value", texSlider[0], 0f, 1f);
                else {
                    /*
                    Color c = GUI.color;
                    if (saveFormat == SaveFormat._JPEG && i == tex.Length - 1) {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("Use .PNG format if you need the Alpha channel!");

                    }*/

                    texInvert[0] = EditorGUILayout.ToggleLeft("invert", texInvert[0], GUILayout.Width(50));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Input channel", GUILayout.Width(150));
                    channelPick[0] = GUILayout.SelectionGrid(channelPick[0], s2, 4);

                    tex[1] = null;
                    tex[2] = null;
                    texSlider[1] = 0f;
                    texSlider[2] = 0f;



                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);



                tex[3] = EditorGUILayout.ObjectField("Smoothness", tex[3], typeof(Texture2D), false) as Texture2D;
                if (tex[3] != null)
                    EditorGUILayout.LabelField("File name: " + Path.GetFileName(AssetDatabase.GetAssetPath(tex[3])));
                if (tex[3] == null)
                    texSlider[3] = EditorGUILayout.Slider("Default value", texSlider[3], 0f, 1f);
                else {
                    /*
                    Color c = GUI.color;
                    if (saveFormat == SaveFormat._JPEG && i == tex.Length - 1) {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("Use .PNG format if you need the Alpha channel!");

                    }*/

                    texInvert[3] = EditorGUILayout.ToggleLeft("invert", texInvert[3], GUILayout.Width(50));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Input channel", GUILayout.Width(150));
                    channelPick[3] = GUILayout.SelectionGrid(channelPick[3], s2, 4);

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField("(Smoothness is usually inverted roughness.)");
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);






            } else if (preset == ChannelPreset.StandardSpecular) {
                saveFormat = SaveFormat._PNG;
                string[] s2 = {
                "R",
                "G",
                "B",
                "A"
                    };

                tex[0] = EditorGUILayout.ObjectField("Specular RGB", tex[0], typeof(Texture2D), false) as Texture2D;
                if (tex[0] != null)
                    EditorGUILayout.LabelField("File name: " + Path.GetFileName(AssetDatabase.GetAssetPath(tex[0])));




                if (tex[0] == null)
                    texSlider[0] = EditorGUILayout.Slider("Default value", texSlider[0], 0f, 1f);
                else {
                    /*
                    Color c = GUI.color;
                    if (saveFormat == SaveFormat._JPEG && i == tex.Length - 1) {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("Use .PNG format if you need the Alpha channel!");

                    }*/

                    texInvert[0] = EditorGUILayout.ToggleLeft("invert", texInvert[0], GUILayout.Width(50));
                    // EditorGUILayout.BeginHorizontal();
                    //EditorGUILayout.LabelField("Input channel", GUILayout.Width(150));
                    channelPick[0] = 0;
                    channelPick[1] = 1;
                    channelPick[2] = 2;

                    tex[1] = tex[0];
                    tex[2] = tex[0];
                    texSlider[1] = texSlider[0];
                    texSlider[2] = texSlider[0];



                    // EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);



                tex[3] = EditorGUILayout.ObjectField("Smoothness", tex[3], typeof(Texture2D), false) as Texture2D;
                if (tex[3] != null)
                    EditorGUILayout.LabelField("File name: " + Path.GetFileName(AssetDatabase.GetAssetPath(tex[3])));
                if (tex[3] == null)
                    texSlider[3] = EditorGUILayout.Slider("Default value", texSlider[3], 0f, 1f);
                else {
                    /*
                    Color c = GUI.color;
                    if (saveFormat == SaveFormat._JPEG && i == tex.Length - 1) {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("Use .PNG format if you need the Alpha channel!");

                    }*/

                    texInvert[3] = EditorGUILayout.ToggleLeft("invert", texInvert[3], GUILayout.Width(50));
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Input channel", GUILayout.Width(150));
                    channelPick[3] = GUILayout.SelectionGrid(channelPick[3], s2, 4);

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.LabelField("(Smoothness is usually inverted roughness.)");
                }
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            } else if (preset == ChannelPreset.HDRPMaskMap) {

                string[] s = {
                "Metallic",
                "Ambient Occlusion",
                "Detail mask",
                "Smoothness"
                    };
                string[] s2 = {
                "R",
                "G",
                "B",
                "A"
                    };
                for (int i = 0; i < tex.Length; i++) {
                    tex[i] = EditorGUILayout.ObjectField(s[i], tex[i], typeof(Texture2D), false) as Texture2D;
                    if (tex[i] != null)
                        EditorGUILayout.LabelField("File name: " + Path.GetFileName(AssetDatabase.GetAssetPath(tex[i])));
                    if (tex[i] == null)
                        texSlider[i] = EditorGUILayout.Slider("Default value", texSlider[i], 0f, 1f);
                    else {

                        Color c = GUI.color;
                        if (saveFormat == SaveFormat._JPEG && i == tex.Length - 1) {
                            GUI.color = Color.red;
                            EditorGUILayout.LabelField("Use .PNG format if you need the Alpha channel!");

                        }

                        texInvert[i] = EditorGUILayout.ToggleLeft("invert", texInvert[i], GUILayout.Width(50));
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Input channel", GUILayout.Width(150));
                        channelPick[i] = GUILayout.SelectionGrid(channelPick[i], s2, 4);

                        EditorGUILayout.EndHorizontal();

                        GUI.color = c;


                    }
                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }
                GUI.skin.button.wordWrap = true;
            }



            GUILayout.BeginHorizontal();
            saveFormat = (SaveFormat)EditorGUILayout.EnumPopup(saveFormat, GUILayout.Width(50));

            if (GUILayout.Button("Save as ..")) {

                bool e = true;
                string currentPath = null;
                string name = null;
                string path;
                for (int i = 0; i < 4; i++) {
                    if (tex[i] != null) {
                        currentPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(tex[i]));
                        name = "Custom Channel Map";
                        e = false;
                        break;
                    }
                }

                if (e) {
                    currentPath = "/Assets/";
                    name = "Custom Channel Map";
                }

                //currentPath = AssetDatabase.GetAssetPath(tex[0]);
                //name = Path.GetFileName(currentPath);
                //currentPath = Path.GetDirectoryName(currentPath);
                if (saveFormat == SaveFormat._JPEG) {
                    path = EditorUtility.SaveFilePanel("Save image", currentPath, name, "jpeg");
                    if (path != "") {
                        QuickEditor.SaveTextureAsJPG(CreateOutput(), path, true);

                        AssetDatabase.Refresh();
#if ! (UNITY_2018 || UNITY_2017)
                        Wtf();
#endif
                    }
                } else {
                    path = EditorUtility.SaveFilePanel("Save image", currentPath, name, "png");
                    if (path != "") {
                        QuickEditor.SaveTextureAsPNG(CreateOutput(), path, true);

                        AssetDatabase.Refresh();
#if ! (UNITY_2018 || UNITY_2017)
                        Wtf();
#endif
                    }
                }


            }
            if (GUILayout.Button("Open first input folder")) {
                for (int i = 0; i < 4; i++) {
                    if (tex[i] != null) {
                        string currentPath = AssetDatabase.GetAssetPath(tex[i]);
                        System.Diagnostics.Process.Start("explorer.exe", "/select," + Path.GetFullPath(currentPath));
                        break;
                    }
                }
                // string currentPath = AssetDatabase.GetAssetPath(tex);
                // System.Diagnostics.Process.Start("explorer.exe", "/select," + Path.GetFullPath(currentPath));
            }
            GUILayout.EndHorizontal();
            /*
            if (GUILayout.Button("Log size")) {

                int x, y;
                LogSize(tex[0],out x, out y); 

            }
            */
            EditorGUILayout.EndScrollView();
        }

#if ! (UNITY_2018 || UNITY_2017)
        async void Wtf() {
            clrs = null;
            output = null;
            t = null;
            await Task.Delay(1000);

            for (int i = 0; i < 5; i++) {

                GC.Collect(20);
                GC.Collect(20);
                GC.Collect(20);
            }
        }
#endif

        static Vector2 GetDimensions(Stream jpegStream) {
            const byte SectionStartMarker = 0xff;
            const byte SOF0 = 0xc0;
            const byte SOF2 = 0xc2;
            const byte SOS = 0xda;
            jpegStream.Read(new byte[2], 0, 2);

            Vector2 imageDetails = new Vector2();

            byte[] headerBuffer = new byte[4];

            do {
                jpegStream.Read(headerBuffer, 0, headerBuffer.Length);

                if (headerBuffer[0] != SectionStartMarker) {
                    int i = 0;

                    while (headerBuffer[i] != SectionStartMarker) {
                        i++;
                        if (i == headerBuffer.Length) {
                            jpegStream.Read(headerBuffer, 0, headerBuffer.Length);
                            i = 0;
                        }
                    }

                    if (i != 0) {
                        Buffer.BlockCopy(headerBuffer, i, headerBuffer, 0, headerBuffer.Length - i);
                        jpegStream.Read(headerBuffer, headerBuffer.Length - i, headerBuffer.Length - (headerBuffer.Length - i));
                    }
                }

                ushort length = ReadLength(headerBuffer, 2);
                byte[] headerData = new byte[length - 2];
                jpegStream.Read(headerData, 0, headerData.Length);

                if (headerBuffer[1] == SOF0 || headerBuffer[1] == SOF2) {
                    imageDetails = new Vector2(ReadLength(headerData, 1), ReadLength(headerData, 3));
                }
            }
            while (headerBuffer[1] != SOS && imageDetails.y == 0);
            return imageDetails;
        }

        static ushort ReadLength(byte[] buffer, int index) {
            byte[] lengthBuffer = new byte[2];
            Array.Copy(buffer, index, lengthBuffer, 0, 2);
            if (BitConverter.IsLittleEndian) {
                Array.Reverse(lengthBuffer);
            }
            ushort length = BitConverter.ToUInt16(lengthBuffer, 0);
            return length;
        }
        private void OnInspectorUpdate() {

        }

    }


}
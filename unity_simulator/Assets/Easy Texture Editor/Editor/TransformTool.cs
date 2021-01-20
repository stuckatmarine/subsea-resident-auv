using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using EasyTextureEditor;

namespace EasyTextureEditor {

    public class TransformTool : EditorWindow {

        Texture2D _tex;
        Texture2D tex {
            get {
                return _tex;
            }
            set {
                if (_tex != value) {
                    _tex = value;
                    LoadImage();
                    PreviewUpdate();
                    suffix = new string[3];
                    for (int i = 0; i < 3; i++) {
                        suffix[i] = "";
                    }
                    if (value == null) {
                        preview = null;
                    }
                }

            }
        }
        Texture2D preview;
        Texture2D fullTexture;

        Color[] befColors;
        //Color[] aftColors;

        private enum SaveFormat {
            _JPEG, _PNG
        }

        SaveFormat saveFormat = new SaveFormat();
        void LoadImage() {
            int previewSize = 512;
            if (tex == null)
                return;

            string path = AssetDatabase.GetAssetPath(tex);


            if (Path.GetExtension(path) == ".tga") {
                fullTexture = TGALoader.LoadTGA(path);
            } else {
                byte[] texData = File.ReadAllBytes(path);
                fullTexture = new Texture2D(2, 2);
                ImageConversion.LoadImage(fullTexture, texData);
            }



            // Color[] texClrs = fullTexture.GetPixels(0);

            float ratio = (float)(fullTexture.height) / fullTexture.width;
            preview = QuickEditor.ScaleTexture(fullTexture, (int)previewSize, (int)((int)previewSize * ratio));


            befColors = preview.GetPixels(0);

        }

        void Swap(ref int a, ref int b) {
            int c = a;
            a = b;
            b = c;
        }


        void CreateOutput() {
            Color[] clrs = fullTexture.GetPixels();

            int sX = fullTexture.width;
            int sY = fullTexture.height;
            switch (suffix[0]) {
                case "_90ccw":
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    fullTexture = new Texture2D(sX, sY);
                    break;
                case "_180ccw":
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    break;
                case "_270ccw":
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    fullTexture = new Texture2D(sX, sY);
                    break;
                case "_90cw":
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    fullTexture = new Texture2D(sX, sY);
                    break;
                case "_180cw":
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    break;
                case "_270cw":
                    clrs = RotateCW(clrs, sX, sY);
                    Swap(ref sX, ref sY);
                    fullTexture = new Texture2D(sX, sY);
                    break;
            }
            if (suffix[1] == "_mX")
                clrs = MirroX(clrs, fullTexture.width, fullTexture.height);
            if (suffix[2] == "_mY")
                clrs = MirroY(clrs, fullTexture.width, fullTexture.height);


            //fullTexture = new Texture2D(sX,sY);
            fullTexture.SetPixels(clrs);
            fullTexture.Apply();
            clrs = null;

        }


        private void PreviewUpdate() {

            if (tex == null)
                return;

            preview.SetPixels(befColors, 0);
            preview.Apply();


        }

        int getI(int x, int y, int width) {

            return y * width + x;

        }


        private Color[] RotateCW(Color[] clrs, int width, int height) {
            Color[] output = new Color[clrs.Length];


            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int i1 = getI(x, height - y - 1, width);
                    int i2 = getI(y, x, height);
                    output[i2] = clrs[i1];
                }
            }
            return output;


        }


        private Color[] MirroX(Color[] clrs, int width, int height) {
            Color[] output = new Color[clrs.Length];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int i1 = getI(x, y, width);
                    int i2 = getI(width - 1 - x, y, width);
                    output[i1] = clrs[i2];
                }
            }
            return output;
        }


        private Color[] MirroY(Color[] clrs, int width, int height) {
            Color[] output = new Color[clrs.Length];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int i1 = getI(x, y, width);
                    int i2 = getI(x, height - 1 - y, width);
                    output[i1] = clrs[i2];
                }
            }
            return output;
        }

        string[] suffix = { "", "", "" };

        Vector2 scrollPos;
        private void OnGUI() {


            // scrollPos = GUILayout.BeginScrollView(scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            tex = EditorGUILayout.ObjectField("Input texture", tex, typeof(Texture2D), false) as Texture2D;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("<- Rotate 90° ccw") && tex != null) {
                befColors = RotateCW(befColors, preview.width, preview.height);
                preview = new Texture2D(preview.height, preview.width);
                PreviewUpdate();

                switch (suffix[0]) {
                    case "":
                        suffix[0] = "_90ccw";
                        break;
                    case "_90ccw":
                        suffix[0] = "_180ccw";
                        break;
                    case "_180ccw":
                        suffix[0] = "_270ccw";
                        break;
                    case "_270ccw":
                        suffix[0] = "";
                        break;
                    case "_90cw":
                        suffix[0] = "";
                        break;
                    case "_180cw":
                        suffix[0] = "_90cw";
                        break;
                    case "_270cw":
                        suffix[0] = "_180cw";
                        break;
                }


            }
            if (GUILayout.Button("Rotate 180°") && tex != null) {
                for (int i = 0; i < 2; i++) {
                    befColors = RotateCW(befColors, preview.width, preview.height);
                    preview = new Texture2D(preview.height, preview.width);

                }
                PreviewUpdate();


                switch (suffix[0]) {
                    case "":
                        suffix[0] = "_180cw";
                        break;
                    case "_90cw":
                        suffix[0] = "_270cw";
                        break;
                    case "_180cw":
                        suffix[0] = "";
                        break;
                    case "_270cw":
                        suffix[0] = "_90cw";
                        break;
                    case "_90ccw":
                        suffix[0] = "_270ccw";
                        break;
                    case "_180ccw":
                        suffix[0] = "";
                        break;
                    case "_270ccw":
                        suffix[0] = "_90ccw";
                        break;
                }


            }

            if (GUILayout.Button("Rotate 90° cw ->") && tex != null) {
                for (int i = 0; i < 3; i++) {
                    befColors = RotateCW(befColors, preview.width, preview.height);
                    preview = new Texture2D(preview.height, preview.width);

                }
                PreviewUpdate();


                switch (suffix[0]) {
                    case "":
                        suffix[0] = "_90cw";
                        break;
                    case "_90cw":
                        suffix[0] = "_180cw";
                        break;
                    case "_180cw":
                        suffix[0] = "_270cw";
                        break;
                    case "_270cw":
                        suffix[0] = "";
                        break;
                    case "_90ccw":
                        suffix[0] = "";
                        break;
                    case "_180ccw":
                        suffix[0] = "_90ccw";
                        break;
                    case "_270ccw":
                        suffix[0] = "_180cw";
                        break;
                }
            }


            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Mirror by X axis") && tex != null) {
                befColors = MirroX(befColors, preview.width, preview.height);

                PreviewUpdate();
                switch (suffix[1]) {
                    case "":
                        suffix[1] = "_mX";
                        break;
                    case "_mX":
                        suffix[1] = "";
                        break;
                }

            }
            if (GUILayout.Button("Mirror by Y axis") && tex != null) {
                befColors = MirroY(befColors, preview.width, preview.height);

                PreviewUpdate();

                switch (suffix[2]) {
                    case "":
                        suffix[2] = "_mY";
                        break;
                    case "_mY":
                        suffix[2] = "";
                        break;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            string assetPath = AssetDatabase.GetAssetPath(tex);
            string name = Path.GetFileNameWithoutExtension(assetPath);
            string qname = name;
            foreach (var item in suffix) {
                qname += item;
            }
            string newPath = null;
            if (tex != null)
                newPath = Path.GetDirectoryName(Path.GetFullPath(assetPath)) + "\\" + qname;
            GUILayout.Label("Quick save name: " + qname, EditorStyles.wordWrappedLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Quick save as JPEG") && tex != null) {
                bool checkDiff = false;
                for (int i = 0; i < 3; i++) {
                    if (suffix[i] != "")
                        checkDiff = true;
                }

                if (checkDiff) {

                    CreateOutput();
                    QuickEditor.SaveTextureAsJPG(fullTexture, newPath, false);

                    Texture2D k = tex;
                    tex = null;
                    tex = k;


                    AssetDatabase.Refresh();
                } else {
                    Debug.Log("Nothing to save.");
                }
            }
            if (GUILayout.Button("Quick save as PNG") && tex != null) {
                bool checkDiff = false;

                for (int i = 0; i < 3; i++) {
                    if (suffix[i] != "")
                        checkDiff = true;
                }
                if (checkDiff) {
                    CreateOutput();
                    QuickEditor.SaveTextureAsPNG(fullTexture, newPath, false);

                    Texture2D k = tex;
                    tex = null;
                    tex = k;


                    AssetDatabase.Refresh();
                } else {
                    Debug.Log("Nothing to save.");
                }
            }
            GUILayout.EndHorizontal();

            //GUILayout.Space(15);

            GUILayout.BeginHorizontal();
            saveFormat = (SaveFormat)EditorGUILayout.EnumPopup(saveFormat, GUILayout.Width(50));

            if (GUILayout.Button("Save elsewhere ...") && tex != null) {
                string currentPath = AssetDatabase.GetAssetPath(tex);
                //string name = Path.GetFileName(currentPath);
                currentPath = Path.GetDirectoryName(currentPath);
                if (saveFormat == SaveFormat._JPEG) {
                    string path = EditorUtility.SaveFilePanel("Save image", currentPath, name, "jpeg");
                    if (path != "") {

                        CreateOutput();
                        QuickEditor.SaveTextureAsJPG(fullTexture, path, true);

                        Texture2D k = tex;
                        tex = null;
                        tex = k;


                        AssetDatabase.Refresh();
                    }
                } else {
                    string path = EditorUtility.SaveFilePanel("Save image", currentPath, name, "png");
                    if (path != "") {
                        CreateOutput();
                        QuickEditor.SaveTextureAsPNG(fullTexture, path, true);

                        Texture2D k = tex;
                        tex = null;
                        tex = k;


                        AssetDatabase.Refresh();
                    }
                }


            }
            if (GUILayout.Button("Open input folder") && tex != null) {
                string currentPath = AssetDatabase.GetAssetPath(tex);
                System.Diagnostics.Process.Start("explorer.exe", "/select," + Path.GetFullPath(currentPath));
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            if (preview != null) {
                int padding = 10;
                int verticalOffset = 235;
                float ratio = (float)preview.width / (float)preview.height;
                int w = preview.width;
                int h = preview.height;
                float windowRatio = position.width / preview.width;
                if (w > h) {
                    w = (int)(position.width - padding * 2);
                    h = (int)(windowRatio * preview.height);
                } else {
                    h = (int)(position.width - padding * 2);
                    w = (int)(h * ratio);
                }
                GUI.DrawTexture(new Rect(padding, verticalOffset, w, h), preview);
                //GUI.DrawTexture(new Rect(padding, verticalOffset, position.width - padding * 2, windowRatio * preview.height), preview);
                //GUILayout.Space(windowRatio * preview.height + padding);
                GUILayout.Space(windowRatio * preview.height);
                GUILayout.Space(padding);
            }


            //  EditorGUILayout.EndScrollView();
        }

        [MenuItem("Window/Easy Texture Editor/Transform tool")]
        public static void ShowWindow() {

            GetWindow(typeof(TransformTool), false, "Transform Tool");
        }


        private void OnInspectorUpdate() {

        }


    }
}
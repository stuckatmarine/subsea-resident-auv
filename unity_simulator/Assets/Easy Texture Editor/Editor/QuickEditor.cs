using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System;
using System.Threading;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace EasyTextureEditor {

    public class QuickEditor : EditorWindow {

        public Texture2D tex {
            get {
                return _tex;
            }
            set {
                if (value != _tex) {
                    _tex = value;
                    LoadImage();
                    if (value == null) {
                        preview.DiscardContents();
                        preview = null;
                    }
                }
            }
        }
        private Texture2D _tex = null;
        private RenderTexture preview;
        private RenderTexture befPreview;





        private Texture2D fullTexture = null;



        //public Texture2D preview = null;

        private byte[] texData;
        private Color[] texClrs;

        private Color[] aftClrs;

        private static bool init = false;

        private static List<ImageEffect> effects;

        bool enablePreview = true;


        private PreviewSize _previewSize = PreviewSize._512pxPreview;

        private static ComputeShader compute;

        private PreviewSize previewSize {
            get { return _previewSize; }
            set {
                if (_previewSize != value) {
                    _previewSize = value;
                    LoadImage();
                    PreviewUpdate();
                }
            }
        }
        private SaveFormat saveFormat;
        enum PreviewSize {
            _128pxPreview = 128, _256pxPreview = 256, _512pxPreview = 512, _1024pxPreview = 1024, _2048pxPreview = 2048
        }
        private enum SaveFormat {
            _JPEG, _PNG
        }

        public static void Init() {
            effects = new List<ImageEffect>();
            effects.Add(new IEBrightness());
            effects.Add(new IEContrast());
            effects.Add(new IEGAmme());
            effects.Add(new IEExposure());
            effects.Add(new IESaturation());
            effects.Add(new IEHue());
            effects.Add(new IEInvert());
            effects.Add(new IEGrayscale());
            effects.Add(new IEBlending());
            effects.Add(new IEReplace());

            compute = (ComputeShader)Resources.Load("ETEComputeShader");

            init = true;
        }




        private void PreviewUpdate() {

            if (tex == null)
                return;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            RenderTexture.active = null;
            preview.Release();
            preview = new RenderTexture(befPreview.width, befPreview.height, 32);
            preview.enableRandomWrite = true;
            // preview.Create();
            RenderTexture.active = preview;

            Graphics.Blit(befPreview, preview);




            if (enablePreview)
                foreach (var effect in effects) {
                    effect.ApplyGPU(ref preview, compute);
                }



            stopwatch.Stop();
        }

        void LoadImage() {
            if (tex == null)
                return;

            string path = AssetDatabase.GetAssetPath(tex);

            if (Path.GetExtension(path) == ".tga") {
                fullTexture = TGALoader.LoadTGA(path);
            } else {
                texData = File.ReadAllBytes(path);
                fullTexture = new Texture2D(2, 2);
                ImageConversion.LoadImage(fullTexture, texData);
            }

            texClrs = fullTexture.GetPixels(0);



            float ratio = (float)(fullTexture.height) / fullTexture.width;
            Texture2D t2dPreview = null;
            if (ratio > 1f)
                t2dPreview = ScaleTexture(fullTexture, (int)(((int)previewSize) / ratio), (int)previewSize);

            else
                t2dPreview = ScaleTexture(fullTexture, (int)previewSize, (int)((int)previewSize * ratio));
            if (preview != null)
                preview.Release();

            if (ratio > 1f)
                preview = new RenderTexture((int)previewSize, (int)((int)previewSize * ratio), 32);
            else
                preview = new RenderTexture((int)(((int)previewSize) / ratio), (int)((int)previewSize), 32);
            preview.enableRandomWrite = true;
            preview.Create();
            RenderTexture.active = preview;
            Graphics.Blit(t2dPreview, preview);
            if (befPreview != null)
                befPreview.Release();
            if (ratio > 1f)
                befPreview = new RenderTexture((int)(((int)previewSize) / ratio), (int)((int)previewSize), 32);
            else
                befPreview = new RenderTexture((int)previewSize, (int)((int)previewSize * ratio), 32);
            befPreview.enableRandomWrite = true;
            befPreview.Create();
            RenderTexture.active = befPreview;
            Graphics.Blit(preview, befPreview);

        }


        Texture2D CreateOutput() {
            RenderTexture outputRender = new RenderTexture(fullTexture.width, fullTexture.height, 32);
            outputRender.enableRandomWrite = true;
            outputRender.Create();
            RenderTexture.active = outputRender;
            Graphics.Blit(fullTexture, outputRender);

            foreach (var effect in effects) {
                effect.ApplyGPU(ref outputRender, compute);
            }

            Texture2D t = new Texture2D(outputRender.width, outputRender.height, TextureFormat.RGBA32, false);
            RenderTexture.active = outputRender;
            t.ReadPixels(new Rect(0, 0, outputRender.width, outputRender.height), 0, 0);

            t.Apply();

            RenderTexture.active = null;
            outputRender.Release();
            return t;


        }






        Vector2 scrollPos;
        private void OnGUI() {
            if (!init)
                Init();

            GUI.skin.button.wordWrap = true;
            bool updateFlag = false;
            scrollPos = GUILayout.BeginScrollView(scrollPos, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);

            tex = EditorGUILayout.ObjectField("Input image", tex, typeof(Texture2D), false) as Texture2D;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            previewSize = (PreviewSize)EditorGUILayout.EnumPopup(previewSize);

            if (preview != null) {
                float ratio = (float)preview.width / (float)preview.height;
                int w = preview.width;
                int h = preview.height;
                int padding = 10;
                int verticalOffset = 115;
                float windowRatio = position.width / preview.width;

                if (w > h) {
                    w = (int)(position.width - padding * 2);
                    h = (int)(windowRatio * preview.height);
                } else {
                    h = (int)(position.width - padding * 2);
                    w = (int)(h * ratio);
                }

                GUI.DrawTexture(new Rect(padding, verticalOffset, w, h), preview);
                if (ratio < 1.1f)
                    GUILayout.Space(position.width - padding * 2);
                else
                    GUILayout.Space(windowRatio * preview.height);
                GUILayout.Space(padding);
            }



            if (enablePreview) {
                if (GUILayout.Button("See the original image")) {
                    updateFlag = true;
                    enablePreview = !enablePreview;
                }
            } else {
                var b = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("See the modified image")) {
                    updateFlag = true;
                    enablePreview = !enablePreview;
                }
                GUI.backgroundColor = b;

            }
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            for (int i = 0; i < effects.Count; i++) {
                if (effects[i].OnGUI(position, false, i == effects.Count - 1))
                    updateFlag = true;
            }


            string assetPath = AssetDatabase.GetAssetPath(tex);
            string[] parts = assetPath.Split('.');
            string newPath = parts[0];
            foreach (var item in effects) {
                newPath += item.ToString();
            }
            GUILayout.Label("Quick save name: " + newPath, EditorStyles.wordWrappedLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Width(50))) {
                foreach (var item in effects) {
                    item.magnitude = 0f;
                    item.value = false;
                    updateFlag = true;
                }
            }
            if (GUILayout.Button("Quick save as JPEG")) {
                bool checkDiff = false;
                foreach (var item in effects) {
                    if (item.Altered())
                        checkDiff = true;
                }
                if (checkDiff) {

                    SaveTextureAsJPG(CreateOutput(), newPath);
                    AssetDatabase.Refresh();
                    LoadImage();
                    updateFlag = true;
                } else {
                    Debug.Log("Nothing to save.");
                }
            }
            if (GUILayout.Button("Quick save as PNG")) {
                bool checkDiff = false;
                foreach (var item in effects) {
                    if (item.Altered())
                        checkDiff = true;
                }
                if (checkDiff) {
                    SaveTextureAsPNG(CreateOutput(), newPath);
                    AssetDatabase.Refresh();
                    LoadImage();
                    updateFlag = true;
                } else {
                    Debug.Log("Nothing to save.");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();
            saveFormat = (SaveFormat)EditorGUILayout.EnumPopup(saveFormat, GUILayout.Width(50));

            if (GUILayout.Button("Save elsewhere ...") && tex != null) {
                string currentPath = AssetDatabase.GetAssetPath(tex);
                string name = Path.GetFileName(currentPath);
                currentPath = Path.GetDirectoryName(currentPath);
                if (saveFormat == SaveFormat._JPEG) {
                    string path = EditorUtility.SaveFilePanel("Save image", currentPath, name, "jpeg");
                    if (path != "") {
                        SaveTextureAsJPG(CreateOutput(), path, true);

                        AssetDatabase.Refresh();
                        LoadImage();
                        updateFlag = true;
                    }
                } else {
                    string path = EditorUtility.SaveFilePanel("Save image", currentPath, name, "png");
                    if (path != "") {
                        SaveTextureAsPNG(CreateOutput(), path, true);

                        AssetDatabase.Refresh();
                        LoadImage();
                        updateFlag = true;
                    }
                }


            }
            if (GUILayout.Button("Open input folder") && tex != null) {
                string currentPath = AssetDatabase.GetAssetPath(tex);
                System.Diagnostics.Process.Start("explorer.exe", "/select," + Path.GetFullPath(currentPath));
            }
            GUILayout.EndHorizontal();


            if (updateFlag) {
                PreviewUpdate();
            }


            EditorGUILayout.EndScrollView();
        }




        private void OnInspectorUpdate() {

            if (tex != null && fullTexture == null) {
                LoadImage();
            }
        }

        public static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight) {
            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] rpixels = result.GetPixels(0);
            float incX = ((float)1 / source.width) * ((float)source.width / targetWidth);
            float incY = ((float)1 / source.height) * ((float)source.height / targetHeight);
            for (int px = 0; px < rpixels.Length; px++) {
                rpixels[px] = source.GetPixelBilinear(incX * ((float)px % targetWidth),
                                  incY * ((float)Mathf.Floor(px / targetWidth)));
            }
            result.SetPixels(rpixels, 0);
            result.Apply();
            return result;
        }

        [MenuItem("Window/Easy Texture Editor/Quick Editor")]
        public static void ShowWindow() {
            if (!init) {
                Init();
            }
            GetWindow(typeof(QuickEditor), false, "Quick Editor");
        }

        public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath, bool extInPath = false) {
            byte[] _bytes = _texture.EncodeToPNG();
            File.WriteAllBytes(_fullPath + (extInPath ? "" : ".png"), _bytes);
            Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath + (extInPath ? "" : ".png"));
        }

        public static void SaveTextureAsJPG(Texture2D _texture, string _fullPath, bool extInPath = false) {
            byte[] _bytes = _texture.EncodeToJPG(90);
            File.WriteAllBytes(_fullPath + (extInPath ? "" : ".jpeg"), _bytes);
            Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath + (extInPath ? "" : ".jpeg"));
        }
    }





}
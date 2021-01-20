using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using EasyTextureEditor;

namespace EasyTextureEditor {


    public abstract class ImageEffect {

        public float magnitude = 0;
        public bool value = false;
        public abstract void Apply(ref Color[] clrs, int begin, int end);

        public abstract void ApplyGPU(ref RenderTexture rt, ComputeShader compute);

        public bool Altered() {
            return magnitude != 0 || value;
        }
        public abstract bool OnGUI(Rect position, bool overscore, bool underscore);

        public abstract override string ToString();



    }



    public class IEReplace : ImageEffect {

        public bool diff = false;
        public bool enabled = false;
        public Color clr2 = Color.white;
        public Color clr1 = Color.white;

        public override string ToString() {
            if (magnitude != 0)
                return "_RE" + (int)magnitude;
            else
                return "";
        }

        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setDiff(bool val) {
            if (val != diff) {
                diff = val;
                return true;
            }
            return false;
        }

        bool setEnable(bool val) {
            if (val != enabled) {
                enabled = val;
                clr2 = Color.white;
                clr1 = Color.white;
                this.magnitude = 0;
                diff = false;
                return true;
            }
            return false;
        }

        bool setColor1(Color val) {
            //if (val != clr2) {
            if (val != clr1) {
                clr1 = val;
                return true;
            }
            return false;
        }
        bool setColor2(Color val) {
            if (val != clr2) {
                clr2 = val;
                return true;
            }
            return false;
        }

        public IEReplace(bool iClamp = false) { }

        public IEReplace(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {

        }


        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {

            if (!enabled)
                return;


            float adjustedMagnitude = magnitude / 100f;

            int kernel = compute.FindKernel("ETERep");

            compute.SetFloat("magRep", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.SetVector("repClr1", clr1);
            compute.SetVector("repClr2", clr2);
            compute.SetFloat("repDiff", diff ? 0f : 1f);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);






        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setEnable(EditorGUILayout.Toggle("Replace Color", enabled)))
                updateFlag = true;

            if (!enabled)
                return updateFlag;



            if (setColor1(EditorGUILayout.ColorField("Color 1", clr1)))
                updateFlag = true;

            if (setColor2(EditorGUILayout.ColorField("Color 2", clr2)))
                updateFlag = true;


            if (setMagnitude(EditorGUILayout.Slider("Opacity", magnitude, 0f, 100f)))
                updateFlag = true;

            if (setDiff(EditorGUILayout.Toggle("Keep the difference ", diff)))
                updateFlag = true;



            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);



            return updateFlag;
        }
    }
    public class IEBrightness : ImageEffect {



        public override string ToString() {
            if (magnitude != 0)
                return "_BR" + (int)magnitude;
            else
                return "";
        }

        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setClamp(bool val) {
            if (val != value) {
                value = val;
                return true;
            }
            return false;
        }

        public IEBrightness(bool iClamp = false) { }

        public IEBrightness(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {
            if (magnitude == 0)
                return;
            float adjustedMagnitude = magnitude / 50f;
            for (int i = begin; i < end; i++) {
                clrs[i].r = clrs[i].r * (1 + adjustedMagnitude);
                clrs[i].g = clrs[i].g * (1 + adjustedMagnitude);
                clrs[i].b = clrs[i].b * (1 + adjustedMagnitude);
            }

        }
        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {

            float adjustedMagnitude = magnitude / 50f;

            int kernel = compute.FindKernel("ETEBrightness");
            compute.SetFloat("magBrightness", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);



        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;

            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setMagnitude(EditorGUILayout.Slider("Brightness", magnitude, -100f, 100f)))
                updateFlag = true;

            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }

    public class IESaturation : ImageEffect {


        public override string ToString() {
            if (magnitude != 0)
                return "_ST" + (int)magnitude;
            else
                return "";
        }

        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setClamp(bool val) {
            if (val != value) {
                value = val;
                return true;
            }
            return false;
        }

        public IESaturation(bool iClamp = false) { }

        public IESaturation(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {
            if (magnitude == 0)
                return;


        }
        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {

            float adjustedMagnitude = (50 + magnitude) / 50f;

            int kernel = compute.FindKernel("ETESaturation");
            compute.SetFloat("magSaturation", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);



        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;

            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setMagnitude(EditorGUILayout.Slider("Saturation", magnitude, -100f, 100f)))
                updateFlag = true;

            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }

    public class IEExposure : ImageEffect {



        public override string ToString() {
            if (magnitude != 0)
                return "_EX" + (int)magnitude;
            else
                return "";
        }

        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setClamp(bool val) {
            if (val != value) {
                value = val;
                return true;
            }
            return false;
        }

        public IEExposure(bool iClamp = false) { }

        public IEExposure(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {
            if (magnitude == 0)
                return;
            float adjustedMagnitude = (magnitude) / 25f;
            for (int i = begin; i < end; i++) {

                clrs[i].r = clrs[i].r * Mathf.Pow(2, adjustedMagnitude);
                clrs[i].g = clrs[i].g * Mathf.Pow(2, adjustedMagnitude);
                clrs[i].b = clrs[i].b * Mathf.Pow(2, adjustedMagnitude);

            }


        }

        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {

            float adjustedMagnitude = (magnitude) / 25f;

            int kernel = compute.FindKernel("ETEExposure");
            compute.SetFloat("magExposure", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);

        }
        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;
            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setMagnitude(EditorGUILayout.Slider("Exposure", magnitude, -100f, 100f)))
                updateFlag = true;

            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }

    public class IEGAmme : ImageEffect {




        public override string ToString() {
            if (magnitude != 0)
                return "_GM" + (int)magnitude;
            else
                return "";
        }

        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setClamp(bool val) {
            if (val != value) {
                value = val;
                return true;
            }
            return false;
        }

        public IEGAmme(bool iClamp = false) { }

        public IEGAmme(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {
            if (magnitude == 0)
                return;

            float adjustedMagnitude = (100 + magnitude) / 100f;
            float g = 1f / adjustedMagnitude;
            for (int i = begin; i < end; i++) {

                clrs[i].r = Mathf.Pow(clrs[i].r, g);
                clrs[i].g = Mathf.Pow(clrs[i].g, g);
                clrs[i].b = Mathf.Pow(clrs[i].b, g);
            }


        }
        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {

            float adjustedMagnitude = (100 + magnitude) / 100f;
            float g = 1f / adjustedMagnitude;

            int kernel = compute.FindKernel("ETEGamma");
            compute.SetFloat("magGamma", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);

        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;
            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setMagnitude(EditorGUILayout.Slider("Gamma", magnitude, -100f, 100f)))
                updateFlag = true;

            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }


    public class IEHue : ImageEffect {




        public override string ToString() {
            if (magnitude != 0)
                return "_HU" + (int)magnitude;
            else
                return "";
        }

        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setClamp(bool val) {
            if (val != value) {
                value = val;
                return true;
            }
            return false;
        }

        public IEHue(bool iClamp = false) { }

        public IEHue(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {


        }
        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {

            float adjustedMagnitude = magnitude * -1f;


            int kernel = compute.FindKernel("ETEHue");
            compute.SetFloat("magHue", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);

        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;
            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setMagnitude(EditorGUILayout.Slider("Hue", magnitude, -180f, 180f)))
                updateFlag = true;

            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }


    public class IEContrast : ImageEffect {



        public override string ToString() {
            if (magnitude != 0)
                return "_CO" + (int)magnitude;
            else
                return "";
        }
        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setClamp(bool val) {
            if (val != value) {
                value = val;
                return true;
            }
            return false;
        }

        public IEContrast(bool iClamp = false) { }

        public IEContrast(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {
            if (magnitude == 0)
                return;
            float adjustedMagnitude = magnitude / 30f;

            adjustedMagnitude = (100.0f + adjustedMagnitude) / 100.0f;
            adjustedMagnitude *= adjustedMagnitude;

            for (int i = begin; i < end; i++) {

                Color c = clrs[i];

                float pR = c.r;
                pR -= 0.5f;
                pR *= adjustedMagnitude;
                pR += 0.5f;

                float pB = c.b;
                pB -= 0.5f;
                pB *= adjustedMagnitude;
                pB += 0.5f;

                float pG = c.g;
                pG -= 0.5f;
                pG *= adjustedMagnitude;
                pG += 0.5f;
                if (value) {
                    pG = Mathf.Clamp01(pG);
                    pB = Mathf.Clamp01(pB);
                    pR = Mathf.Clamp01(pR);
                }
                clrs[i] = new Color(pR, pG, pB, clrs[i].a);
            }

        }

        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {

            float adjustedMagnitude = magnitude / 10f;
            adjustedMagnitude = (100.0f + adjustedMagnitude) / 100.0f;
            adjustedMagnitude *= adjustedMagnitude;


            int kernel = compute.FindKernel("ETEContrast");
            compute.SetFloat("magContrast", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);

        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;

            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setMagnitude(EditorGUILayout.Slider("Contrast", magnitude, -100f, 100f)))
                updateFlag = true;

            if (position.width > 170) {
                GUILayout.BeginHorizontal();
                GUILayout.Space(position.width - 170);
                GUILayout.EndHorizontal();

            }

            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }


    public class IEInvert : ImageEffect {


        bool setValue(bool val) {
            if (value != val) {
                value = val;
                return true;
            }
            return false;
        }

        public override string ToString() {
            if (value)
                return "_IN";
            else
                return "";
        }

        public IEInvert(bool val = false) {
            value = val;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {
            if (value)
                for (int i = begin; i < end; i++) {
                    clrs[i].r = 1f - clrs[i].r;
                    clrs[i].g = 1f - clrs[i].g;
                    clrs[i].b = 1f - clrs[i].b;
                }

        }

        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {
            if (!value)
                return;

            int kernel = compute.FindKernel("ETEInvert");
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);
        }
        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {
            bool updateFlag = false;

            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setValue(EditorGUILayout.Toggle("Invert ", value)))
                updateFlag = true;


            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }

    public class IEGrayscale : ImageEffect {


        bool setValue(bool val) {
            if (value != val) {
                value = val;
                return true;
            }
            return false;
        }

        public override string ToString() {
            if (value)
                return "_GR";
            else
                return "";
        }

        public IEGrayscale(bool val = false) {
            value = val;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {
            if (value)
                for (int i = begin; i < end; i++) {
                    float v = clrs[i].r * 0.59f + clrs[i].g * 0.3f + clrs[i].b * 0.11f;

                    clrs[i].r = v;
                    clrs[i].g = v;
                    clrs[i].b = v;
                }

        }
        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {
            if (!value)
                return;
            int kernel = compute.FindKernel("ETEGrayscale");
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);
        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {
            bool updateFlag = false;

            if (overscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (setValue(EditorGUILayout.Toggle("Grayscale ", value)))
                updateFlag = true;


            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            return updateFlag;
        }


    }

    public enum BlendingWith {

        color = 0, texture = 1, myself = 2
    }
    public enum BlendMode {

        Multiply, LinearDodge_Add, Divide, Subtract, Normal, Darken, Lighten, Screen, Dissolve, ColorBurn, LinearBurn, DarkerColor, LighterColor, ColorDodge, Overlay, HardLight, SoftLight, VividLight, LinearLight, PinLight, HardMix, Difference, Exclusion
    }

    public class IEBlending : ImageEffect {

        public BlendingWith with = BlendingWith.color;
        public BlendMode mode = BlendMode.Multiply;
        public Texture2D oTex;
        public Color _clr;
        public bool enabled = false;
        RenderTexture withData = null;
        public Color clr {
            get { return _clr; }
            set {
                if (value != _clr)
                    withData = null;
                _clr = value;

            }
        }

        public override string ToString() {
            if (magnitude != 0)
                return "_BL" + (int)magnitude;
            else
                return "";
        }

        bool setEnabled(bool val) {
            if (val != enabled) {
                enabled = val;
                _clr = Color.white;
                oTex = null;
                withData = null;
                this.magnitude = 0;
                with = BlendingWith.color;
                mode = BlendMode.Multiply;

                return true;
            }
            return false;
        }
        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                // withData = null;
                return true;
            }
            return false;
        }

        bool setWith(BlendingWith val) {
            if (val != with) {
                with = val;
                withData = null;
                return true;
            }
            return false;
        }
        bool setMode(BlendMode val) {
            if (val != mode) {
                mode = val;
                return true;
            }
            return false;
        }

        bool setColor(Color val) {
            if (val != clr) {
                clr = val;
                return true;
            }
            return false;
        }

        bool setTex(Texture2D val) {
            if (val != oTex) {
                oTex = val;
                withData = null;
                return true;
            }
            return false;
        }
        public IEBlending(bool iClamp = false) { }

        public IEBlending(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {

        }

        RenderTexture getWithData(ref RenderTexture rt) {
            if (withData == null) {
                Texture2D tmpTex = null;
                withData = new RenderTexture(rt.width, rt.height, 32);
                withData.enableRandomWrite = true;


                tmpTex = new Texture2D(rt.width, rt.height);
                for (int y = 0; y < rt.height; y++) {
                    for (int x = 0; x < rt.width; x++) {
                        tmpTex.SetPixel(x, y, clr);
                    }
                }
                tmpTex.Apply();
                Graphics.Blit(tmpTex, withData);

            }
            return withData;
        }



        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {


            if (!enabled)
                return;

            float adjustedMagnitude = magnitude / 100f;

            int kernel = 0;

            if (withData != null)
                if (rt.width != withData.width || rt.height != withData.height)
                    withData = null;



            switch (mode) {
                case BlendMode.Multiply:
                    kernel = compute.FindKernel("ETEBlendMul");
                    break;
                case BlendMode.LinearDodge_Add:
                    kernel = compute.FindKernel("ETEBlendAdd");
                    break;
                case BlendMode.Divide:
                    kernel = compute.FindKernel("ETEBlendDiv");
                    break;
                case BlendMode.Subtract:
                    kernel = compute.FindKernel("ETEBlendSub");
                    break;
                case BlendMode.Normal:
                    kernel = compute.FindKernel("ETEBlendNor");
                    break;
                case BlendMode.Darken:
                    kernel = compute.FindKernel("ETEBlendDar");
                    break;
                case BlendMode.Lighten:
                    kernel = compute.FindKernel("ETEBlendLig");
                    break;
                case BlendMode.Screen:
                    kernel = compute.FindKernel("ETEBlendScr");
                    break;
                case BlendMode.Dissolve:
                    kernel = compute.FindKernel("ETEBlendDis");
                    break;
                case BlendMode.ColorBurn:
                    kernel = compute.FindKernel("ETEBlendCBu");
                    break;
                case BlendMode.LinearBurn:
                    kernel = compute.FindKernel("ETEBlendLBu");
                    break;
                case BlendMode.DarkerColor:
                    kernel = compute.FindKernel("ETEBlendDCo");
                    break;
                case BlendMode.LighterColor:
                    kernel = compute.FindKernel("ETEBlendLCo");
                    break;
                case BlendMode.ColorDodge:
                    kernel = compute.FindKernel("ETEBlendCDo");
                    break;
                case BlendMode.Overlay:
                    kernel = compute.FindKernel("ETEBlendOve");
                    break;
                case BlendMode.HardLight:
                    kernel = compute.FindKernel("ETEBlendHLi");
                    break;
                case BlendMode.SoftLight:
                    kernel = compute.FindKernel("ETEBlendSLi");
                    break;
                case BlendMode.VividLight:
                    kernel = compute.FindKernel("ETEBlendVLi");
                    break;
                case BlendMode.LinearLight:
                    kernel = compute.FindKernel("ETEBlendLLi");
                    break;
                case BlendMode.PinLight:
                    kernel = compute.FindKernel("ETEBlendPLi");
                    break;
                case BlendMode.HardMix:
                    kernel = compute.FindKernel("ETEBlendHMi");
                    break;
                case BlendMode.Difference:
                    kernel = compute.FindKernel("ETEBlendDif");
                    break;
                case BlendMode.Exclusion:
                    kernel = compute.FindKernel("ETEBlendExc");
                    break;
                default:
                    break;
            }




            switch (with) {
                case BlendingWith.color:


                    compute.SetFloat("magBlend", adjustedMagnitude);
                    compute.SetTexture(kernel, "data", rt);
                    compute.SetTexture(kernel, "dataBlend", getWithData(ref rt));

                    break;
                case BlendingWith.texture:

                    if (oTex == null)
                        return;

                    if (withData == null) {
                        string path = AssetDatabase.GetAssetPath(oTex);
                        Texture2D t = null;
                        if (Path.GetExtension(path) == ".tga") {
                            t = TGALoader.LoadTGA(path);
                        } else {
                            byte[] texData = File.ReadAllBytes(path);
                            t = new Texture2D(2, 2);
                            ImageConversion.LoadImage(t, texData);
                        }
                        t = QuickEditor.ScaleTexture(t, rt.width, rt.height);

                        withData = new RenderTexture(t.width, t.height, rt.depth);
                        withData.enableRandomWrite = true;
                        Graphics.Blit(t, withData);

                    }


                    compute.SetFloat("magBlend", adjustedMagnitude);
                    compute.SetTexture(kernel, "data", rt);
                    compute.SetTexture(kernel, "dataBlend", withData);

                    break;
                case BlendingWith.myself:
                    compute.SetFloat("magBlend", adjustedMagnitude);
                    compute.SetTexture(kernel, "data", rt);
                    withData = new RenderTexture(rt.width, rt.height, rt.depth);
                    withData.enableRandomWrite = true;
                    Graphics.Blit(rt, withData);

                    compute.SetTexture(kernel, "dataBlend", withData);
                    //wtf.Release();
                    break;
            }



            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);


        }

        void NextWith() {
            switch (with) {
                case BlendingWith.color:
                    with = BlendingWith.texture;
                    break;
                case BlendingWith.texture:
                    with = BlendingWith.myself;
                    break;
                case BlendingWith.myself:
                    with = BlendingWith.myself;
                    break;
                default:
                    break;
            }

        }
        void LastWith() {
            switch (with) {
                case BlendingWith.color:
                    with = BlendingWith.color;
                    break;
                case BlendingWith.texture:
                    with = BlendingWith.color;
                    break;
                case BlendingWith.myself:
                    with = BlendingWith.texture;
                    break;
                default:
                    break;
            }

        }

        void LastMode() {

            mode = (BlendMode)((int)mode - 1);
            if ((int)mode < 0)
                mode = (BlendMode)0;


        }

        void NextMode() {

            mode = (BlendMode)((int)mode + 1);
            if ((int)mode > 22)
                mode = (BlendMode)22;


        }


        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {

            bool updateFlag = false;
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


            if (setEnabled(EditorGUILayout.Toggle("Enable Blending", enabled)))
                updateFlag = true;

            if (!enabled)
                return updateFlag;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(25))) {
                LastWith();
                updateFlag = true;
            }
            if (setWith((BlendingWith)EditorGUILayout.EnumPopup("Blend with: ", with)))
                updateFlag = true;

            if (GUILayout.Button(">", GUILayout.Width(25))) {
                NextWith();
                updateFlag = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(25))) {
                LastMode();
                updateFlag = true;
            }

            if (setMode((BlendMode)EditorGUILayout.EnumPopup("Blend mode: ", mode)))
                updateFlag = true;

            if (GUILayout.Button(">", GUILayout.Width(25))) {
                NextMode();
                updateFlag = true;
            }
            EditorGUILayout.EndHorizontal();

            if (with == BlendingWith.color)
                if (setColor(EditorGUILayout.ColorField("Color", clr)))
                    updateFlag = true;

            if (with == BlendingWith.texture)
                if (setTex(EditorGUILayout.ObjectField("Input texture", oTex, typeof(Texture2D), false) as Texture2D))
                    updateFlag = true;

            if (setMagnitude(EditorGUILayout.Slider("Opacity", magnitude, 0f, 100f)))
                updateFlag = true;




            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);



            return updateFlag;
        }


    }
    public enum DistanceCalculation {

        color = 0, texture = 1, myself = 2
    }


    public class IEReplaceColor : ImageEffect {

        public Color clr = new Color(1, 1, 1, 1);
        public DistanceCalculation distance = DistanceCalculation.color;

        public override string ToString() {
            if (magnitude != 0)
                return "_CR" + (int)magnitude;
            else
                return "";
        }

        bool setMagnitude(float val) {
            if (val != magnitude) {
                magnitude = val;
                return true;
            }
            return false;
        }

        bool setDistance(DistanceCalculation val) {
            if (val != distance) {
                distance = val;
                return true;
            }
            return false;
        }
        bool setColor(Color val) {
            if (val != clr) {
                clr = val;
                return true;
            }
            return false;
        }

        public IEReplaceColor(bool iClamp = false) { }

        public IEReplaceColor(float iMagnitude, bool iClamp = false) {
            magnitude = iMagnitude;
            value = iClamp;
        }

        public override void Apply(ref Color[] clrs, int begin, int end) {

        }


        public override void ApplyGPU(ref RenderTexture rt, ComputeShader compute) {
            /*
            float adjustedMagnitude = magnitude / 50f;

            int kernel = compute.FindKernel("ETEBrightness");
            compute.SetFloat("magBrightness", adjustedMagnitude);
            compute.SetTexture(kernel, "data", rt);
            compute.Dispatch(kernel, rt.width / 8 + rt.width % 8, rt.height / 8 + rt.height % 8, 1);
            */


        }

        public override bool OnGUI(Rect position, bool overscore = true, bool underscore = true) {
            bool updateFlag = false;
            /*
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            if (setWith((BlendingWith)EditorGUILayout.EnumPopup("Blend with: ", with)))
                updateFlag = true;

            // if (overscore)

            if (with == BlendingWith.color)
                if (setColor(EditorGUILayout.ColorField("Color", clr)))
                    updateFlag = true;

            if (with == BlendingWith.texture)
                if (setTex(EditorGUILayout.ObjectField("Input texture", oTex, typeof(Texture2D), false) as Texture2D))
                    updateFlag = true;

            if (setMagnitude(EditorGUILayout.Slider("Opacity", magnitude, 0f, 100f)))
                updateFlag = true;




            if (underscore)
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        */

            return updateFlag;
        }


    }

}
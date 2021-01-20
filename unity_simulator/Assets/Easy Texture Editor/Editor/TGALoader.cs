using System;
using System.IO;
using UnityEngine;
using EasyTextureEditor;

namespace EasyTextureEditor {
    public static class TGALoader {

        public static Texture2D LoadTGA(string fileName) {
            using (var imageFile = File.OpenRead(fileName)) {
                return LoadTGA(imageFile);
            }
        }

        public static Texture2D LoadTGA(Stream TGAStream) {

            using (BinaryReader r = new BinaryReader(TGAStream)) {
                r.ReadByte();
                r.ReadByte();
                if (r.ReadByte() == 2){
                    for (int i = 0; i < 9; i++) 
                    r.ReadByte();

                    short width = r.ReadInt16();
                    short height = r.ReadInt16();
                    int bitDepth = r.ReadByte();

                    r.BaseStream.Seek(1, SeekOrigin.Current);

                    Texture2D tex = new Texture2D(width, height);
                    Color32[] pulledColors = new Color32[width * height];

                    if (bitDepth == 32) {
                        for (int i = 0; i < width * height; i++) {
                            byte red = r.ReadByte();
                            byte green = r.ReadByte();
                            byte blue = r.ReadByte();
                            byte alpha = r.ReadByte();

                            pulledColors[i] = new Color32(blue, green, red, alpha);
                        }
                    } else if (bitDepth == 24) {
                        for (int i = 0; i < width * height; i++) {
                            byte red = r.ReadByte();
                            byte green = r.ReadByte();
                            byte blue = r.ReadByte();

                            pulledColors[i] = new Color32(blue, green, red, 255);
                        }
                    } else if (bitDepth == 8) {

                        for (int i = 0; i < width * height; i++) {
                            byte red = r.ReadByte();
                            byte green = red;
                            byte blue = green;

                            pulledColors[i] = new Color32(blue, green, red, 255);
                        }


                    } else {
                        throw new Exception("TGA texture had weird bit depth.");
                    }

                    tex.SetPixels32(pulledColors);
                    tex.Apply();
                    return tex;
                } else {
                    for (int i = 0; i < 9; i++)
                        r.ReadByte();


                    short width = r.ReadInt16();
                    short height = r.ReadInt16();
                    int bitDepth = r.ReadByte();

                    r.BaseStream.Seek(1, SeekOrigin.Current);

                    Texture2D tex = new Texture2D(width, height);
                    Color32[] pulledColors = new Color32[width * height];
                    int p = 0;

                    do {
                        int header = r.ReadByte();
                        if (header < 128) {
                            header++;

                            for (int i = 0; i < header; i++) {

                                byte red, green, blue, alpha;

                                red = r.ReadByte();
                                if (bitDepth > 8) {
                                    green = r.ReadByte();
                                    blue = r.ReadByte();
                                    if (bitDepth > 24)
                                        alpha = r.ReadByte();
                                    else
                                        alpha = 255;
                                } else {
                                    green = red;
                                    blue = red;
                                    alpha = 255;
                                }

                                pulledColors[p++] = new Color32(red, green, blue, alpha);

                            }
                        } else {
                            header -= 127; 
                            byte red, green, blue, alpha;

                            red = r.ReadByte();
                            if (bitDepth > 8) {
                                green = r.ReadByte();
                                blue = r.ReadByte();
                                if (bitDepth > 24)
                                    alpha = r.ReadByte();
                                else
                                    alpha = 255;
                            } else {
                                green = red;
                                blue = red;
                                alpha = 255;
                            }
                            for (int i = 0; i < header; i++) {
                                pulledColors[p++] = new Color32(red, green, blue, alpha);
                            }

                        }

                    } while (p != width*height);


                    tex.SetPixels32(pulledColors);
                    tex.Apply();
                    return tex;

                }
            }
        }

        public static void GetSize(string fileName, ref int x, ref int y) {
            using (BinaryReader r = new BinaryReader(File.OpenRead(fileName))) {
                r.BaseStream.Seek(12, SeekOrigin.Begin);

                x = r.ReadInt16();
                y = r.ReadInt16();



            }
        }


    }
}
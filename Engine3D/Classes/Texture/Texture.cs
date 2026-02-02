using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using System.Runtime.CompilerServices;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Engine3D
{
    public enum TextureType
    {
        Texture,
        Normal,
        Height,
        AO,
        Rough,
        Metal
    }

    public class Texture
    {
        public string textureName
        {
            get
            {
                return Path.GetFileName(TexturePath);
            }
            set
            {
                string texturePath = value;
                if (texturePath == "")
                {
                    throw new NotImplementedException();
                    //Engine.textureManager.DeleteTexture(this, mesh, "");
                    //texture = null;
                }
                else
                {
                    Engine.textureManager.DeleteTexture(this);
                    Texture? t = Engine.textureManager.AddTexture(texturePath, out bool success);
                    if(t != null)
                    {
                        Copy(t);
                    }
                    else
                        Engine.consoleManager.AddLog("Texture: " + texturePath + "was not found!", LogType.Warning);
                }
            }
        }

        public TextureMinFilter tminf;
        public TextureMagFilter tmagf;

        public string TextureName;
        public string TexturePath;
        public int TextureId;
        public int TextureUnit;

        public bool isBinded = false;

        public bool flipY;

        public bool UITexture = false;

        public Texture(int unit, string texturePath, out bool success, bool flipY = true, string textureFilter = "linear", AssetTypeEditor type = AssetTypeEditor.UI)
        {
            TextureName = Path.GetFileName(texturePath);
            UITexture = true;

            if (texturePath == TextureName)
            {
                if (type == AssetTypeEditor.UI)
                    TexturePath = FileManager.GetFilePath(texturePath, "Textures");
                else if (type == AssetTypeEditor.Store)
                    TexturePath = FileManager.GetFilePath(texturePath, "Temp");
            }
            else
                TexturePath = texturePath;

            if (TexturePath == "" || TexturePath == null)
                throw new Exception("Texture not found!");

            int currentUnit = unit;

            TextureId = GL.GenTexture();
            TextureUnit = currentUnit;

            this.flipY = flipY;

            if (textureFilter == "linear")
            {
                tminf = TextureMinFilter.Linear;
                tmagf = TextureMagFilter.Linear;
            }
            else if(textureFilter == "nearest")
            {
                tminf = TextureMinFilter.Nearest;
                tmagf = TextureMagFilter.Nearest;
            }
            else
            {
                tminf = TextureMinFilter.Linear;
                tmagf = TextureMagFilter.Linear;
            }

            Bind();

            if(type == AssetTypeEditor.Store)
                success = LoadTexture(TexturePath, flipY, tminf, tmagf, "Temp");
            else
                success = LoadTexture(TexturePath, flipY, tminf, tmagf);

            Unbind();
        }

        public Texture(int unit)
        {
            int currentUnit = unit;

            TextureId = GL.GenTexture();
            TextureUnit = currentUnit;
        }

        public static bool LoadTexture(string filePath, bool flipY, TextureMinFilter tminf, TextureMagFilter tmagf, string folder = "Textures", bool downscale=false)
        {
            Stream? stream_t = FileManager.GetFileStream(filePath);
            if (stream_t != Stream.Null && stream_t != null)
            {
                using (stream_t)
                {
                    using Image<Rgba32> image = Image.Load<Rgba32>(stream_t);
                    if (flipY)
                        image.Mutate(x => x.Flip(FlipMode.Vertical));

                    if (downscale)
                        image.Mutate(x => x.Resize(128, 128));

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)tminf);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)tmagf);

                    byte[] data;
                    if (image.DangerousTryGetSinglePixelMemory(out var memory))
                    {
                        ReadOnlySpan<byte> pixelBytes = MemoryMarshal.AsBytes(memory.Span);
                        data = pixelBytes.ToArray();
                    }
                    else
                    {
                        data = new byte[image.Width * image.Height * 4];
                        image.CopyPixelDataTo(data);
                    }

                    GL.TexImage2D(
                        TextureTarget.Texture2D,
                        0,
                        PixelInternalFormat.Rgba,
                        image.Width,
                        image.Height,
                        0,
                        OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                        PixelType.UnsignedByte,
                        data);
                }
                return true;
            }
            else
            {
                Engine.consoleManager.AddLog("Texture: " + filePath + "was not found!", LogType.Warning);
                return false;
            }
        }

        public void Copy(Texture t)
        {
            tminf = t.tminf;
            tmagf = t.tmagf;

            TextureName = t.TextureName;
            TexturePath = t.TexturePath;
            TextureId = t.TextureId;
            TextureUnit = t.TextureUnit;

            isBinded = t.isBinded;

            flipY = t.flipY;
        }

        public void Bind() 
        {
            GL.ActiveTexture(OpenTK.Graphics.OpenGL4.TextureUnit.Texture0 + TextureUnit);
            Engine.GLState.currentTextureUnit = TextureUnit;

            GL.BindTexture(TextureTarget.Texture2D, TextureId);
            Engine.GLState.currentTextureId = TextureId;
            isBinded = true;
        }
        public void Unbind()
        {
            Engine.GLState.currentTextureUnit = -1;
            Engine.GLState.currentTextureId = -1;

            GL.BindTexture(TextureTarget.Texture2D, 0);
            isBinded = false;
        }

        public void Delete()
        {
            Engine.GLState.currentTextureUnit = -1;
            Engine.GLState.currentTextureId = -1;

            GL.BindTexture(TextureTarget.Texture2D, 0);
            isBinded = false;
            GL.DeleteTexture(TextureId);
        }

        ~Texture()
        {
            //Delete();
        }

    }
}

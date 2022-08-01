using System;
using System.Numerics;
using ShadowMap.Shared;
using Silk.NET.OpenGL;
using static ShadowMapGL.MainWindow;

namespace ShadowMapGL;

public class Model : IDisposable
{
    private uint _vao;
    private uint _vbo;
    private uint _ebo;

    private Texture2D _texture;
    private Effect _effect;
    private uint _numIndices;

    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    
    public unsafe Model(IPrimitive primitive, Texture2D texture)
    {
        _texture = texture;
        _numIndices = (uint) primitive.Indices.Length;
        
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
        
        _vao = Gl.GenVertexArray();
        Gl.BindVertexArray(_vao);

        _vbo = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (VertexPositionTextureNormal* vptn = primitive.Vertices)
        {
            Gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint) (primitive.Vertices.Length * sizeof(VertexPositionTextureNormal)), vptn, GLEnum.StaticDraw);
        }

        _ebo = Gl.GenBuffer();
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, _ebo);
        fixed (uint* i = primitive.Indices)
        {
            Gl.BufferData(GLEnum.ElementArrayBuffer, (nuint) (primitive.Indices.Length * sizeof(uint)), i,
                GLEnum.StaticDraw);
        }

        _effect = new Effect("Content/Shaders/Model.vert", "Content/Shaders/Model.frag");
        _effect.Use();

        const uint stride = 32;
        
        uint posLoc = _effect.GetAttribLocation("aPosition");
        Gl.EnableVertexAttribArray(posLoc);
        Gl.VertexAttribPointer(posLoc, 3, VertexAttribPointerType.Float, false, stride, (void*) 0);

        uint texLoc = _effect.GetAttribLocation("aTexCoords");
        Gl.EnableVertexAttribArray(texLoc);
        Gl.VertexAttribPointer(texLoc, 2, VertexAttribPointerType.Float, false, stride, (void*) 12);

        uint nmlLoc = _effect.GetAttribLocation("aNormals");
        Gl.EnableVertexAttribArray(nmlLoc);
        Gl.VertexAttribPointer(nmlLoc, 3, VertexAttribPointerType.Float, false, stride, (void*) 20);
        
        Gl.BindVertexArray(0);
        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
    }

    public unsafe void Draw(Matrix4x4 camera, Vector3 lightPos)
    {
        Gl.BindVertexArray(_vao);
        _effect.Use();
        
        // Model & camera matrices
        _effect.SetUniform("uModel",
            Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) *
            Matrix4x4.CreateTranslation(Position));
        _effect.SetUniform("uCamera", camera);
        _effect.SetUniform("uCameraPos", camera.Translation);
        
        // Material
        _effect.SetUniform("uMaterial.albedo", 0);
        _effect.SetUniform("uMaterial.specular", 1);
        _effect.SetUniform("uMaterial.color", Vector4.One);
        _effect.SetUniform("uMaterial.shininess", 32);
        
        // Light
        _effect.SetUniform("uSun.direction", lightPos);
        _effect.SetUniform("uSun.Ambient", new Vector3(0.1f));
        _effect.SetUniform("uSun.diffuse", new Vector3(0.7f));
        _effect.SetUniform("uSun.specular", new Vector3(1.0f));
        
        _texture.Bind(0);
        _texture.Bind(1);

        Gl.DrawElements(PrimitiveType.Triangles, _numIndices, DrawElementsType.UnsignedInt, null);
    }
    
    public void Dispose() { }
}
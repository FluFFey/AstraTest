﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AstraSkeletonParticles : MonoBehaviour
{
    private Texture2D _texture;
    private Color[] _textureBuffer;

    private long _lastFrameIndex = -1;
    private short[] _depthFrameData;
    private const TextureFormat Format = TextureFormat.RGBA32;
    [SerializeField] [Range(1.0f, 50000.0f)] public float particledepthScaler;
    //[SerializeField][Range(100.0f, 10000.0f)] private float depthScaler;
    private float particleTimer = 0.0f;
    [SerializeField] public ParticleSystem ps;

    [Range(0.0f, 5.0f)] public float particleCooldown;

    private int currentChunk = 0;
    [SerializeField] private int particleChunks = 5; //Higher = Better performance because less particles are drawn per update

    private void Start()
    {
        _textureBuffer = new Color[320 * 240];
        _depthFrameData = new short[320 * 240];
        _texture = new Texture2D(320, 240, Format, false);

        GetComponent<Renderer>().material.mainTexture = _texture;
    }

    public void OnNewColorizedBodyFrame(Astra.ColorizedBodyFrame frame)
    {
        if (frame.Width == 0 ||
            frame.Height == 0)
        {
            return;
        }
        //This is default, but I would assume astra only calls "onNewFrame" once fram index has changed in the first place??
        if (_lastFrameIndex == frame.FrameIndex)
        {
            //return;
        }

        _lastFrameIndex = frame.FrameIndex;
        //print("Mask frame x: " + frame.Width + "y: " + frame.Height);
        //EnsureTexture(frame.Width, frame.Height);
        _texture.LoadRawTextureData(frame.DataPtr, (int)frame.ByteLength);
       // _texture.Apply();
    }

    public void OnNewDepthFrame(Astra.DepthFrame depthFrame)
    {
        //TODO: I think this should guarantee that there is data, but should make sure
        if (depthFrame.Width == 0 ||
            depthFrame.Height == 0)
        {
            return;
        }
        //This is default, but I would assume astra only calls "onNewFrame" once fram index has changed in the first place??
        if (_lastFrameIndex == depthFrame.FrameIndex)
        {
            return;
        }
        _lastFrameIndex = depthFrame.FrameIndex;
        //print("Depth frame x: " + depthFrame.Width + "y: " + depthFrame.Height);
        //EnsureBuffers(depthFrame.Width, depthFrame.Height);
        depthFrame.CopyData(ref _depthFrameData);

        //MapParticlesToBody(_depthFrameData);
    }

    public void OnNewPointFrame(Astra.PointFrame pointFrame)
    {
        //TODO: I think this should guarantee that there is data, but should make sure
        if (pointFrame.Width == 0 ||
            pointFrame.Height == 0)
        {
            return;
        }
        //This is default, but I would assume astra only calls "onNewFrame" once fram index has changed in the first place??
        //if (_lastFrameIndex == pointFrame.FrameIndex)
        //{
        //    return;
        //}
        //_lastFrameIndex = depthFrame.FrameIndex;
        //print("Depth frame x: " + depthFrame.Width + "y: " + depthFrame.Height);
        //EnsureBuffers(depthFrame.Width, depthFrame.Height);
        //depthFrame.CopyData(ref _depthFrameData);
        Astra.Vector3D[] pointData = new Astra.Vector3D[pointFrame.Width* pointFrame.Height];
        //Vector3[] pointData = new Vector3;
        pointFrame.CopyData(ref pointData);

        dataToParticles(pointData);
    }

    private void EnsureTexture(int width, int height)
    {
        if (_texture == null)
        {
            _texture = new Texture2D(width, height, Format, false);
            print(width + height);
            //GetComponent<Renderer>().material.mainTexture = _texture;
            return;
        }

        if (_texture.width != width ||
            _texture.height != height)
        {
            _texture.Resize(width, height);
        }
    }

    //TODO: optimize to fit correct buffer size
    private void EnsureBuffers(int width, int height)
    {
        int length = width * height;
        if (_textureBuffer.Length != length)
        {
            _textureBuffer = new Color[length];
        }

        if (_depthFrameData.Length != length)
        {
            _depthFrameData = new short[length];
        }

        if (_texture != null)
        {
            if (_texture.width != width ||
                _texture.height != height)
            {
                _texture.Resize(width, height);
            }
        }
    }
    private void Update()
    {
        particleTimer += Time.deltaTime;
    }
    
    void dataToParticles(Astra.Vector3D[] worldPosData)
    {
        int length = worldPosData.Length;
        Color[] colorArray = _texture.GetPixels();
        ParticleSystem.EmitParams psParams = new ParticleSystem.EmitParams();
        float lz= worldPosData[0].Z;
        float hz= worldPosData[0].Z;
        for (int i = iStart% iStartIncrementer; i < length; i+= iStartIncrementer)
        {
            if (particleTimer > particleCooldown && colorArray[i].a != 0 && i % 7 == 0)
            {
                psParams.position = new Vector3(
                       worldPosData[i].X / 1000.0f,
                       worldPosData[i].Y / 1000.0f,
                       worldPosData[i].Z / 1000.0f);
                float zScaled = 1 - worldPosData[i].Z / particledepthScaler;
                colorArray[i].r *= zScaled;
                colorArray[i].g *= zScaled;
                colorArray[i].b *= zScaled;
                psParams.startColor = colorArray[i];
                
                ps.Emit(psParams, 1);
                if (worldPosData[i].Z > hz)
                    hz = worldPosData[i].Z;
                if (worldPosData[i].Z < lz)
                    lz = worldPosData[i].Z;
                iStart++;
            }
                
        }
        if (particleTimer > particleCooldown)
        {
            particleTimer = 0;
        }
    }
}


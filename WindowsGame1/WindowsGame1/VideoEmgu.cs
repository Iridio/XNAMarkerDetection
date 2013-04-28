using System;
using System.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;

namespace WindowsGame1
{
  public class VideoEmgu
  {
    GraphicsDevice device;
    Texture2D frame;
    Capture capture;
    Image<Bgr, byte> nextFrame;
    ThreadStart thread;
    public bool IsRunning;
    public Color[] colorData;

    private Image<Gray, byte> gray = null;

    public Texture2D Frame
    {
      get
      {
        if (frame.GraphicsDevice.Textures[0] == frame)
          frame.GraphicsDevice.Textures[0] = null;
        frame.SetData<Color>(0, null, colorData, 0, colorData.Length);
        return frame;
      }
    }

    public VideoEmgu(GraphicsDevice device, int camIndex)
    {
      this.device = device;
      capture = new Capture(camIndex);
      frame = new Texture2D(device, capture.Width, capture.Height);
      colorData = new Color[capture.Width * capture.Height];
    }

    public void Start()
    {
      thread = new ThreadStart(QueryFrame);
      IsRunning = true;
      thread.BeginInvoke(null, null);
    }

    public void Dispose()
    {
      IsRunning = false;
      capture.Dispose();
    }

    private void QueryFrame()
    {
      while (IsRunning)
      {
        nextFrame = capture.QueryFrame().Flip(FLIP.HORIZONTAL);
        if (nextFrame != null)
        {
          GetMarker(nextFrame);
          byte[] bgrData = nextFrame.Bytes;
          for (int i = 0; i < colorData.Length; i++)
            colorData[i] = new Color(bgrData[3 * i + 2], bgrData[3 * i + 1], bgrData[3 * i]);
        }
      }
    }

    //MCvFont font = new MCvFont(FONT.CV_FONT_HERSHEY_TRIPLEX, 0.3d, 0.3d);
    private Image<Gray, byte> GetMarker(Image<Bgr, byte> currentFrame)
    {
      var markerImagesList = new List<Image<Gray, byte>>();
      var filteredMarkerImagesList = new List<Image<Gray, byte>>();
      var markerBoxList = new List<MCvBox2D>();
      MarkerDetector.DetectMarker(currentFrame, markerImagesList, filteredMarkerImagesList, markerBoxList);
      if (markerBoxList.Count > 0)
      {
        var box = markerBoxList[0];
        currentFrame.Draw(box, new Bgr(System.Drawing.Color.Red), 2);
        //currentFrame.Draw("X: " + box.size.Width, ref font, new System.Drawing.Point((int)box.center.X, (int)box.center.Y), new Bgr(System.Drawing.Color.Blue));
      }
      return null;
    }
  }
}


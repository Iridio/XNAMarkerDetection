using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace WindowsGame1
{
  public class MarkerDetector : DisposableObject
  {
    public static List<String> DetectMarker(Image<Bgr, byte> img, List<Image<Gray, Byte>> markerImagesList, List<Image<Gray, Byte>> filteredMarkerImagesList, List<MCvBox2D> detectedMarkerRegionList)
    {
      var markers = new List<String>();
      using (var gray = img.Convert<Gray, Byte>())
      using (var canny = new Image<Gray, byte>(gray.Size))
      using (var stor = new MemStorage())
      {
        CvInvoke.cvCanny(gray, canny, 100, 50, 3);
        var contours = canny.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_TREE, stor);
        FindMarker(contours, gray, canny, markerImagesList, filteredMarkerImagesList, detectedMarkerRegionList, markers);
      }
      return markers;
    }

    private static int GetNumberOfChildren(Contour<Point> contours)
    {
      var child = contours.VNext;
      if (child == null) return 0;
      var count = 0;
      while (child != null)
      {
        count++;
        child = child.HNext;
      }
      return count;
    }

    private static void FindMarker(Contour<Point> contours, Image<Gray, Byte> gray, Image<Gray, Byte> canny, ICollection<Image<Gray, byte>> markerImagesList, ICollection<Image<Gray, byte>> filteredMarkerImagesList, ICollection<MCvBox2D> detectedMarkerRegionList, ICollection<string> markers)
    {
      for (; contours != null; contours = contours.HNext)
      {
        var numberOfChildren = GetNumberOfChildren(contours);
        if (numberOfChildren == 0) continue;

        if (!(contours.Area > 400)) continue;

        if (numberOfChildren < 3)
        {
          FindMarker(contours.VNext, gray, canny, markerImagesList, filteredMarkerImagesList, detectedMarkerRegionList, markers);
          continue;
        }
        var box = contours.GetMinAreaRect();
        if (box.angle < -45.0)
        {
          float tmp = box.size.Width;
          box.size.Width = box.size.Height;
          box.size.Height = tmp;
          box.angle += 90.0f;
        }
        else if (box.angle > 45.0)
        {
          float tmp = box.size.Width;
          box.size.Width = box.size.Height;
          box.size.Height = tmp;
          box.angle -= 90.0f;
        }
        double whRatio = (double)box.size.Width / box.size.Height;
        if (!(0.9 < whRatio && whRatio < 1.1)) //misure del quadrato
        {
          Contour<Point> child = contours.VNext;
          if (child != null)
            FindMarker(child, gray, canny, markerImagesList, filteredMarkerImagesList, detectedMarkerRegionList, markers);
          continue;
        }
        using (Image<Gray, Byte> tmp1 = gray.Copy(box))
        using (Image<Gray, Byte> tmp2 = tmp1.Resize(120, 120, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC, true)) //ridimensiono il marker. in teoria tesseract è più preciso
        {
          const int edgePixelSize = 2; //tolgo pixel dai bordi
          tmp2.ROI = new Rectangle(new Point(edgePixelSize, edgePixelSize), tmp2.Size - new Size(2 * edgePixelSize, 2 * edgePixelSize));
          var marker = tmp2.Copy();
          var filteredMarker = FilterMarker(marker);
          markerImagesList.Add(marker);
          filteredMarkerImagesList.Add(filteredMarker);
          detectedMarkerRegionList.Add(box);
        }
      }
    }

    private static Image<Gray, Byte> FilterMarker(Image<Gray, Byte> marker)
    {
      var thresh = marker.ThresholdBinaryInv(new Gray(120), new Gray(255));
      using (var mask = new Image<Gray, byte>(marker.Size))
      using (var canny = marker.Canny(100, 50))
      using (var stor = new MemStorage())
      {
        mask.SetValue(255.0);
        for (var contours = canny.FindContours(Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE, Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL, stor); contours != null; contours = contours.HNext)
        {
          var rect = contours.BoundingRectangle;
          if (rect.Height <= (marker.Height >> 1)) continue;
          rect.X -= 1;
          rect.Y -= 1;
          rect.Width += 2;
          rect.Height += 2;
          rect.Intersect(marker.ROI);
          mask.Draw(rect, new Gray(0.0), -1);
        }
        thresh.SetValue(0, mask);
      }
      thresh._Erode(1);
      thresh._Dilate(1);
      return thresh;
    }

    protected override void DisposeObject()
    {
      base.Dispose();
    }
  }
}
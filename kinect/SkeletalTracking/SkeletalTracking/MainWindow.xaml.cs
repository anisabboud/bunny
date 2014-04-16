// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf; 

namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        bool closing = false;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        private static SerialPort serialPort;
        float lastSentX = 0;
        Stopwatch stopWatch;
        bool waving = false;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);

            // Allow the user to set the appropriate properties.
            System.Diagnostics.Debug.Write(SerialPort.GetPortNames()[0]);
            serialPort = new SerialPort(SerialPort.GetPortNames()[0]);
            serialPort.BaudRate = 9600;
            //serialPort.PortName = SerialPort.GetPortNames()[0];
            //serialPort.Parity = SetPortParity(serialPort.Parity);
            //serialPort.DataBits = SetPortDataBits(serialPort.DataBits);
            //serialPort.StopBits = SetPortStopBits(serialPort.StopBits);
            //serialPort.Handshake = SetPortHandshake(serialPort.Handshake);
            serialPort.WriteTimeout = 500;
            serialPort.Open();
            stopWatch = new Stopwatch();
        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;

            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null)
            {
                return;
            }

            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.3f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };
            sensor.SkeletonStream.Enable(parameters);

            sensor.SkeletonStream.Enable();

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30); 
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            if (closing)
            {
                return;
            }

            //Get a skeleton
            Skeleton first =  GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }

            //set scaled position
            //ScalePosition(headImage, first.Joints[JointType.Head]);
            ScalePosition(leftEllipse, first.Joints[JointType.HandLeft]);
            ScalePosition(rightEllipse, first.Joints[JointType.HandRight]);

            label1.Content = first.Joints[JointType.Head].Position.X.ToString() + "\n" + first.Joints[JointType.Head].Position.Y.ToString() + "\n" + first.Joints[JointType.Head].Position.Z.ToString();
            try
            {
                float newX = first.Joints[JointType.Head].Position.X;
                if (Math.Abs(newX - lastSentX) > 0.1) {
                    serialPort.WriteLine(String.Format("{0}", newX));
                    Console.WriteLine("Sending: {0}", newX);
                    lastSentX = newX;
                    waving = false;
                } else {
                    if (!waving || stopWatch.ElapsedMilliseconds > 400) {
                        serialPort.WriteLine(String.Format("{0} w", lastSentX));
                        Console.WriteLine("Sending: {0} w", lastSentX);
                        waving = true;
                        stopWatch.Restart();
                    }
                }
            }
            catch (TimeoutException) { }

            GetCameraPoint(first, e);
        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {

            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }
                

                //Map a joint location to a point on the depth map
                //head
                DepthImagePoint headDepthPoint =
                    kinectSensorChooser1.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(first.Joints[JointType.Head].Position, kinectSensorChooser1.Kinect.DepthStream.Format);
                //left hand
                DepthImagePoint leftDepthPoint =
                    kinectSensorChooser1.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(first.Joints[JointType.HandLeft].Position, kinectSensorChooser1.Kinect.DepthStream.Format);
                //right hand
                DepthImagePoint rightDepthPoint =
                    kinectSensorChooser1.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(first.Joints[JointType.HandRight].Position, kinectSensorChooser1.Kinect.DepthStream.Format);


                //Map a depth point to a point on the color image
                //head
                ColorImagePoint headColorPoint =
                    kinectSensorChooser1.Kinect.CoordinateMapper.MapDepthPointToColorPoint(kinectSensorChooser1.Kinect.DepthStream.Format, headDepthPoint,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //left hand
                ColorImagePoint leftColorPoint =
                    kinectSensorChooser1.Kinect.CoordinateMapper.MapDepthPointToColorPoint(kinectSensorChooser1.Kinect.DepthStream.Format, leftDepthPoint,
                    ColorImageFormat.RgbResolution640x480Fps30);
                //right hand
                ColorImagePoint rightColorPoint =
                    kinectSensorChooser1.Kinect.CoordinateMapper.MapDepthPointToColorPoint(kinectSensorChooser1.Kinect.DepthStream.Format, rightDepthPoint,
                    ColorImageFormat.RgbResolution640x480Fps30);


                //Set location
                CameraPosition(headImage, headColorPoint);
                CameraPosition(leftEllipse, leftColorPoint);
                CameraPosition(rightEllipse, rightColorPoint);
            }        
        }


        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null; 
                }

                
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                         where s.TrackingState == SkeletonTrackingState.Tracked
                                         select s).FirstOrDefault();

                return first;

            }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }
                }
            }
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetTop(element, point.Y - element.Height / 2);

        }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 
            
            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(1280, 720, .3f, .3f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y); 
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            //StopKinect(kinectSensorChooser1.Kinect);
            serialPort.Close();
        }
    }
}

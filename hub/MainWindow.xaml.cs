﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// Modified by Henry Martin for the Kinected Hub
// Based on the Microsoft BodyBasics-WPF sample
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Net;
    using System.Collections;
    using System.Threading;
    using System.Text;
    using System.Threading.Tasks;

    public class menuItem {
        public string text { get; set; }
        public string actionLeft { get; set; }
        public string actionRight { get; set; }

        public menuItem(string text, string actionLeft, string actionRight)
        {
            this.text = text;
            this.actionLeft = actionLeft;
            this.actionRight = actionRight;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /////// POWERTAIL CONFIG ///////////
  
        private string powertailServerIp = "http://192.168.1.200:2531/";
        
        ////////////////////////////////////
        /// 
        private const double HandSize = 30;
        private const double JointThickness = 3;
        private const double ClipBoundsThickness = 10;
        private const float InferredZPositionClamp = 0.1f;
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));    
        private readonly Brush inferredJointBrush = Brushes.Yellow;   
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;


        private KinectSensor kinectSensor = null;
        private CoordinateMapper coordinateMapper = null;
        private BodyFrameReader bodyFrameReader = null;

        private Body[] bodies = null;
        private List<Tuple<JointType, JointType>> bones;

        private int displayWidth;
        private int displayHeight;

        private List<Pen> bodyColors;

        private string statusText = null;

        private bool wasRightHandLastClosed = false;
        private bool wasLeftHandLastClosed = false;
        
        private bool powerActionCooldown = true;
        private int powerActionCooldownAmount = 500;

        private bool currentRightHandState = false;
        private bool currentLeftHandState = false;
        
        public bool isInMenuMode = false;
        public List<menuItem> menuItems = new List<menuItem>();
        public int currentMenuItem = 0;

        // Stuff for cooldown
        private bool isCooldown = false;
        private int cooldownAmount = 500;
        private int cooldownTimer = 0;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            
            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();

            this.menuItems.Add(new menuItem("Control UDS Socket", "http://192.168.1.200:2531/power/off", "http://192.168.1.200:2531/power/on"));
            this.menuItems.Add(new menuItem("Control Remote Lights", "http://maker.ifttt.com/trigger/LaundryOff/with/key/BQBlebqgTjtddNl9ciNQx", "http://maker.ifttt.com/trigger/Laundry/with/key/BQBlebqgTjtddNl9ciNQx"));
            this.menuItems.Add(new menuItem("Toggle Music / Next Song", "http://192.168.1.200:2531/music/toggle", "http://192.168.1.200:2531/music/next"));

            this.updateMenu();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc, "left");
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc, "right");
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            this.updateMenu();
            
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }
        
        private void updateMenu() {

            if (this.isInMenuMode)
            {
                this.txtMenuText.Text = "                Menu Mode Active";
                this.txtMenuText3.Text = "                Select an option to deactivate";
                this.txtMenuText2.Text = "Current selection: " + this.menuItems[this.currentMenuItem].text;
                this.MenuStatusImg.Source = new BitmapImage(new Uri("pack://application:,,,/bars-solid.png"));
            }
            else {
                this.txtMenuText.Text = "                Menu Mode Inactive";
                this.txtMenuText3.Text = "                Close both fists to activate";
                this.txtMenuText2.Text = "Current mode: " + this.menuItems[this.currentMenuItem].text;
                this.MenuStatusImg.Source = new BitmapImage(new Uri("pack://application:,,,/hand-solid.png"));
            }

        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext, string hand)
        {
            
  
            
            switch (handState)
            {
                case HandState.Closed:
                    
                    if (hand == "right")
                    {
                        this.currentRightHandState = true;
                    }
                    else if (hand == "left")
                    {
                        this.currentLeftHandState = true;
                    }
                    
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);

                    if (this.currentRightHandState && this.currentLeftHandState && !(this.MenuModeCheckbox.IsChecked ?? false))
                    {
                        Debug.WriteLine("Both hands are closed");
                        
                        // Do cooldown
                        if (!this.isCooldown) {
                            this.isInMenuMode = !this.isInMenuMode;
                            this.currentRightHandState = false;
                            this.currentLeftHandState = false;
                            this.updateMenu();
                            this.doMenuModeCooldown();
                        }

                        break;
                    }

                    if (hand == "right")
                    {
                        if (!this.wasRightHandLastClosed)
                        {
                            Debug.WriteLine("Closed right hand");
                            this.wasRightHandLastClosed = true;

                            // Log the timestamp
                            if (!this.isInMenuMode)
                            {
                                Debug.WriteLine("Calling thread at: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                                Debug.WriteLine(this.menuItems[this.currentMenuItem].actionRight);
                                handleGetRequest(this.menuItems[this.currentMenuItem].actionRight);
                            }
                            else
                            {
                                if (!this.isCooldown) {
                                    Debug.WriteLine("right hand in menumode");
                                    // Right hand selects (closes the menu)
                                    this.isInMenuMode = false;
                                    this.updateMenu();
                                    this.doMenuModeCooldown();
                                }
                            }
                        }
                        else
                        {
                            this.wasRightHandLastClosed = false;
                        }
                    }
                    else if (hand == "left") {
                        if (!this.wasLeftHandLastClosed)
                        {
                            Debug.WriteLine("Closed left hand");
                            this.wasLeftHandLastClosed = true;
                            
                            if (!this.isInMenuMode)
                            {
                                Debug.WriteLine("Calling thread at: " + DateTime.Now.ToString("HH:mm:ss.fff"));
                                Debug.WriteLine(this.menuItems[this.currentMenuItem].actionLeft);
                                handleGetRequest(this.menuItems[this.currentMenuItem].actionLeft);
                            }
                            else
                            {
                                if (!this.isCooldown) {
                                    Debug.WriteLine("left hand in menumode");
                                    this.currentMenuItem++;
                                    if (this.currentMenuItem >= this.menuItems.Count)
                                    {
                                        this.currentMenuItem = 0;
                                    }
                                    this.updateMenu();
                                    this.doMenuModeCooldown();
                                }
                            }
                        }
                        else {
                            this.wasLeftHandLastClosed = false;
                        }
                    }

                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    if (hand == "right")
                    {
                        this.currentRightHandState = false;
                    }
                    else if (hand == "left")
                    {
                        this.currentLeftHandState = false;
                    }
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    if (hand == "right")
                    {
                        this.currentRightHandState = false;
                    }
                    else if (hand == "left")
                    {
                        this.currentLeftHandState = false;
                    }
                    break;
                default:
                    if (hand == "right")
                    {
                        this.currentRightHandState = false;
                    }
                    else if (hand == "left")
                    {
                        this.currentLeftHandState = false;
                    }
                    break;


            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void handlePowertailPowerAction(string action) {

            // super simple, just shoots a web request to this.powertailServerIp with the action as the route
            string url = this.powertailServerIp + "power/" + action;



            // Create a new thread to handle the GetRequest call
            Thread thread = new Thread(() =>
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        string message = String.Format("Get failed. Received HTTP {0}", response.StatusCode);
                        throw new ApplicationException(message);
                    }

                    // grab the response
                    string result = null;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        result = reader.ReadToEnd();
                        Debug.WriteLine(result);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                Debug.WriteLine("Thread exited with timestamp: " + DateTime.Now.ToString("h:mm:ss.fff tt"));
                
                
            });

            thread.Start();

        }

        private void handleGetRequest(string url) {
            // Create a new thread to handle the GetRequest call
            Thread thread = new Thread(() =>
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        string message = String.Format("Get failed. Received HTTP {0}", response.StatusCode);
                        throw new ApplicationException(message);
                    }

                    // grab the response
                    string result = null;
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
                        result = reader.ReadToEnd();
                        Debug.WriteLine(result);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                Debug.WriteLine("Thread exited with timestamp: " + DateTime.Now.ToString("h:mm:ss.fff tt"));


            });

            thread.Start();
        }

        private void doMenuModeCooldown()
        {
            var _this = this;
            Thread thread = new Thread(() =>
            {
                _this.isCooldown = true;
                Thread.Sleep(this.cooldownAmount);
                _this.isCooldown = false;
            });
            thread.Start();
        }
    }
}

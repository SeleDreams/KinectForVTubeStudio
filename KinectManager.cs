using Microsoft.Kinect.Face;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace LumiosNoctis
{
    internal class KinectManager : IDisposable
    {
        public MainWindow mainWindow;
        /// <summary>
        /// Currently used KinectSensor
        /// </summary>
        private KinectSensor sensor = null;

        /// <summary>
        /// Body frame source to get a BodyFrameReader
        /// </summary>
        private BodyFrameSource bodySource = null;

        /// <summary>
        /// Body frame reader to get body frames
        /// </summary>
        private BodyFrameReader bodyReader = null;

        /// <summary>
        /// HighDefinitionFaceFrameSource to get a reader and a builder from.
        /// Also to set the currently tracked user id to get High Definition Face Frames of
        /// </summary>
        private HighDefinitionFaceFrameSource highDefinitionFaceFrameSource = null;

        /// <summary>
        /// HighDefinitionFaceFrameReader to read HighDefinitionFaceFrame to get FaceAlignment
        /// </summary>
        private HighDefinitionFaceFrameReader highDefinitionFaceFrameReader = null;


        /// <summary>
        /// The currently tracked body
        /// </summary>
        private Body currentTrackedBody = null;

        /// <summary>
        /// The currently tracked body
        /// </summary>
        private ulong currentTrackingId = 0;

        /// <summary>
        /// Gets or sets the current tracked user id
        /// </summary>
        private string currentBuilderStatus = string.Empty;

        private FaceMesh mesh;

        /// <summary>
        /// Gets or sets the current tracked user id
        /// </summary>
        private ulong CurrentTrackingId
        {
            get
            {
                return this.currentTrackingId;
            }

            set
            {
                this.currentTrackingId = value;

            }
        }

        /// <summary>
        /// Gets or sets the current Face Builder instructions to user
        /// </summary>
        private string CurrentBuilderStatus
        {
            get
            {
                return this.currentBuilderStatus;
            }

            set
            {
                this.currentBuilderStatus = value;
            }
        }

        public KinectManager(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            InitializeHDFace();
        }

        public void Dispose()
        {
            mesh.Dispose();
        }

        /// <summary>
        /// Returns the length of a vector from origin
        /// </summary>
        /// <param name="point">Point in space to find it's distance from origin</param>
        /// <returns>Distance from origin</returns>
        private static double VectorLength(CameraSpacePoint point)
        {
            var result = Math.Pow(point.X, 2) + Math.Pow(point.Y, 2) + Math.Pow(point.Z, 2);

            result = Math.Sqrt(result);

            return result;
        }

        /// <summary>
        /// Finds the closest body from the sensor if any
        /// </summary>
        /// <param name="bodyFrame">A body frame</param>
        /// <returns>Closest body, null of none</returns>
        private static Body FindClosestBody(BodyFrame bodyFrame)
        {
            Body result = null;
            double closestBodyDistance = double.MaxValue;

            Body[] bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    var currentLocation = body.Joints[JointType.SpineBase].Position;

                    var currentDistance = VectorLength(currentLocation);

                    if (result == null || currentDistance < closestBodyDistance)
                    {
                        result = body;
                        closestBodyDistance = currentDistance;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Find if there is a body tracked with the given trackingId
        /// </summary>
        /// <param name="bodyFrame">A body frame</param>
        /// <param name="trackingId">The tracking Id</param>
        /// <returns>The body object, null of none</returns>
        private static Body FindBodyWithTrackingId(BodyFrame bodyFrame, ulong trackingId)
        {
            Body result = null;

            Body[] bodies = new Body[bodyFrame.BodyCount];
            bodyFrame.GetAndRefreshBodyData(bodies);

            foreach (var body in bodies)
            {
                if (body.IsTracked)
                {
                    if (body.TrackingId == trackingId)
                    {
                        result = body;
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Initialize Kinect object
        /// </summary>
        private void InitializeHDFace()
        {
            this.CurrentBuilderStatus = "Ready To Start Capture";

            this.sensor = KinectSensor.GetDefault();
            this.bodySource = this.sensor.BodyFrameSource;
            this.bodyReader = this.bodySource.OpenReader();
            this.bodyReader.FrameArrived += this.BodyReader_FrameArrived;

            this.highDefinitionFaceFrameSource = new HighDefinitionFaceFrameSource(this.sensor);
            this.highDefinitionFaceFrameSource.TrackingIdLost += this.HdFaceSource_TrackingIdLost;

            this.highDefinitionFaceFrameReader = this.highDefinitionFaceFrameSource.OpenReader();
            this.highDefinitionFaceFrameReader.FrameArrived += this.HdFaceReader_FrameArrived;

            mesh = new FaceMesh(this);

            this.sensor.Open();
        }

        /// <summary>
        /// Start a face capture operation
        /// </summary>
        public void StartCapture()
        {
            StopFaceCapture();
            mesh.StartCapture(in highDefinitionFaceFrameSource);
        }

        /// <summary>
        /// Cancel the current face capture operation
        /// </summary>
        public void StopFaceCapture()
        {
            mesh.StopCapture();    
        }

        /// <summary>
        /// This event fires when a BodyFrame is ready for consumption
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            var frameReference = e.FrameReference;
            using (var frame = frameReference.AcquireFrame())
            {
                if (frame == null)
                {
                    // We might miss the chance to acquire the frame, it will be null if it's missed
                    return;
                }

                if (this.currentTrackedBody != null)
                {
                    this.currentTrackedBody = FindBodyWithTrackingId(frame, this.CurrentTrackingId);

                    if (this.currentTrackedBody != null)
                    {
                        return;
                    }
                }

                Body selectedBody = FindClosestBody(frame);

                if (selectedBody == null)
                {
                    return;
                }

                this.currentTrackedBody = selectedBody;
                this.CurrentTrackingId = selectedBody.TrackingId;

                this.highDefinitionFaceFrameSource.TrackingId = this.CurrentTrackingId;
            }
        }

        /// <summary>
        /// This event is fired when a tracking is lost for a body tracked by HDFace Tracker
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void HdFaceSource_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            var lostTrackingID = e.TrackingId;

            if (this.CurrentTrackingId == lostTrackingID)
            {
                this.CurrentTrackingId = 0;
                this.currentTrackedBody = null;
                mesh.Dispose();
                this.highDefinitionFaceFrameSource.TrackingId = 0;
            }
        }

        /// <summary>
        /// This event is fired when a new HDFace frame is ready for consumption
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void HdFaceReader_FrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                // We might miss the chance to acquire the frame; it will be null if it's missed.
                // Also ignore this frame if face tracking failed.
                if (frame == null || !frame.IsFaceTracked)
                {
                    return;
                }

                mesh.FrameArrived(in frame);
            }
        }



    }
}

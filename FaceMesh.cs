using System;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Microsoft.Kinect.Face;
using System.Windows;

namespace LumiosNoctis
{
    public class FaceMesh : IDisposable
    {
        KinectManager kinect;
        MeshGeometry3D theGeometry;
        /// <summary>
        /// FaceModel is a result of capturing a face
        /// </summary>
        private FaceModel currentFaceModel = null;

        /// <summary>
        /// FaceModelBuilder is used to produce a FaceModel
        /// </summary>
        public FaceModelBuilder faceModelBuilder = null;

        /// <summary>
        /// FaceAlignment is the result of tracking a face, it has face animations location and orientation
        /// </summary>
        private FaceAlignment currentFaceAlignment = null;

        public void Dispose()
        {
            if (currentFaceModel != null)
            {
                currentFaceModel.Dispose();
                currentFaceModel = null;
            }
            if (faceModelBuilder != null)
            {
                faceModelBuilder.Dispose();
                faceModelBuilder = null;
            }
        }

        public FaceMesh(KinectManager kinectManager)
        {
            kinect = kinectManager;
            theGeometry = kinect.mainWindow.theGeometry;
            currentFaceModel = new FaceModel();
            currentFaceAlignment = new FaceAlignment();

            InitializeMesh();
            UpdateMesh();
        }

        /// <summary>
        /// Initializes a 3D mesh to deform every frame
        /// </summary>
        private void InitializeMesh()
        {
            var vertices = currentFaceModel.CalculateVerticesForAlignment(currentFaceAlignment);

            var triangleIndices = currentFaceModel.TriangleIndices;

            var indices = new Int32Collection(triangleIndices.Count);

            for (int i = 0; i < triangleIndices.Count; i += 3)
            {
                uint index01 = triangleIndices[i];
                uint index02 = triangleIndices[i + 1];
                uint index03 = triangleIndices[i + 2];

                indices.Add((int)index03);
                indices.Add((int)index02);
                indices.Add((int)index01);
            }
            theGeometry.TriangleIndices = indices;
            theGeometry.Normals = null;
            theGeometry.Positions = new Point3DCollection();
            theGeometry.TextureCoordinates = new PointCollection();

            foreach (var vert in vertices)
            {
                theGeometry.Positions.Add(new Point3D(vert.X, vert.Y, -vert.Z));
                theGeometry.TextureCoordinates.Add(new Point());
            }
        }
        public Point3D GetCenterPoint(Point3D upperLip, Point3D lowerLip)
        {
            float centerX = (float)(upperLip.X + lowerLip.X) / 2;
            float centerY = (float)(upperLip.Y + lowerLip.Y) / 2;
            float centerZ = (float)(upperLip.Z + lowerLip.Z) / 2;

            return new Point3D { X = centerX, Y = centerY, Z = centerZ };
        }

        private async void UpdateFacialExpressions()
        {
            // Get the orientation quaternion from FaceAlignment
            Microsoft.Kinect.Vector4 headOrientation = currentFaceAlignment.FaceOrientation;
           
            // Calculate the up direction using quaternion math
            Vector3D upDirection = new Vector3D(
                2 * (headOrientation.X * headOrientation.Y + headOrientation.W * headOrientation.Z),
                headOrientation.W * headOrientation.W - headOrientation.X * headOrientation.X
                    - headOrientation.Y * headOrientation.Y + headOrientation.Z * headOrientation.Z,
                2 * (headOrientation.Y * headOrientation.Z - headOrientation.W * headOrientation.X)
            );
            upDirection.Normalize();

            var vtubeStudio = kinect.mainWindow.vtubeStudio;
            float jawOpenValue = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.JawOpen];
            Console.WriteLine(currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LefteyeClosed] * 10);
            float rightEyeOpenValue = 1.0f - Math.Min(currentFaceAlignment.AnimationUnits[FaceShapeAnimations.RighteyeClosed] * 5, 1.0f);

            float leftEyeOpenValue = 1.0f - Math.Min(currentFaceAlignment.AnimationUnits[FaceShapeAnimations.LefteyeClosed] * 5,1.0f);
            vtubeStudio.SetVtubeStudioParam(VTubeStudioParameters.EyeOpenLeft, leftEyeOpenValue);
            vtubeStudio.SetVtubeStudioParam(VTubeStudioParameters.EyeOpenRight, rightEyeOpenValue);
            float yaw, pitch, roll;
            QuaternionMethods.ToYawPitchRoll(currentFaceAlignment.FaceOrientation, out yaw, out pitch, out roll);
            vtubeStudio.SetVtubeStudioParam(VTubeStudioParameters.FaceAngleX, -pitch );
            vtubeStudio.SetVtubeStudioParam(VTubeStudioParameters.FaceAngleY, yaw);
            vtubeStudio.SetVtubeStudioParam(VTubeStudioParameters.FaceAngleZ, -roll);
            vtubeStudio.SetVtubeStudioParam(VTubeStudioParameters.MouthOpen, jawOpenValue);
           // CameraSpacePoint leftEyePosition = theGeometry..GetCameraSpacePoint(FaceShapeAnimations.LeftEyeClosed);
            Point3D upperLipPos = theGeometry.Positions[(int)HighDetailFacePoints.MouthUpperlipMidbottom];
            Point3D lowerLipPos = theGeometry.Positions[(int)HighDetailFacePoints.MouthLowerlipMidtop];
            Point3D middleMouthPosPoint3D = GetCenterPoint(upperLipPos, lowerLipPos);
            Point3D rightMouthCorner = theGeometry.Positions[(int)HighDetailFacePoints.MouthRightcorner];
            Vector3D distanceCorner = new Vector3D((rightMouthCorner.X - middleMouthPosPoint3D.X) * upDirection.X, (rightMouthCorner.Y - middleMouthPosPoint3D.Y) * upDirection.Y, (rightMouthCorner.Z - middleMouthPosPoint3D.Z) * upDirection.Z);
            float SmileValue = (float)(distanceCorner.Length * 100);
            vtubeStudio.SetVtubeStudioParam(VTubeStudioParameters.MouthSmile, SmileValue);
            //float rightSmirkOpenValue = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.JawSlideRight]
            await vtubeStudio.SendTrackingParameter();
        }

        /// <summary>
        /// Sends the new deformed mesh to be drawn
        /// </summary>
        private void UpdateMesh()
        {
            if (currentFaceModel == null)
            {
                return;
            }
            var vertices = currentFaceModel.CalculateVerticesForAlignment(currentFaceAlignment);

            for (int i = 0; i < vertices.Count; i++)
            {
                var vert = vertices[i];
                theGeometry.Positions[i] = new Point3D(vert.X, vert.Y, -vert.Z);
            }
        }

        /// <summary>
        /// This event fires when the face capture operation is completed
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void HdFaceBuilder_CollectionCompleted(object sender, FaceModelBuilderCollectionCompletedEventArgs e)
        {
            var modelData = e.ModelData;

            this.currentFaceModel = modelData.ProduceFaceModel();

            this.faceModelBuilder.Dispose();
            this.faceModelBuilder = null;

           // return "Capture Complete";
        }


        public void StartCapture(in HighDefinitionFaceFrameSource highDefinitionFaceFrameSource)
        {
            this.faceModelBuilder = null;

            this.faceModelBuilder = highDefinitionFaceFrameSource.OpenModelBuilder(FaceModelBuilderAttributes.None);

            this.faceModelBuilder.BeginFaceDataCollection();

            this.faceModelBuilder.CollectionCompleted += this.HdFaceBuilder_CollectionCompleted;
        }

        public void StopCapture()
        {
            if (this.faceModelBuilder != null)
            {
                this.faceModelBuilder.Dispose();
                this.faceModelBuilder = null;
            }
        }

        public void FrameArrived(in HighDefinitionFaceFrame frame)
        {
            frame.GetAndRefreshFaceAlignmentResult(currentFaceAlignment);
            this.UpdateMesh();
            this.UpdateFacialExpressions();
        }
    }
}
